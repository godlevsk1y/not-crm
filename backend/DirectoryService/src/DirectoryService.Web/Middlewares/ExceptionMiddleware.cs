using DirectoryService.Shared.Errors;

namespace DirectoryService.Web.Middlewares;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        LogException(ex);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var error = GeneralErrors.Internal();
        
        await context.Response.WriteAsJsonAsync(error, cancellationToken: context.RequestAborted);
    }

    [LoggerMessage(
        Level = LogLevel.Error, 
        Message = "An exception occurred while processing the request.")]
    private partial void LogException(Exception ex);
}