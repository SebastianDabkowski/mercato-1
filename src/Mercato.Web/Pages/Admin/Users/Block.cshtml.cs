using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Users;

/// <summary>
/// Page model for blocking a user account.
/// </summary>
[Authorize(Roles = "Admin")]
public class BlockModel : PageModel
{
    private readonly IUserAccountManagementService _userAccountManagementService;
    private readonly ILogger<BlockModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockModel"/> class.
    /// </summary>
    /// <param name="userAccountManagementService">The user account management service.</param>
    /// <param name="logger">The logger.</param>
    public BlockModel(
        IUserAccountManagementService userAccountManagementService,
        ILogger<BlockModel> logger)
    {
        _userAccountManagementService = userAccountManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserDetailInfo? UserInfo { get; set; }

    /// <summary>
    /// Gets or sets the input model for the block operation.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Input model for the block user form.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the block reason.
        /// </summary>
        [Required(ErrorMessage = "Please select a reason for blocking.")]
        public BlockReason Reason { get; set; }

        /// <summary>
        /// Gets or sets optional additional details about the block reason.
        /// </summary>
        [MaxLength(2000, ErrorMessage = "Additional details must be at most 2000 characters.")]
        public string? ReasonDetails { get; set; }

        /// <summary>
        /// Gets or sets whether the admin has confirmed the block.
        /// </summary>
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm this block action.")]
        public bool ConfirmBlock { get; set; }
    }

    /// <summary>
    /// Handles GET requests to load user information.
    /// </summary>
    /// <param name="userId">The user ID to load.</param>
    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Admin attempted to access Block page without a user ID.");
            return RedirectToPage("Index");
        }

        UserInfo = await _userAccountManagementService.GetUserDetailAsync(userId);

        if (UserInfo == null)
        {
            _logger.LogWarning("Admin attempted to access Block page for non-existent user {UserId}.", userId);
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("Index");
        }

        if (UserInfo.IsBlocked)
        {
            _logger.LogWarning("Admin attempted to block already blocked user {UserId}.", userId);
            TempData["ErrorMessage"] = "User is already blocked.";
            return RedirectToPage("Details", new { userId });
        }

        Input.UserId = userId;

        return Page();
    }

    /// <summary>
    /// Handles POST requests to block the user.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        // Reload user info for display
        UserInfo = await _userAccountManagementService.GetUserDetailAsync(Input.UserId);

        if (UserInfo == null)
        {
            _logger.LogWarning("Admin attempted to block non-existent user {UserId}.", Input.UserId);
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("Index");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var adminEmail = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(adminUserId) || string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogError("Could not determine admin user ID or email.");
            ErrorMessage = "Could not verify your identity. Please log in again.";
            return Page();
        }

        var command = new BlockUserCommand
        {
            UserId = Input.UserId,
            AdminUserId = adminUserId,
            AdminEmail = adminEmail,
            Reason = Input.Reason,
            ReasonDetails = Input.ReasonDetails
        };

        var result = await _userAccountManagementService.BlockUserAsync(command);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Admin {AdminId} successfully blocked user {UserId} for reason {Reason}.",
                adminUserId,
                Input.UserId,
                Input.Reason);

            TempData["SuccessMessage"] = $"Successfully blocked user {UserInfo.Email}.";
            return RedirectToPage("Details", new { userId = Input.UserId });
        }

        if (result.IsNotAuthorized)
        {
            return Forbid();
        }

        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }
}
