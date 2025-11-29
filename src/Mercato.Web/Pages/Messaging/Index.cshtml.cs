using System.Security.Claims;
using Mercato.Notifications.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Messaging;

/// <summary>
/// Page model for displaying the buyer's message threads.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IMessagingService _messagingService;
    private readonly ILogger<IndexModel> _logger;

    private const int DefaultPageSize = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="messagingService">The messaging service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(IMessagingService messagingService, ILogger<IndexModel> logger)
    {
        _messagingService = messagingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the result containing the buyer's message threads.
    /// </summary>
    public GetThreadsResult? ThreadsResult { get; private set; }

    /// <summary>
    /// Handles GET requests for the messaging index page.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        page = Math.Max(1, page);
        ThreadsResult = await _messagingService.GetBuyerThreadsAsync(userId, page, DefaultPageSize);

        return Page();
    }
}
