using System.Globalization;
using EmployeeService.Web.Configuration;
using EmployeeService.Web.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.With<SourceNameEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceName:l} {Message:lj} TraceId={TraceId}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddProgramDependencies(builder.Configuration);

    var app = builder.Build();

    app.Configure();

    await app.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}