using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mercato.Web.Filters;

/// <summary>
/// Action filter for request validation across the application.
/// Provides a centralized location for validating incoming requests before they reach the action.
/// Currently implements logging-only validation; to be extended with blocking behavior as needed.
/// </summary>
public class RequestValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<RequestValidationFilter> _logger;

    public RequestValidationFilter(ILogger<RequestValidationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // TODO: Implement custom validation logic beyond model state validation
        // TODO: Add cross-field validation support
        // TODO: Implement request rate limiting checks
        // TODO: Add request size validation
        // TODO: Implement anti-forgery token validation for sensitive operations
        // TODO: Add input sanitization for XSS prevention
        // TODO: Implement IP-based validation rules

        // Log model state validation errors for monitoring and debugging.
        // Note: This filter intentionally does NOT block requests with invalid model state,
        // as model validation is already handled by ASP.NET Core's built-in model binding.
        // This hook is for centralized logging and can be extended to add custom validation.
        if (!context.ModelState.IsValid)
        {
            _logger.LogWarning("Request validation failed for {ActionName}. Errors: {Errors}",
                context.ActionDescriptor.DisplayName,
                string.Join("; ", context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));

            // TODO: Implement custom blocking behavior if needed (e.g., for specific validation rules)
            // Example: context.Result = new BadRequestObjectResult(new { Errors = ... });
            // return;
        }
        else
        {
            _logger.LogDebug("Request validation passed for {ActionName}", context.ActionDescriptor.DisplayName);
        }

        // Execute the action
        await next();
    }
}

/// <summary>
/// Extension methods for registering the RequestValidationFilter.
/// </summary>
public static class RequestValidationFilterExtensions
{
    /// <summary>
    /// Adds the request validation filter globally to all controllers and pages.
    /// </summary>
    /// <param name="options">The MVC options to configure.</param>
    /// <returns>The MVC options for chaining.</returns>
    public static void AddRequestValidationFilter(this MvcOptions options)
    {
        // TODO: Consider conditional registration based on configuration
        options.Filters.Add<RequestValidationFilter>();
    }
}
