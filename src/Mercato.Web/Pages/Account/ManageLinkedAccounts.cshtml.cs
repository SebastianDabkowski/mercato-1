using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for managing linked social accounts for buyers.
/// </summary>
[Authorize(Roles = "Buyer")]
public class ManageLinkedAccountsModel : PageModel
{
    private readonly IAccountLinkingService _accountLinkingService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ManageLinkedAccountsModel> _logger;

    public ManageLinkedAccountsModel(
        IAccountLinkingService accountLinkingService,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration,
        ILogger<ManageLinkedAccountsModel> logger)
    {
        _accountLinkingService = accountLinkingService;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of currently linked accounts.
    /// </summary>
    public IReadOnlyList<LinkedAccountInfo> LinkedAccounts { get; set; } = Array.Empty<LinkedAccountInfo>();

    /// <summary>
    /// Gets or sets a status message to display.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status message is an error.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Gets a value indicating whether Google login is available.
    /// </summary>
    public bool IsGoogleLoginEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Facebook login is available.
    /// </summary>
    public bool IsFacebookLoginEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Google is already linked.
    /// </summary>
    public bool IsGoogleLinked { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Facebook is already linked.
    /// </summary>
    public bool IsFacebookLinked { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? message = null, bool? success = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadLinkedAccountsAsync(userId);
        
        // Check for status message from TempData or query string
        if (!string.IsNullOrEmpty(message))
        {
            StatusMessage = message;
            IsError = success != true;
        }
        else if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
            IsError = TempData["IsError"] as bool? ?? false;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUnlinkAsync(string provider)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrEmpty(provider))
        {
            TempData["StatusMessage"] = "Invalid provider specified.";
            TempData["IsError"] = true;
            return RedirectToPage();
        }

        var result = await _accountLinkingService.UnlinkAccountAsync(userId, provider);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} unlinked {Provider} account.", userId, provider);
            TempData["StatusMessage"] = $"Successfully unlinked your {provider} account.";
            TempData["IsError"] = false;
        }
        else
        {
            _logger.LogWarning("Failed to unlink {Provider} for user {UserId}: {Error}", provider, userId, result.ErrorMessage);
            TempData["StatusMessage"] = result.ErrorMessage ?? $"Failed to unlink {provider} account.";
            TempData["IsError"] = true;
        }

        return RedirectToPage();
    }

    private async Task LoadLinkedAccountsAsync(string userId)
    {
        LinkedAccounts = await _accountLinkingService.GetLinkedAccountsAsync(userId);

        IsGoogleLinked = LinkedAccounts.Any(a => a.ProviderName == "Google");
        IsFacebookLinked = LinkedAccounts.Any(a => a.ProviderName == "Facebook");

        // Check if providers are configured
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        IsGoogleLoginEnabled = !string.IsNullOrEmpty(googleClientId);

        var facebookAppId = _configuration["Authentication:Facebook:AppId"];
        IsFacebookLoginEnabled = !string.IsNullOrEmpty(facebookAppId);
    }
}
