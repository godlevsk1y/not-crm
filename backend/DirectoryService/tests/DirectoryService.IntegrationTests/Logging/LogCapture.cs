using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace DirectoryService.IntegrationTests.Logging;

public sealed class LogCapture : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => _events.ToArray();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    public void Clear() => _events.Clear();
}
