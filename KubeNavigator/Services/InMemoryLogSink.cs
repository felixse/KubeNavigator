using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace KubeNavigator.Services;

public class InMemoryLogSink : ILogEventSink
{
    private readonly Queue<LogEvent> _logEvents = new();
    private readonly int _maxLogCount;

    public event EventHandler<LogEvent>? LogReceived;

    public InMemoryLogSink(int maxLogCount = 1000)
    {
        _maxLogCount = maxLogCount;
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_logEvents)
        {
            _logEvents.Enqueue(logEvent);

            if (_logEvents.Count > _maxLogCount)
            {
                _logEvents.Dequeue();
            }
        }

        LogReceived?.Invoke(this, logEvent);
    }

    public IReadOnlyList<LogEvent> GetLogs()
    {
        lock (_logEvents)
        {
            return [.. _logEvents];
        }
    }

    public void Clear()
    {
        lock (_logEvents)
        {
            _logEvents.Clear();
        }
    }
}
