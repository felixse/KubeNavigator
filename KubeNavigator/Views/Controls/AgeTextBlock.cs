using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views.Controls;

public partial class AgeTextBlock : UserControl
{
    private DispatcherQueueTimer? _timer;
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
        if (_timer == null)
        {
            _timer = DispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
        }

        UpdateAge();
        _timer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer?.Stop();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        UpdateAge();
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
