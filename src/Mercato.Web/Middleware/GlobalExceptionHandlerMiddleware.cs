using System.Net;
using System.Text.Json;

namespace Mercato.Web.Middleware;

/// <summary>
/// Middleware for centralized exception handling across the application.
/// Catches unhandled exceptions and returns a consistent error response.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // TODO: Implement detailed exception categorization (e.g., validation, not found, authorization, etc.)
        // TODO: Add correlation ID tracking for distributed tracing
        // TODO: Implement exception-specific response messages based on exception types
        // TODO: Add integration with external error monitoring services (e.g., Application Insights, Sentry)
        // TODO: Implement PII scrubbing from error logs for GDPR compliance

        _logger.LogError(exception, "An unhandled exception occurred while processing the request. Path: {Path}, Method: {Method}",
            context.Request.Path,
            context.Request.Method);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An unexpected error occurred. Please try again later.",
            // TODO: Include request correlation ID for support inquiries
            // TODO: In development, consider including exception details
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Extension methods for registering the GlobalExceptionHandlerMiddleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handler middleware to the application pipeline.
    /// Should be added early in the pipeline to catch exceptions from all downstream middleware.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
