using Mercato.Notifications.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Messages;

/// <summary>
/// Page model for displaying all message threads for admin moderation.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IMessagingService _messagingService;
    private readonly ILogger<IndexModel> _logger;

    private const int DefaultPageSize = 20;

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
    /// Gets the result containing all message threads.
    /// </summary>
    public GetThreadsResult? ThreadsResult { get; private set; }

    /// <summary>
    /// Handles GET requests for the admin messages index page.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        page = Math.Max(1, page);
        ThreadsResult = await _messagingService.GetAllThreadsAsync(page, DefaultPageSize);

        return Page();
    }
}
