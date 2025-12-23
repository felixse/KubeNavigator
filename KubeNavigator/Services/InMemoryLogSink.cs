using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace KubeNavigator.Services;

public class InMemoryLogSink : ILogEventSink
{
    private readonly List<LogEvent> _logEvents = [];
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
            _logEvents.Add(logEvent);
            
            if (_logEvents.Count > _maxLogCount)
            {
                _logEvents.RemoveAt(0);
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
