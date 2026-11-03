using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Framework.Middlewares;

namespace Shared.Framework.Tests;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task UnhandledException_ReturnsInternalErrorEnvelopeAndLogsException()
    {
        var exception = new InvalidOperationException("sensitive detail");
        var logger = new TestLogger();
        var middleware = new ExceptionMiddleware(_ => throw exception, logger);
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Same(exception, logger.Exception);
        Assert.Equal(LogLevel.Error, logger.Level);
        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("result").ValueKind);
        Assert.True(root.GetProperty("isError").GetBoolean());
        Assert.True(root.GetProperty("timeGenerated").TryGetDateTime(out _));
        var error = root.GetProperty("error");
        Assert.Equal("Internal", error.GetProperty("type").GetString());
        var message = Assert.Single(error.GetProperty("messages").EnumerateArray());
        Assert.Equal("internal.server.error", message.GetProperty("code").GetString());
        Assert.Equal("Internal error", message.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, message.GetProperty("invalidField").ValueKind);
        Assert.DoesNotContain("sensitive detail", root.GetRawText(), StringComparison.Ordinal);
    }

    private sealed class TestLogger : ILogger<ExceptionMiddleware>
    {
        public Exception? Exception { get; private set; }
        public LogLevel Level { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Exception = exception;
        }
    }
}
