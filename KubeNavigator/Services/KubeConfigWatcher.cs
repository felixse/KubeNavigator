using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace KubeNavigator.Services;

public sealed partial class KubeConfigWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger<KubeConfigWatcher> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private HashSet<string> _knownContexts;

    public event Action<string>? ContextAdded;
    public event Action<string>? ContextRemoved;
    public event Action<IReadOnlyList<string>>? ContextsChanged;

    public KubeConfigWatcher(
        IReadOnlyList<string> initialContextNames,
        ILoggerFactory loggerFactory,
        DispatcherQueue dispatcherQueue
    )
    {
        _logger = loggerFactory.CreateLogger<KubeConfigWatcher>();
        _dispatcherQueue = dispatcherQueue;
        _knownContexts = new HashSet<string>(initialContextNames);

        var configPath = KubernetesClientConfiguration.KubeConfigDefaultLocation;
        var directory = Path.GetDirectoryName(configPath)!;
        var fileName = Path.GetFileName(configPath);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;

        Log.WatcherStarted(_logger, configPath);
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Log.KubeConfigFileChanged(_logger, e.FullPath);

        // Small delay to allow the file write to complete and to debounce
        // rapid successive change notifications.
        await Task.Delay(500);

        IReadOnlyList<string> newContextNames;
        try
        {
            newContextNames = await KubernetesService.LoadContextNamesAsync();
        }
        catch (Exception ex)
        {
            Log.KubeConfigReloadFailed(_logger, ex);
            return;
        }

        var newSet = new HashSet<string>(newContextNames);
        var added = newContextNames.Where(n => !_knownContexts.Contains(n)).ToList();
        var removed = _knownContexts.Where(n => !newSet.Contains(n)).ToList();
        var retained = _knownContexts.Where(n => newSet.Contains(n)).ToList();

        _knownContexts = newSet;

        if (added.Count == 0 && removed.Count == 0)
        {
            // Context list unchanged — the file edit likely modified an existing context.
            Log.KubeConfigContextsUnchanged(_logger, retained.Count);
            _dispatcherQueue.TryEnqueue(() =>
            {
                ContextsChanged?.Invoke(retained);
            });
            return;
        }

        foreach (var name in added)
        {
            Log.KubeConfigContextAdded(_logger, name);
        }

        foreach (var name in removed)
        {
            Log.KubeConfigContextRemoved(_logger, name);
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            foreach (var name in added)
            {
                ContextAdded?.Invoke(name);
            }

            foreach (var name in removed)
            {
                ContextRemoved?.Invoke(name);
            }

            if (retained.Count > 0)
            {
                ContextsChanged?.Invoke(retained);
            }
        });
    }

    public void Dispose()
    {
        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Dispose();
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 6001,
            Level = LogLevel.Information,
            Message = "KubeConfig watcher started for {Path}"
        )]
        public static partial void WatcherStarted(ILogger logger, string path);

        [LoggerMessage(
            EventId = 6002,
            Level = LogLevel.Information,
            Message = "KubeConfig file changed: {Path}"
        )]
        public static partial void KubeConfigFileChanged(ILogger logger, string path);

        [LoggerMessage(
            EventId = 6003,
            Level = LogLevel.Error,
            Message = "Failed to reload KubeConfig after file change"
        )]
        public static partial void KubeConfigReloadFailed(ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 6004,
            Level = LogLevel.Information,
            Message = "KubeConfig context added: {ContextName}"
        )]
        public static partial void KubeConfigContextAdded(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 6005,
            Level = LogLevel.Information,
            Message = "KubeConfig context removed: {ContextName}"
        )]
        public static partial void KubeConfigContextRemoved(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 6006,
            Level = LogLevel.Debug,
            Message = "KubeConfig context list unchanged, {Count} existing contexts may have been modified"
        )]
        public static partial void KubeConfigContextsUnchanged(ILogger logger, int count);
    }
}
