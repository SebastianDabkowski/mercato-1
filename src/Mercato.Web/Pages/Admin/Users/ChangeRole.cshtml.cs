using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Users;

/// <summary>
/// Page model for changing a user's role.
/// </summary>
[Authorize(Roles = "Admin")]
public class ChangeRoleModel : PageModel
{
    private readonly IUserRoleManagementService _userRoleManagementService;
    private readonly ILogger<ChangeRoleModel> _logger;

    public ChangeRoleModel(
        IUserRoleManagementService userRoleManagementService,
        ILogger<ChangeRoleModel> logger)
    {
        _userRoleManagementService = userRoleManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserWithRolesInfo? UserInfo { get; set; }

    /// <summary>
    /// Gets or sets the input model for the role change.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the available roles.
    /// </summary>
    public static IReadOnlyList<string> AvailableRoles => ["Buyer", "Seller", "Admin"];

    /// <summary>
    /// Input model for the role change form.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new role.
        /// </summary>
        [Required(ErrorMessage = "Please select a role.")]
        public string NewRole { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the admin has confirmed the change.
        /// </summary>
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm this role change.")]
        public bool ConfirmChange { get; set; }
    }

    /// <summary>
    /// Handles GET requests to load user information.
    /// </summary>
    /// <param name="userId">The user ID to load.</param>
    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Admin attempted to access ChangeRole without a user ID.");
            return RedirectToPage("Index");
        }

        UserInfo = await _userRoleManagementService.GetUserWithRolesAsync(userId);

        if (UserInfo == null)
        {
            _logger.LogWarning("Admin attempted to access ChangeRole for non-existent user {UserId}.", userId);
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("Index");
        }

        Input.UserId = userId;

        return Page();
    }

    /// <summary>
    /// Handles POST requests to change the user's role.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        // Reload user info for display
        UserInfo = await _userRoleManagementService.GetUserWithRolesAsync(Input.UserId);

        if (UserInfo == null)
        {
            _logger.LogWarning("Admin attempted to change role for non-existent user {UserId}.", Input.UserId);
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("Index");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            _logger.LogError("Could not determine admin user ID.");
            ErrorMessage = "Could not verify your identity. Please log in again.";
            return Page();
        }

        var command = new ChangeUserRoleCommand
        {
            UserId = Input.UserId,
            NewRole = Input.NewRole,
            AdminUserId = adminUserId
        };

        var result = await _userRoleManagementService.ChangeUserRoleAsync(command);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Admin {AdminId} successfully changed role for user {UserId} to {NewRole}.",
                adminUserId,
                Input.UserId,
                Input.NewRole);

            TempData["SuccessMessage"] = $"Successfully changed role for {UserInfo.Email} to {Input.NewRole}.";
            return RedirectToPage("Index");
        }

        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }
}
