using System.Security.Claims;
using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Seller.Messages;

/// <summary>
/// Page model for displaying and interacting with a customer message thread.
/// </summary>
[Authorize(Roles = "Seller")]
public class ThreadModel : PageModel
{
    private readonly IMessagingService _messagingService;
    private readonly ILogger<ThreadModel> _logger;

    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadModel"/> class.
    /// </summary>
    /// <param name="messagingService">The messaging service.</param>
    /// <param name="logger">The logger.</param>
    public ThreadModel(IMessagingService messagingService, ILogger<ThreadModel> logger)
    {
        _messagingService = messagingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the message thread.
    /// </summary>
    public MessageThread? Thread { get; private set; }

    /// <summary>
    /// Gets the result containing the thread messages.
    /// </summary>
    public GetThreadMessagesResult? MessagesResult { get; private set; }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    public string? CurrentUserId { get; private set; }

    /// <summary>
    /// Handles GET requests for the seller thread page.
    /// </summary>
    /// <param name="id">The thread ID.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, int page = 1)
    {
        CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return Challenge();
        }

        var threadResult = await _messagingService.GetThreadAsync(id, CurrentUserId, false);
        if (!threadResult.Succeeded)
        {
            if (threadResult.IsNotAuthorized)
            {
                return Forbid();
            }
            return Page();
        }

        Thread = threadResult.Thread;
        page = Math.Max(1, page);
        MessagesResult = await _messagingService.GetThreadMessagesAsync(id, CurrentUserId, false, page, DefaultPageSize);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to send a message.
    /// </summary>
    /// <param name="id">The thread ID.</param>
    /// <param name="content">The message content.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostSendMessageAsync(Guid id, string content)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Message content is required.";
            return RedirectToPage(new { id });
        }

        var command = new SendMessageCommand
        {
            ThreadId = id,
            SenderId = userId,
            Content = content
        };

        var result = await _messagingService.SendMessageAsync(command);

        if (result.Succeeded)
        {
            TempData["Success"] = "Reply sent successfully.";
        }
        else if (result.IsNotAuthorized)
        {
            return Forbid();
        }
        else
        {
            TempData["Error"] = string.Join(", ", result.Errors);
        }

        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Handles POST requests to close a thread.
    /// </summary>
    /// <param name="id">The thread ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostCloseThreadAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var result = await _messagingService.CloseThreadAsync(id, userId, false);

        if (result.Succeeded)
        {
            TempData["Success"] = "Thread closed successfully.";
        }
        else if (result.IsNotAuthorized)
        {
            return Forbid();
        }
        else
        {
            TempData["Error"] = string.Join(", ", result.Errors);
        }

        return RedirectToPage(new { id });
    }
}
