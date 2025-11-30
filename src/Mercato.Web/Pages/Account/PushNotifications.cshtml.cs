using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for managing push notification preferences.
/// </summary>
[Authorize]
public class PushNotificationsModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushNotificationsModel"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public PushNotificationsModel(
        IConfiguration configuration,
        ILogger<PushNotificationsModel> logger)
    {
        _configuration = configuration;
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
    /// Gets the VAPID public key for push notifications.
    /// </summary>
    public string VapidPublicKey => _configuration["PushNotifications:VapidPublicKey"] ?? string.Empty;

    /// <summary>
    /// Handles the GET request for the push notifications page.
    /// </summary>
    /// <returns>The page result.</returns>
    public IActionResult OnGet()
    {
        // Check for status message from TempData
        if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
            IsError = TempData["IsError"] as bool? ?? false;
        }

        return Page();
    }
}
