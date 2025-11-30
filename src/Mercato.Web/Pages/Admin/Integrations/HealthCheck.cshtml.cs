using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Integrations;

/// <summary>
/// Page model for running integration health checks.
/// </summary>
[Authorize(Roles = "Admin")]
public class HealthCheckModel : PageModel
{
    private readonly IIntegrationManagementService _integrationService;
    private readonly ILogger<HealthCheckModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckModel"/> class.
    /// </summary>
    /// <param name="integrationService">The integration management service.</param>
    /// <param name="logger">The logger.</param>
    public HealthCheckModel(
        IIntegrationManagementService integrationService,
        ILogger<HealthCheckModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the integration.
    /// </summary>
    public Integration Integration { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether a health check was just performed.
    /// </summary>
    public bool HealthCheckPerformed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the health check was healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the health check message.
    /// </summary>
    public string? HealthCheckMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the health check page.
    /// </summary>
    /// <param name="id">The integration ID.</param>
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
    /// Handles POST requests to run a health check.
    /// </summary>
    /// <param name="id">The integration ID.</param>
    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var result = await _integrationService.TestConnectionAsync(id, userId);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            // Get the integration for display even if health check failed
            var getResult = await _integrationService.GetIntegrationByIdAsync(id);
            if (getResult.Succeeded && getResult.Integration != null)
            {
                Integration = getResult.Integration;
            }
            else
            {
                return NotFound();
            }

            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        if (result.Integration == null)
        {
            return NotFound();
        }

        Integration = result.Integration;
        HealthCheckPerformed = true;
        IsHealthy = result.IsHealthy;
        HealthCheckMessage = result.Message;

        _logger.LogInformation(
            "Health check performed for integration '{Name}': {Status}",
            Integration.Name,
            IsHealthy ? "Healthy" : "Unhealthy");

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
}
