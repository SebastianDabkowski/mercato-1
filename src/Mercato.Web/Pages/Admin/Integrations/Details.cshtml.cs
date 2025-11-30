using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Integrations;

/// <summary>
/// Page model for viewing integration details.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IIntegrationManagementService _integrationService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="integrationService">The integration management service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IIntegrationManagementService integrationService,
        ILogger<DetailsModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the integration.
    /// </summary>
    public Integration Integration { get; set; } = null!;

    /// <summary>
    /// Handles GET requests to the details page.
    /// </summary>
    /// <param name="id">The integration ID to view.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _integrationService.GetIntegrationByIdAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        if (result.Integration == null)
        {
            return NotFound();
        }

        Integration = result.Integration;
        return Page();
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
