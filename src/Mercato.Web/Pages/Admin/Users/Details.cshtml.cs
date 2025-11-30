using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Users;

/// <summary>
/// Page model for viewing detailed user information.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IUserAccountManagementService _userAccountManagementService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IUserAccountManagementService userAccountManagementService,
        ILogger<DetailsModel> logger)
    {
        _userAccountManagementService = userAccountManagementService;
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

        _logger.LogInformation("Admin viewing details for user {UserId} ({Email}).", userId, UserDetail.Email);

        return Page();
    }
}
