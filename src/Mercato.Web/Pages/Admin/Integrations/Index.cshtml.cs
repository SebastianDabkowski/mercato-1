using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Integrations;

/// <summary>
/// Page model for the integrations index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IIntegrationManagementService _integrationService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="integrationService">The integration management service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IIntegrationManagementService integrationService,
        ILogger<IndexModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of integrations.
    /// </summary>
    public IReadOnlyList<Integration> Integrations { get; set; } = [];

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the integrations index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var result = await _integrationService.GetAllIntegrationsAsync();

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get integrations: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Integrations = result.Integrations;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to enable an integration.
    /// </summary>
    /// <param name="id">The integration ID to enable.</param>
    public async Task<IActionResult> OnPostEnableAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _integrationService.EnableIntegrationAsync(id, userId, userEmail);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Integration '{result.Integration?.Name}' has been enabled.";
        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST requests to disable an integration.
    /// </summary>
    /// <param name="id">The integration ID to disable.</param>
    public async Task<IActionResult> OnPostDisableAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _integrationService.DisableIntegrationAsync(id, userId, userEmail);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Integration '{result.Integration?.Name}' has been disabled.";
        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST requests to delete an integration.
    /// </summary>
    /// <param name="id">The integration ID to delete.</param>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var result = await _integrationService.DeleteIntegrationAsync(id, userId);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = "Integration has been deleted.";
        return RedirectToPage();
    }

    /// <summary>
    /// Gets the CSS class for the status badge.
    /// </summary>
    /// <param name="status">The integration status.</param>
    /// <returns>The Bootstrap badge class.</returns>
    public static string GetStatusBadgeClass(IntegrationStatus status)
    {
        return status switch
        {
            IntegrationStatus.Active => "bg-success",
            IntegrationStatus.Inactive => "bg-secondary",
            IntegrationStatus.Error => "bg-danger",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Gets the display text for the integration type.
    /// </summary>
    /// <param name="type">The integration type.</param>
    /// <returns>The display text.</returns>
    public static string GetTypeDisplayText(IntegrationType type)
    {
        return type switch
        {
            IntegrationType.Payment => "Payment",
            IntegrationType.Shipping => "Shipping",
            IntegrationType.ERP => "ERP",
            IntegrationType.Other => "Other",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the CSS class for the type badge.
    /// </summary>
    /// <param name="type">The integration type.</param>
    /// <returns>The Bootstrap badge class.</returns>
    public static string GetTypeBadgeClass(IntegrationType type)
    {
        return type switch
        {
            IntegrationType.Payment => "bg-primary",
            IntegrationType.Shipping => "bg-info",
            IntegrationType.ERP => "bg-warning text-dark",
            IntegrationType.Other => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Gets the display text for the environment.
    /// </summary>
    /// <param name="environment">The integration environment.</param>
    /// <returns>The display text.</returns>
    public static string GetEnvironmentDisplayText(IntegrationEnvironment environment)
    {
        return environment switch
        {
            IntegrationEnvironment.Sandbox => "Sandbox",
            IntegrationEnvironment.Production => "Production",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the CSS class for the environment badge.
    /// </summary>
    /// <param name="environment">The integration environment.</param>
    /// <returns>The Bootstrap badge class.</returns>
    public static string GetEnvironmentBadgeClass(IntegrationEnvironment environment)
    {
        return environment switch
        {
            IntegrationEnvironment.Sandbox => "bg-warning text-dark",
            IntegrationEnvironment.Production => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
