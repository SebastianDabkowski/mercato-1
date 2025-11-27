using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for changing a user's password from account settings.
/// Available to both Buyers and Sellers.
/// </summary>
[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly IPasswordChangeService _passwordChangeService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(
        IPasswordChangeService passwordChangeService,
        UserManager<IdentityUser> userManager,
        ILogger<ChangePasswordModel> logger)
    {
        _passwordChangeService = passwordChangeService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the change password input model.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets a status message to display.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status message is an error.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has a password set.
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// Input model for the change password form.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the user's current password.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Current password is required.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "New password is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confirmation of the new password.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Confirm password is required.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        HasPassword = await _passwordChangeService.HasPasswordAsync(userId);

        // Check for status message from TempData
        if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
            IsError = TempData["IsError"] as bool? ?? false;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        HasPassword = await _passwordChangeService.HasPasswordAsync(userId);

        if (!HasPassword)
        {
            // User doesn't have a password set (registered via social login)
            StatusMessage = "You cannot change your password because you don't have one set. You registered using a social login provider.";
            IsError = true;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = Input.CurrentPassword,
            NewPassword = Input.NewPassword,
            ConfirmPassword = Input.ConfirmPassword
        };

        var result = await _passwordChangeService.ChangePasswordAsync(command);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} changed their password successfully.", userId);
            TempData["StatusMessage"] = "Your password has been changed successfully.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        // Handle specific error cases
        if (result.IsIncorrectCurrentPassword)
        {
            _logger.LogWarning("User {UserId} entered incorrect current password.", userId);
            ModelState.AddModelError(nameof(Input.CurrentPassword), "The current password is incorrect.");
        }
        else if (result.IsUserNotFound)
        {
            // This shouldn't happen for authenticated users, but handle it anyway
            _logger.LogWarning("Password change attempted for non-existent user: {UserId}", userId);
            ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
        }
        else
        {
            // Add all errors to ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        IsError = true;
        return Page();
    }
}
