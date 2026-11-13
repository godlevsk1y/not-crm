using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Communication;
using DirectoryService.Contracts.Communication.Http;
using DirectoryService.Contracts.Positions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Framework.Results;
using Shared.Kernel.Errors;

namespace DirectoryService.IntegrationTests.Communication;

public class HttpCommunicationServiceTests
{
    [Fact]
    public async Task GetPositionById_ShouldReturnPosition_AndSendGetToConfiguredBaseAddress()
    {
        var position = new PositionDto(Guid.NewGuid(), "Software Engineer");
        using var handler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(new Uri($"https://directory.example/api/positions/{position.Id}"), request.RequestUri);
            Assert.True(cancellationToken.CanBeCanceled);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Envelope.Ok(position)),
            });
        });
        using var provider = CreateServiceProvider(handler);
        var service = provider.GetRequiredService<IDirectoryServiceCommunication>();
        using var cancellation = new CancellationTokenSource();

        var result = await service.GetPositionByIdAsync(position.Id, cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(position, result.Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.OK)]
    public async Task GetPositionById_ShouldPreserveDirectoryServiceError(HttpStatusCode statusCode)
    {
        var positionId = Guid.NewGuid();
        var error = Error.NotFound(new ErrorMessage(
            "position.not.found", $"Position with id '{positionId}' was not found"));
        using var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(Envelope.Failure(error)),
        }));
        using var provider = CreateServiceProvider(handler);
        var service = provider.GetRequiredService<IDirectoryServiceCommunication>();

        var result = await service.GetPositionByIdAsync(positionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error.Type, result.Error.Type);
        Assert.Equal(error.Messages, result.Error.Messages);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"result\":null}")]
    [InlineData("{\"result\":\"invalid\"}")]
    public async Task GetPositionById_ShouldReturnInternalError_WhenResponseBodyIsInvalid(string body)
    {
        using var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        }));
        using var provider = CreateServiceProvider(handler);
        var service = provider.GetRequiredService<IDirectoryServiceCommunication>();

        var result = await service.GetPositionByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(GeneralErrors.Internal().Type, result.Error.Type);
        Assert.Equal(GeneralErrors.Internal().Messages, result.Error.Messages);
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnInternalError_AndLogTransportExceptionWithPositionId()
    {
        var positionId = Guid.NewGuid();
        var exception = new HttpRequestException("Connection failed");
        using var logger = new TestLoggerProvider();
        using var handler = new StubHttpMessageHandler((_, _) => throw exception);
        using var provider = CreateServiceProvider(handler, logger);
        var service = provider.GetRequiredService<IDirectoryServiceCommunication>();

        var result = await service.GetPositionByIdAsync(positionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(GeneralErrors.Internal().Type, result.Error.Type);
        Assert.Equal(GeneralErrors.Internal().Messages, result.Error.Messages);
        Assert.Same(exception, logger.Exception);
        Assert.Equal(LogLevel.Error, logger.Level);
        Assert.Contains(logger.Properties, property => string.Equals(property.Key, "positionId", StringComparison.Ordinal) && Equals(property.Value, positionId));
    }

    [Fact]
    public async Task GetPositionById_ShouldPassCancellationToHttpRequest()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken requestToken = default;
        using var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            requestToken = cancellationToken;
            await cancellation.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The request should have been canceled");
        });
        using var provider = CreateServiceProvider(handler);
        var service = provider.GetRequiredService<IDirectoryServiceCommunication>();

        var result = await service.GetPositionByIdAsync(Guid.NewGuid(), cancellation.Token);

        Assert.True(requestToken.IsCancellationRequested);
        Assert.True(result.IsFailure);
        Assert.Equal(GeneralErrors.Internal().Type, result.Error.Type);
        Assert.Equal(GeneralErrors.Internal().Messages, result.Error.Messages);
    }

    private static ServiceProvider CreateServiceProvider(HttpMessageHandler handler, ILoggerProvider? logger = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["DirectoryServiceOptions:BaseAddress"] = "https://directory.example/api/",
            ["DirectoryServiceOptions:Timeout"] = "00:00:30",
        }).Build();
        var services = new ServiceCollection();
        if (logger is not null)
            services.AddLogging(builder => builder.AddProvider(logger));
        services.AddDirectoryService(configuration);
        services.AddHttpClient(nameof(IDirectoryServiceCommunication))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }

    private sealed class TestLoggerProvider : ILoggerProvider, ILogger
    {
        public Exception? Exception { get; private set; }
        public LogLevel Level { get; private set; }
        public IReadOnlyList<KeyValuePair<string, object?>> Properties { get; private set; } = [];

        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;
        public void Dispose() { }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Exception = exception;
            Properties = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToArray();
        }
    }
}
