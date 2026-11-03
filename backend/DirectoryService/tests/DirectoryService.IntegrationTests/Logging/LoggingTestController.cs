using System.Diagnostics.CodeAnalysis;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Database;
using Shared.Framework.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace DirectoryService.IntegrationTests.Logging;

[ApiController]
[Route("__logging")]
public class LoggingTestController : ControllerBase
{
    [HttpGet("exception")]
    [SuppressMessage("Performance", "CA1822", Justification = "MVC actions must be instance methods.")]
    public IResult Exception() => throw new InvalidOperationException("Unexpected request failure");

    [HttpPost("database-conflict")]
    public async Task<IResult> DatabaseConflict(
        [FromServices] DirectoryServiceDbContext context,
        [FromServices] ITransactionManager transactionManager)
    {
        var address = Address.Create("Russia", "Moscow", "Moscow", null, "Tverskaya", "1", "125009").Value;
        var name = LocationName.Create("Duplicate office").Value;
        context.Locations.AddRange(Location.Create(name, address), Location.Create(name, address));
        var result = await transactionManager.SaveChangesAsync(HttpContext.RequestAborted);
        return result.IsFailure ? EndpointResults.Error(result.Error) : EndpointResults.NoContent();
    }

    [HttpGet("transaction-exception")]
    public async Task<IResult> TransactionException([FromServices] ITransactionManager transactionManager)
    {
        var result = await transactionManager.ExecuteAsync<int>(
            _ => throw new InvalidOperationException("Unexpected transaction failure"),
            HttpContext.RequestAborted);

        return result.IsFailure ? EndpointResults.Error(result.Error) : EndpointResults.Ok(result.Value);
    }

    [HttpGet("cancelled")]
    public IResult Cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        HttpContext.RequestAborted = cancellation.Token;
        throw new OperationCanceledException(cancellation.Token);
    }

    [HttpGet("transaction-cancelled")]
    public async Task<IResult> TransactionCancelled([FromServices] ITransactionManager transactionManager)
    {
        using var cancellation = new CancellationTokenSource();
        HttpContext.RequestAborted = cancellation.Token;
        var result = await transactionManager.ExecuteAsync<int>(async _ =>
        {
            await cancellation.CancelAsync();
            throw new OperationCanceledException(cancellation.Token);
        }, cancellation.Token);

        return result.IsFailure ? EndpointResults.Error(result.Error) : EndpointResults.Ok(result.Value);
    }

    [HttpGet("unrelated-cancellation")]
    [SuppressMessage("Performance", "CA1822", Justification = "MVC actions must be instance methods.")]
    public IResult UnrelatedCancellation() => throw new OperationCanceledException("Internal timeout");

    [HttpGet("started-response")]
    public async Task StartedResponse()
    {
        await Response.WriteAsync("Partial response", HttpContext.RequestAborted);
        throw new InvalidOperationException("Failure after response started");
    }

    [HttpGet("started-cancelled")]
    public async Task StartedCancelled()
    {
        await Response.WriteAsync("Partial response", HttpContext.RequestAborted);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        HttpContext.RequestAborted = cancellation.Token;
        throw new OperationCanceledException(cancellation.Token);
    }
}
