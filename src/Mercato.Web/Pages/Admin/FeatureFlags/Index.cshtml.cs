using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for the feature flags index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IFeatureFlagManagementService _featureFlagService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="featureFlagService">The feature flag management service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IFeatureFlagManagementService featureFlagService,
        ILogger<IndexModel> logger)
    {
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of feature flags.
    /// </summary>
    public IReadOnlyList<FeatureFlag> Flags { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected environment filter.
    /// </summary>
    public string? SelectedEnvironment { get; set; }

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
    /// Handles GET requests to the feature flags index page.
    /// </summary>
    /// <param name="environment">Optional environment filter.</param>
    public async Task<IActionResult> OnGetAsync(string? environment = null)
    {
        SelectedEnvironment = environment;

        GetFeatureFlagsResult result;

        if (!string.IsNullOrEmpty(environment) && Enum.TryParse<FeatureFlagEnvironment>(environment, out var env))
        {
            result = await _featureFlagService.GetFlagsByEnvironmentAsync(env);
        }
        else
        {
            result = await _featureFlagService.GetAllFlagsAsync();
        }

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get feature flags: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Flags = result.Flags;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to toggle a feature flag.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    /// <param name="isEnabled">The new enabled state.</param>
    public async Task<IActionResult> OnPostToggleAsync(Guid id, bool isEnabled)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _featureFlagService.ToggleFlagAsync(id, isEnabled, userId, userEmail);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Feature flag '{result.Flag?.Key}' was {(isEnabled ? "enabled" : "disabled")} successfully.";
        return RedirectToPage();
    }
}
