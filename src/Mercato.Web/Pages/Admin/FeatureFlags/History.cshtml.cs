using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for viewing feature flag history.
/// </summary>
[Authorize(Roles = "Admin")]
public class HistoryModel : PageModel
{
    private readonly IFeatureFlagManagementService _featureFlagService;
    private readonly ILogger<HistoryModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryModel"/> class.
    /// </summary>
    /// <param name="featureFlagService">The feature flag management service.</param>
    /// <param name="logger">The logger.</param>
    public HistoryModel(
        IFeatureFlagManagementService featureFlagService,
        ILogger<HistoryModel> logger)
    {
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the feature flag.
    /// </summary>
    public FeatureFlag? Flag { get; set; }

    /// <summary>
    /// Gets or sets the history records.
    /// </summary>
    public IReadOnlyList<FeatureFlagHistory> History { get; set; } = [];

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the history page.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _featureFlagService.GetFlagHistoryAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get feature flag history: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        Flag = result.Flag;
        History = result.History;

        return Page();
    }
}
