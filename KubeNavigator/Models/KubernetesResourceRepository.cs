using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Services;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.Models;

public partial class KubernetesResourceRepository<T> : IKubernetesResourceRepository
    where T : IKubernetesObject<V1ObjectMeta>
{
    private static readonly TimeSpan[] ReconnectBackoff =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    ];

    // If no watch event is received within this window the connection is assumed
    // to be silently dead (e.g. after system sleep) and is forcibly re-established.
    private static readonly TimeSpan WatchIdleTimeout = TimeSpan.FromMinutes(5);

    private readonly ILogger _logger;
    private readonly HashSet<IKubernetesResourceEventSubscriber> _subscribers = [];
    private readonly KubernetesService _kubernetesService;
    private readonly Dictionary<(string?, string?), T> _resources = [];
    private readonly object _watcherLock = new();

    private Watcher<T>? _watcher;
    private CancellationTokenSource? _watcherCts;
    private Timer? _watchdogTimer;
    private DateTime _lastEventUtc;
    private DateTime _watcherCreatedUtc;
    private int _reconnectAttempt;

    public ResourceType ResourceType { get; }

    private int _instance;

    public KubernetesResourceRepository(
        ResourceType resourceType,
        KubernetesService kubernetesService,
        ILoggerFactory loggerFactory
    )
    {
        _kubernetesService = kubernetesService;
        ResourceType = resourceType;
        _logger = loggerFactory.CreateLogger(
            $"KubeNavigator.Models.KubernetesResourceRepository<{typeof(T).Name}>"
        );
        _instance = Random.Shared.Next();
    }

    private static (string?, string?) ResourceKey(IKubernetesObject<V1ObjectMeta> r) =>
        (r.Metadata.Name, r.Metadata.NamespaceProperty);

    public IReadOnlyCollection<TItem> GetItems<TItem>()
        where TItem : IKubernetesObject<V1ObjectMeta>
    {
        if (typeof(T) == typeof(TItem))
        {
            return (IReadOnlyCollection<TItem>)(IReadOnlyCollection<T>)_resources.Values;
        }

        return [.. _resources.Values.Cast<TItem>()];
    }

    public async Task StartAsync()
    {
        Log.InitialListStarting(_logger, ResourceType.Plural);
        var items = await _kubernetesService.ListResourcesAsync<T>(ResourceType);

        foreach (var item in items.Items)
        {
            _resources[ResourceKey(item)] = item;
        }
        Log.InitialListCompleted(_logger, ResourceType.Plural, items.Items.Count);
    }

    public void AddSubscriber(IKubernetesResourceEventSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
        Log.SubscriberAdded(_logger, ResourceType.Plural, _subscribers.Count);

        if (_subscribers.Any())
        {
            CancellationToken token;
            lock (_watcherLock)
            {
                if (_watcherCts == null || _watcherCts.IsCancellationRequested)
                {
                    _watcherCts = new CancellationTokenSource();
                }
                token = _watcherCts.Token;
            }
            _ = ConnectWatcherAsync(token);
        }
    }

    public void RemoveSubscriber(IKubernetesResourceEventSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
        Log.SubscriberRemoved(_logger, ResourceType.Plural, _subscribers.Count);
        if (!_subscribers.Any())
        {
            StopWatcher();
        }
    }

    private async Task ConnectWatcherAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Log.WatcherStarting(_logger, ResourceType.Plural);

        string? listResourceVersion;
        try
        {
            var currentItems = await _kubernetesService.ListResourcesAsync<T>(
                ResourceType,
                cancellationToken
            );

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var currentSet = new HashSet<(string?, string?)>(
                currentItems.Items.Select(i => ResourceKey(i))
            );

            listResourceVersion = currentItems.Metadata?.ResourceVersion;

            var staleKeys = _resources.Keys.Where(k => !currentSet.Contains(k)).ToList();

            foreach (var key in staleKeys)
            {
                var stale = _resources[key];
                _resources.Remove(key);
                foreach (var subscriber in _subscribers)
                {
                    subscriber.OnResourceEvent(
                        KubernetesResourceEvent.Deleted,
                        ResourceType,
                        stale
                    );
                }
            }

            foreach (var item in currentItems.Items)
            {
                var key = ResourceKey(item);
                if (!_resources.ContainsKey(key))
                {
                    _resources[key] = item;
                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OnResourceEvent(
                            KubernetesResourceEvent.Added,
                            ResourceType,
                            item
                        );
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.WatcherError(_logger, ResourceType.Plural, ex);
            ScheduleReconnect(cancellationToken);
            return;
        }

        Watcher<T> watcher;
        try
        {
            watcher = _kubernetesService.WatchResources<T>(
                ResourceType,
                listResourceVersion,
                (watchEventType, resource) => HandleWatchEvent(watchEventType, resource),
                ex => HandleWatcherError(ex, cancellationToken),
                () => HandleWatcherClosed(cancellationToken)
            );
        }
        catch (Exception ex)
        {
            Log.WatcherError(_logger, ResourceType.Plural, ex);
            ScheduleReconnect(cancellationToken);
            return;
        }

        lock (_watcherLock)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                watcher.Dispose();
                return;
            }

            _watcher?.Dispose();
            _watcher = watcher;
            _lastEventUtc = DateTime.UtcNow;
            _watcherCreatedUtc = DateTime.UtcNow;
            EnsureWatchdogTimer();
        }
    }

    // Only consider the connection healthy (and reset backoff) once the watcher
    // has been alive long enough. Kubernetes sends synthetic ADDED events through
    // the stream even when the connection is about to be closed immediately.
    private static readonly TimeSpan MinHealthyWatchDuration = TimeSpan.FromSeconds(30);

    private void HandleWatchEvent(WatchEventType watchEventType, T resource)
    {
        _lastEventUtc = DateTime.UtcNow;
        if (DateTime.UtcNow - _watcherCreatedUtc > MinHealthyWatchDuration)
        {
            Interlocked.Exchange(ref _reconnectAttempt, 0);
        }

        var key = ResourceKey(resource);
        if (watchEventType == WatchEventType.Added)
        {
            if (_resources.TryGetValue(key, out var existing))
            {
                var currentVersion = int.Parse(existing.Metadata.ResourceVersion);
                var receivedVersion = int.Parse(resource.Metadata.ResourceVersion);
                if (receivedVersion > currentVersion)
                {
                    _resources[key] = resource;

                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OnResourceEvent(
                            KubernetesResourceEvent.Modified,
                            ResourceType,
                            resource
                        );
                    }
                }
            }
            else
            {
                _resources[key] = resource;

                foreach (var subscriber in _subscribers)
                {
                    subscriber.OnResourceEvent(
                        KubernetesResourceEvent.Added,
                        ResourceType,
                        resource
                    );
                }
            }
        }
        else if (watchEventType == WatchEventType.Modified)
        {
            if (_resources.TryGetValue(key, out var existing))
            {
                var currentVersion = int.Parse(existing.Metadata.ResourceVersion);
                var receivedVersion = int.Parse(resource.Metadata.ResourceVersion);
                if (receivedVersion > currentVersion)
                {
                    _resources[key] = resource;

                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OnResourceEvent(
                            KubernetesResourceEvent.Modified,
                            ResourceType,
                            resource
                        );
                    }
                }
            }
        }
        else if (watchEventType == WatchEventType.Deleted)
        {
            _resources.Remove(key);

            foreach (var subscriber in _subscribers)
            {
                subscriber.OnResourceEvent(
                    KubernetesResourceEvent.Deleted,
                    ResourceType,
                    resource
                );
            }
        }
        else if (watchEventType == WatchEventType.Bookmark)
        {
            // Bookmark events carry an updated resourceVersion but no payload.
            // The _lastEventUtc and backoff reset at the top of this method
            // already handle what we need — just don't process as a resource change.
        }
        else
        {
            Log.UnhandledWatchEventType(
                _logger,
                ResourceType.Plural,
                watchEventType.ToString()
            );
        }
    }

    private void HandleWatcherError(Exception ex, CancellationToken cancellationToken)
    {
        Log.WatcherError(_logger, ResourceType.Plural, ex);
        ScheduleReconnect(cancellationToken);
    }

    private void HandleWatcherClosed(CancellationToken cancellationToken)
    {
        Log.WatcherClosed(_logger, ResourceType.Plural);
        ScheduleReconnect(cancellationToken);
    }

    private void ScheduleReconnect(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var attempt = Interlocked.Increment(ref _reconnectAttempt);

        // After too many consecutive failed attempts without receiving data, stop
        // active reconnects and let the watchdog timer retry at its slower cadence.
        const int maxAttempts = 8;
        if (attempt > maxAttempts)
        {
            Log.WatcherReconnectGaveUp(_logger, ResourceType.Plural, attempt);
            lock (_watcherLock)
            {
                _watcher?.Dispose();
                _watcher = null;
            }
            return;
        }

        var baseDelay = ReconnectBackoff[Math.Min(attempt - 1, ReconnectBackoff.Length - 1)];
        // Apply +/-25% jitter to avoid thundering-herd reconnects across many
        // repositories simultaneously when the API server or network recovers.
        var jitterFactor = 0.75 + Random.Shared.NextDouble() * 0.5;
        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * jitterFactor);

        Log.WatcherReconnectScheduled(
            _logger,
            ResourceType.Plural,
            attempt,
            (int)delay.TotalMilliseconds
        );

        lock (_watcherLock)
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
                await ConnectWatcherAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // expected on stop
            }
            catch (Exception ex)
            {
                Log.WatcherError(_logger, ResourceType.Plural, ex);
            }
        });
    }

    private void EnsureWatchdogTimer()
    {
        _watchdogTimer ??= new Timer(
            OnWatchdogTick,
            null,
            WatchIdleTimeout,
            WatchIdleTimeout
        );
    }

    private void OnWatchdogTick(object? state)
    {
        CancellationToken token;
        lock (_watcherLock)
        {
            if (_watcherCts == null || _watcherCts.IsCancellationRequested)
            {
                return;
            }

            if (_watcher == null && DateTime.UtcNow - _lastEventUtc < WatchIdleTimeout)
            {
                // A reconnect is already in flight; nothing to do.
                return;
            }

            if (DateTime.UtcNow - _lastEventUtc < WatchIdleTimeout)
            {
                return;
            }

            token = _watcherCts.Token;
        }

        Log.WatcherIdleTimeout(_logger, ResourceType.Plural, (int)WatchIdleTimeout.TotalSeconds);
        // Reset attempt counter so the watchdog-triggered reconnect gets fresh backoff.
        Interlocked.Exchange(ref _reconnectAttempt, 0);
        ScheduleReconnect(token);
    }

    private void StopWatcher()
    {
        Log.WatcherStopping(_logger, ResourceType.Plural);

        CancellationTokenSource? cts;
        Timer? timer;
        Watcher<T>? watcher;
        lock (_watcherLock)
        {
            cts = _watcherCts;
            _watcherCts = null;
            timer = _watchdogTimer;
            _watchdogTimer = null;
            watcher = _watcher;
            _watcher = null;
            _reconnectAttempt = 0;
        }

        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        cts?.Dispose();
        timer?.Dispose();
        watcher?.Dispose();
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 6001,
            Level = LogLevel.Debug,
            Message = "Starting initial list for {ResourceType}"
        )]
        public static partial void InitialListStarting(ILogger logger, string resourceType);

        [LoggerMessage(
            EventId = 6002,
            Level = LogLevel.Debug,
            Message = "Initial list completed for {ResourceType} with {Count} items"
        )]
        public static partial void InitialListCompleted(
            ILogger logger,
            string resourceType,
            int count
        );

        [LoggerMessage(
            EventId = 6003,
            Level = LogLevel.Debug,
            Message = "Subscriber added for {ResourceType}, total subscribers: {Count}"
        )]
        public static partial void SubscriberAdded(ILogger logger, string resourceType, int count);

        [LoggerMessage(
            EventId = 6004,
            Level = LogLevel.Debug,
            Message = "Subscriber removed for {ResourceType}, total subscribers: {Count}"
        )]
        public static partial void SubscriberRemoved(
            ILogger logger,
            string resourceType,
            int count
        );

        [LoggerMessage(
            EventId = 6005,
            Level = LogLevel.Information,
            Message = "Starting watcher for {ResourceType}"
        )]
        public static partial void WatcherStarting(ILogger logger, string resourceType);

        [LoggerMessage(
            EventId = 6006,
            Level = LogLevel.Information,
            Message = "Stopping watcher for {ResourceType}"
        )]
        public static partial void WatcherStopping(ILogger logger, string resourceType);

        [LoggerMessage(
            EventId = 6007,
            Level = LogLevel.Warning,
            Message = "Unhandled watch event type {EventType} for {ResourceType}"
        )]
        public static partial void UnhandledWatchEventType(
            ILogger logger,
            string resourceType,
            string eventType
        );

        [LoggerMessage(
            EventId = 6008,
            Level = LogLevel.Error,
            Message = "Watcher error for {ResourceType}"
        )]
        public static partial void WatcherError(
            ILogger logger,
            string resourceType,
            Exception exception
        );

        [LoggerMessage(
            EventId = 6009,
            Level = LogLevel.Information,
            Message = "Watcher closed by server for {ResourceType}"
        )]
        public static partial void WatcherClosed(ILogger logger, string resourceType);

        [LoggerMessage(
            EventId = 6010,
            Level = LogLevel.Information,
            Message = "Reconnecting watcher for {ResourceType} (attempt {Attempt}) in {DelayMilliseconds}ms"
        )]
        public static partial void WatcherReconnectScheduled(
            ILogger logger,
            string resourceType,
            int attempt,
            int delayMilliseconds
        );

        [LoggerMessage(
            EventId = 6011,
            Level = LogLevel.Warning,
            Message = "Watcher for {ResourceType} idle for {IdleSeconds}s; forcing reconnect"
        )]
        public static partial void WatcherIdleTimeout(
            ILogger logger,
            string resourceType,
            int idleSeconds
        );

        [LoggerMessage(
            EventId = 6012,
            Level = LogLevel.Warning,
            Message = "Watcher for {ResourceType} gave up reconnecting after {Attempts} attempts; will retry on next watchdog tick"
        )]
        public static partial void WatcherReconnectGaveUp(
            ILogger logger,
            string resourceType,
            int attempts
        );
    }
}
