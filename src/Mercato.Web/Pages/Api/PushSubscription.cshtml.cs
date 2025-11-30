using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Api;

/// <summary>
/// API endpoint for managing push notification subscriptions.
/// </summary>
[Authorize]
public class PushSubscriptionModel : PageModel
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<PushSubscriptionModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushSubscriptionModel"/> class.
    /// </summary>
    /// <param name="pushNotificationService">The push notification service.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger.</param>
    public PushSubscriptionModel(
        IPushNotificationService pushNotificationService,
        UserManager<IdentityUser> userManager,
        ILogger<PushSubscriptionModel> logger)
    {
        _pushNotificationService = pushNotificationService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current subscription status for the user.
    /// </summary>
    /// <returns>A JSON result with the subscription status.</returns>
    public async Task<IActionResult> OnGetStatusAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { isSubscribed = false, subscriptionCount = 0 });
        }

        var result = await _pushNotificationService.GetSubscriptionStatusAsync(userId);
        if (!result.Succeeded)
        {
            return new JsonResult(new { isSubscribed = false, subscriptionCount = 0 });
        }

        return new JsonResult(new
        {
            isSubscribed = result.IsSubscribed,
            subscriptionCount = result.SubscriptionCount
        });
    }

    /// <summary>
    /// Subscribes the user's device to push notifications.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <returns>A JSON result indicating success or failure.</returns>
    public async Task<IActionResult> OnPostSubscribeAsync([FromBody] SubscribeRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return new UnauthorizedResult();
        }

        if (request == null)
        {
            return BadRequest(new { succeeded = false, error = "Invalid request." });
        }

        var command = new SubscribePushCommand
        {
            UserId = userId,
            Endpoint = request.Endpoint ?? string.Empty,
            P256DH = request.P256dh ?? string.Empty,
            Auth = request.Auth ?? string.Empty
        };

        var result = await _pushNotificationService.SubscribeAsync(command);

        if (result.IsNotAuthorized)
        {
            return Forbid();
        }

        if (!result.Succeeded)
        {
            return BadRequest(new { succeeded = false, errors = result.Errors });
        }

        _logger.LogInformation("User {UserId} subscribed to push notifications", userId);

        return new JsonResult(new
        {
            succeeded = true,
            subscriptionId = result.SubscriptionId
        });
    }

    /// <summary>
    /// Unsubscribes the user from push notifications.
    /// </summary>
    /// <returns>A JSON result indicating success or failure.</returns>
    public async Task<IActionResult> OnPostUnsubscribeAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return new UnauthorizedResult();
        }

        var result = await _pushNotificationService.UnsubscribeAsync(userId);

        if (result.IsNotAuthorized)
        {
            return Forbid();
        }

        if (!result.Succeeded)
        {
            return BadRequest(new { succeeded = false, errors = result.Errors });
        }

        _logger.LogInformation("User {UserId} unsubscribed from push notifications", userId);

        return new JsonResult(new
        {
            succeeded = true,
            removedCount = result.RemovedCount
        });
    }

    /// <summary>
    /// Request model for subscribing to push notifications.
    /// </summary>
    public class SubscribeRequest
    {
        /// <summary>
        /// Gets or sets the push service endpoint URL.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the P256DH encryption key.
        /// </summary>
        public string? P256dh { get; set; }

        /// <summary>
        /// Gets or sets the authentication secret.
        /// </summary>
        public string? Auth { get; set; }
    }
}
