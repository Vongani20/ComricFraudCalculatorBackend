using System.Net;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Middleware;

/// <summary>
/// Maps common domain failures to stable HTTP status codes instead of opaque 500s.
/// </summary>
public class ExceptionMappingMiddleware(RequestDelegate next, ILogger<ExceptionMappingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized request to {Path}", context.Request.Path);
            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update failed for {Path}", context.Request.Path);
            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Could not save the event. Check tenant context and required fields.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }
}
