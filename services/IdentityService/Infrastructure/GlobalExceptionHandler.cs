using IdentityService.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace IdentityService.Infrastructure;

/// <summary>Global exception middleware: every failure becomes the standard envelope. Details are never returned.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is BadHttpRequestException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(Api.Fail("Invalid request."), ct);
            return true;
        }

        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path.Value);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(Api.Fail("An unexpected error occurred."), ct);
        return true;
    }
}
