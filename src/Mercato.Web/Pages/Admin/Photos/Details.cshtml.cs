using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Photos;

/// <summary>
/// Page model for viewing and moderating individual photo details.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IPhotoModerationService _photoModerationService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="photoModerationService">The photo moderation service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IPhotoModerationService photoModerationService,
        ILogger<DetailsModel> logger)
    {
        _photoModerationService = photoModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the photo details.
    /// </summary>
    public PhotoModerationDetails? PhotoDetails { get; private set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the removal reason.
    /// </summary>
    [BindProperty]
    public string? RemovalReason { get; set; }

    /// <summary>
    /// Gets or sets the approval reason.
    /// </summary>
    [BindProperty]
    public string? ApprovalReason { get; set; }

    /// <summary>
    /// Handles GET requests to load photo details.
    /// </summary>
    /// <param name="id">The image ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _photoModerationService.GetPhotoDetailsAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        PhotoDetails = result.PhotoDetails;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to approve a photo.
    /// </summary>
    /// <param name="id">The image ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to approve photo {ImageId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new ApprovePhotoCommand
        {
            ImageId = id,
            AdminUserId = userId,
            Reason = ApprovalReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _photoModerationService.ApprovePhotoAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Photo has been approved and remains visible on the product page.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Handles POST requests to remove a photo.
    /// </summary>
    /// <param name="id">The image ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostRemoveAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RemovalReason))
        {
            ErrorMessage = "Removal reason is required.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to remove photo {ImageId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new RemovePhotoCommand
        {
            ImageId = id,
            AdminUserId = userId,
            Reason = RemovalReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _photoModerationService.RemovePhotoAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Photo has been removed. The seller has been notified with the reason.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Gets the CSS class for a moderation status badge.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetModerationStatusBadgeClass(PhotoModerationStatus status) =>
        PhotoModerationDisplayHelpers.GetModerationStatusBadgeClass(status);

    /// <summary>
    /// Gets the display text for a moderation status.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The display text.</returns>
    public static string GetModerationStatusDisplayText(PhotoModerationStatus status) =>
        PhotoModerationDisplayHelpers.GetModerationStatusDisplayText(status);

    /// <summary>
    /// Formats file size to human-readable format.
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>Human-readable file size.</returns>
    public static string FormatFileSize(long bytes) =>
        PhotoModerationDisplayHelpers.FormatFileSize(bytes);
}
