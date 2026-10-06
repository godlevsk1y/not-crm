using Microsoft.AspNetCore.Builder;

namespace Shared.Framework.Middlewares;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this WebApplication app)
        => app.UseMiddleware<ExceptionMiddleware>();
}