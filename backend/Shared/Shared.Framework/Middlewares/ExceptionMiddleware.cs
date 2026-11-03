using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Errors;

namespace Shared.Framework.Middlewares;

public partial class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
        }
        catch (Exception ex)
        {
            context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
            {
                Error = ex,
                Path = context.Request.Path.ToString(),
            });
            LogException(ex);

            if (context.Response.HasStarted)
            {
                context.Abort();
                return;
            }

            await HandleExceptionAsync(context);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var error = GeneralErrors.Internal();
        
        await context.Response.WriteAsJsonAsync(error, cancellationToken: context.RequestAborted);
    }

    [LoggerMessage(
        Level = LogLevel.Error, 
        Message = "An exception occurred while processing the request")]
    private partial void LogException(Exception ex);
}
