using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for viewing data processing activity details.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IDataProcessingRegistryService _registryService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="registryService">The data processing registry service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IDataProcessingRegistryService registryService,
        ILogger<DetailsModel> logger)
    {
        _registryService = registryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the data processing activity.
    /// </summary>
    public DataProcessingActivity? Activity { get; set; }

    /// <summary>
    /// Gets or sets the change history.
    /// </summary>
    public IReadOnlyList<DataProcessingActivityHistory> History { get; set; } = [];

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
    /// Handles GET requests.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var activityResult = await _registryService.GetActivityByIdAsync(id);

        if (!activityResult.Succeeded || activityResult.Activity == null)
        {
            if (activityResult.IsNotAuthorized)
            {
                return Forbid();
            }

            return NotFound();
        }

        Activity = activityResult.Activity;

        var historyResult = await _registryService.GetActivityHistoryAsync(id);
        if (historyResult.Succeeded)
        {
            History = historyResult.History;
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to deactivate a processing activity.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="reason">The reason for deactivation.</param>
    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, string? reason)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var result = await _registryService.DeactivateActivityAsync(id, userId, reason);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage(new { id });
        }

        _logger.LogInformation(
            "Deactivated data processing activity {ActivityId} by user {UserId}",
            id,
            userId);

        TempData["SuccessMessage"] = "Processing activity deactivated successfully.";
        return RedirectToPage(new { id });
    }
}
