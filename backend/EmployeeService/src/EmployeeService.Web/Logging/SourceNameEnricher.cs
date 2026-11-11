using Serilog.Core;
using Serilog.Events;

namespace EmployeeService.Web.Logging;

public sealed class SourceNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var source) &&
            source is ScalarValue { Value: string sourceContext })
        {
            var sourceName = sourceContext[(sourceContext.LastIndexOf('.') + 1)..];
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SourceName", sourceName));
        }
    }
}
