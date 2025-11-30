using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Users;

/// <summary>
/// Page model for viewing detailed user information.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IUserAccountManagementService _userAccountManagementService;
    private readonly ISensitiveAccessAuditService _sensitiveAccessAuditService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IUserAccountManagementService userAccountManagementService,
        ISensitiveAccessAuditService sensitiveAccessAuditService,
        ILogger<DetailsModel> logger)
    {
        _userAccountManagementService = userAccountManagementService;
        _sensitiveAccessAuditService = sensitiveAccessAuditService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the detailed user information.
    /// </summary>
    public UserDetailInfo? UserDetail { get; set; }

    /// <summary>
    /// Handles GET requests to load user details.
    /// </summary>
    /// <param name="userId">The user ID to load.</param>
    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Admin attempted to access user details without a user ID.");
            return RedirectToPage("Index");
        }

        UserDetail = await _userAccountManagementService.GetUserDetailAsync(userId);

        if (UserDetail == null)
        {
            _logger.LogWarning("Admin attempted to access details for non-existent user {UserId}.", userId);
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("Index");
        }

        // Log sensitive access for viewing customer profile
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(adminUserId))
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _sensitiveAccessAuditService.LogCustomerProfileAccessAsync(
                adminUserId,
                userId,
                ipAddress);
        }

        _logger.LogInformation("Admin viewing details for user {UserId} ({Email}).", userId, UserDetail.Email);

        return Page();
    }
}
