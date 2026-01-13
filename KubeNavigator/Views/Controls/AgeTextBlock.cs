using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views.Controls;

public partial class AgeTextBlock : UserControl
{
    private static readonly object _lockObject = new object();
    private static readonly List<WeakReference<AgeTextBlock>> _instances = new();
    private static DispatcherQueueTimer? _sharedTimer;
    private static DispatcherQueue? _dispatcherQueue;

    private DateTime? _timestamp;
    private readonly TextBlock _textBlock;

    public static readonly DependencyProperty TimestampProperty = DependencyProperty.Register(
        nameof(Timestamp),
        typeof(DateTime?),
        typeof(AgeTextBlock),
        new PropertyMetadata(null, OnTimestampChanged)
    );

    public DateTime? Timestamp
    {
        get => (DateTime?)GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public AgeTextBlock()
    {
        _textBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        Content = _textBlock;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnTimestampChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AgeTextBlock control)
        {
            control._timestamp = e.NewValue as DateTime?;
            control.UpdateAge();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        lock (_lockObject)
        {
            // Register this instance
            _instances.Add(new WeakReference<AgeTextBlock>(this));

            // Initialize shared timer if needed
            if (_sharedTimer == null)
            {
                _dispatcherQueue = DispatcherQueue;
                _sharedTimer = _dispatcherQueue.CreateTimer();
                _sharedTimer.Interval = TimeSpan.FromSeconds(1);
                _sharedTimer.Tick += OnSharedTimerTick;
                _sharedTimer.Start();
            }
        }

        UpdateAge();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Instance will be cleaned up on next timer tick
    }

    private static void OnSharedTimerTick(DispatcherQueueTimer sender, object args)
    {
        lock (_lockObject)
        {
            // Clean up dead references and update alive instances
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].TryGetTarget(out var control))
                {
                    control.UpdateAge();
                }
                else
                {
                    // Remove dead reference
                    _instances.RemoveAt(i);
                }
            }

            // Stop timer if no instances remain
            if (_instances.Count == 0 && _sharedTimer != null)
            {
                _sharedTimer.Stop();
                _sharedTimer = null;
                _dispatcherQueue = null;
            }
        }
    }

    private void UpdateAge()
    {
        _textBlock.Text = FormatDuration(_timestamp);
    }

    private static string FormatDuration(DateTime? timestamp)
    {
        var nullableAge = DateTime.UtcNow - timestamp;
        if (nullableAge is TimeSpan age)
        {
            return FormatDuration(age);
        }

        return string.Empty;
    }

    private static string FormatDuration(TimeSpan duration, bool compact = true)
    {
        var totalSeconds = (int)Math.Floor(duration.TotalSeconds);
        var separator = compact ? "" : " ";

        if (totalSeconds < 0)
        {
            return "0s";
        }
        else if (totalSeconds < 60 * 2)
        {
            return $"{totalSeconds}s";
        }

        var totalMinutes = (int)Math.Floor(duration.TotalMinutes);

        if (totalMinutes < 60 * 2)
        {
            return $"{totalMinutes}m";
        }

        var totalHours = (int)Math.Floor(duration.TotalHours);

        if (totalHours < 24 * 2)
        {
            return $"{totalHours}h";
        }

        var totalDays = (int)Math.Floor(duration.TotalDays);

        if (totalDays < 365 * 2)
        {
            return $"{totalDays}d";
        }

        var totalYears = totalDays / 365;

        return $"{totalYears}y";
    }
}
