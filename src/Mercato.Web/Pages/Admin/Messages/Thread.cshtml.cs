using System.Security.Claims;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Messages;

/// <summary>
/// Page model for admin viewing of a message thread.
/// </summary>
[Authorize(Roles = "Admin")]
public class ThreadModel : PageModel
{
    private readonly IMessagingService _messagingService;
    private readonly ILogger<ThreadModel> _logger;

    private const int DefaultPageSize = 50;

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
    /// Handles GET requests for the admin thread page.
    /// </summary>
    /// <param name="id">The thread ID.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var threadResult = await _messagingService.GetThreadAsync(id, userId, true);
        if (!threadResult.Succeeded)
        {
            return Page();
        }

        Thread = threadResult.Thread;
        page = Math.Max(1, page);
        MessagesResult = await _messagingService.GetThreadMessagesAsync(id, userId, true, page, DefaultPageSize);

        return Page();
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

        var result = await _messagingService.CloseThreadAsync(id, userId, true);

        if (result.Succeeded)
        {
            TempData["Success"] = "Thread closed successfully.";
        }
        else
        {
            TempData["Error"] = string.Join(", ", result.Errors);
        }

        return RedirectToPage(new { id });
    }
}
