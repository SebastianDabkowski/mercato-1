using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for GDPR account deletion functionality.
/// Allows users to delete their account with proper anonymization.
/// </summary>
[Authorize]
public class DeleteAccountModel : PageModel
{
    private readonly IAccountDeletionService _accountDeletionService;
    private readonly IAccountDeletionCheckService _accountDeletionCheckService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ILogger<DeleteAccountModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAccountModel"/> class.
    /// </summary>
    /// <param name="accountDeletionService">The account deletion service.</param>
    /// <param name="accountDeletionCheckService">The account deletion check service.</param>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="signInManager">The ASP.NET Core Identity sign-in manager.</param>
    /// <param name="adminAuditRepository">The admin audit repository for logging.</param>
    /// <param name="logger">The logger.</param>
    public DeleteAccountModel(
        IAccountDeletionService accountDeletionService,
        IAccountDeletionCheckService accountDeletionCheckService,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IAdminAuditRepository adminAuditRepository,
        ILogger<DeleteAccountModel> logger)
    {
        _accountDeletionService = accountDeletionService;
        _accountDeletionCheckService = accountDeletionCheckService;
        _userManager = userManager;
        _signInManager = signInManager;
        _adminAuditRepository = adminAuditRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets a status message to display.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status message is an error.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can delete their account.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Gets or sets the list of conditions blocking account deletion.
    /// </summary>
    public IReadOnlyList<string> BlockingConditions { get; set; } = [];

    /// <summary>
    /// Gets or sets information about the impact of deleting the account.
    /// </summary>
    public AccountDeletionImpactInfo? ImpactInfo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the confirmation checkbox was checked.
    /// </summary>
    [BindProperty]
    public bool ConfirmDeletion { get; set; }

    /// <summary>
    /// Handles the GET request for the delete account page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Check for blocking conditions
        var checkResult = await _accountDeletionCheckService.CheckBlockingConditionsAsync(userId);
        CanDelete = checkResult.CanDelete;
        BlockingConditions = checkResult.BlockingConditions;

        // Get deletion impact info
        ImpactInfo = await _accountDeletionService.GetDeletionImpactAsync(userId);

        // Check for status message from TempData
        if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
            IsError = TempData["IsError"] as bool? ?? false;
        }

        return Page();
    }

    /// <summary>
    /// Handles the POST request to delete the user account.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Verify user confirmed deletion
        if (!ConfirmDeletion)
        {
            TempData["StatusMessage"] = "Please confirm that you understand the consequences of deleting your account.";
            TempData["IsError"] = true;
            return RedirectToPage();
        }

        var userEmail = User.Identity?.Name ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation("User {UserId} requested account deletion", userId);

        // Perform the deletion
        var result = await _accountDeletionService.DeleteAccountAsync(userId, userId);

        if (!result.Succeeded)
        {
            if (result.IsBlocked)
            {
                _logger.LogWarning(
                    "Account deletion blocked for user {UserId}: {Conditions}",
                    userId,
                    string.Join(", ", result.BlockingConditions));

                TempData["StatusMessage"] = "Account deletion is not possible at this time. Please resolve the following issues first.";
                TempData["IsError"] = true;
                return RedirectToPage();
            }

            _logger.LogWarning(
                "Account deletion failed for user {UserId}: {Errors}",
                userId,
                string.Join(", ", result.Errors));

            TempData["StatusMessage"] = "An error occurred while deleting your account. Please try again.";
            TempData["IsError"] = true;
            return RedirectToPage();
        }

        // Log the account deletion for audit (without exposing personal data)
        await _adminAuditRepository.AddAsync(new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = userId, // Self-service deletion
            Action = "AccountDeletion",
            EntityType = "User",
            EntityId = userId,
            Details = "GDPR account deletion completed. Personal data anonymized.",
            Timestamp = result.DeletedAt ?? DateTimeOffset.UtcNow,
            IpAddress = ipAddress
        });

        _logger.LogInformation("Account successfully deleted for user {UserId}", userId);

        // Sign out the user
        await _signInManager.SignOutAsync();

        // Redirect to a confirmation page
        return RedirectToPage("/Account/DeleteAccountConfirmation");
    }
}
