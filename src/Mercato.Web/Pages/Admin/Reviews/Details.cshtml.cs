using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Reviews;

/// <summary>
/// Page model for viewing and moderating individual review details.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IReviewModerationService _reviewModerationService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="reviewModerationService">The review moderation service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IReviewModerationService reviewModerationService,
        ILogger<DetailsModel> logger)
    {
        _reviewModerationService = reviewModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the review details.
    /// </summary>
    public AdminReviewDetails? ReviewDetails { get; private set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the new status for moderation.
    /// </summary>
    [BindProperty]
    public ReviewStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the moderation reason.
    /// </summary>
    [BindProperty]
    public string? ModerationReason { get; set; }

    /// <summary>
    /// Gets or sets the flag reason.
    /// </summary>
    [BindProperty]
    public string? FlagReason { get; set; }

    /// <summary>
    /// Handles GET requests to load review details.
    /// </summary>
    /// <param name="id">The review ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _reviewModerationService.GetReviewDetailsAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        ReviewDetails = result.ReviewDetails;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to moderate a review.
    /// </summary>
    /// <param name="id">The review ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostModerateAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (!NewStatus.HasValue)
        {
            ErrorMessage = "New status is required.";
            return await OnGetAsync(id);
        }

        if (string.IsNullOrWhiteSpace(ModerationReason))
        {
            ErrorMessage = "Moderation reason is required.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to moderate review {ReviewId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new ModerateReviewCommand
        {
            ReviewId = id,
            AdminUserId = userId,
            NewStatus = NewStatus.Value,
            ModerationReason = ModerationReason
        };

        var result = await _reviewModerationService.ModerateReviewAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = $"Review status changed to {NewStatus}.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Handles POST requests to flag a review.
    /// </summary>
    /// <param name="id">The review ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostFlagAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(FlagReason))
        {
            ErrorMessage = "Flag reason is required.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to flag review {ReviewId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new FlagReviewCommand
        {
            ReviewId = id,
            AdminUserId = userId,
            FlagReason = FlagReason
        };

        var result = await _reviewModerationService.FlagReviewAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Review has been flagged for moderation.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Gets the CSS class for a review status badge.
    /// </summary>
    /// <param name="status">The review status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "bg-warning text-dark",
        ReviewStatus.Published => "bg-success",
        ReviewStatus.Hidden => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a review status.
    /// </summary>
    /// <param name="status">The review status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "Pending",
        ReviewStatus.Published => "Published",
        ReviewStatus.Hidden => "Hidden",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets a star rating display as HTML.
    /// </summary>
    /// <param name="rating">The rating from 1 to 5.</param>
    /// <returns>The star display string.</returns>
    public static string GetStarRating(int rating)
    {
        var filled = new string('★', rating);
        var empty = new string('☆', 5 - rating);
        return filled + empty;
    }

    /// <summary>
    /// Gets all available review statuses for moderation.
    /// </summary>
    public static IEnumerable<ReviewStatus> AllStatuses => Enum.GetValues<ReviewStatus>();
}
