using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Buyer.Cases;

/// <summary>
/// Page model for the buyer's returns and complaints list page.
/// </summary>
[Authorize(Roles = "Buyer")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IOrderService orderService,
        ILogger<IndexModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of return requests for the buyer.
    /// </summary>
    public IReadOnlyList<ReturnRequest> Cases { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ReturnStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start date for date range filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets all available return statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<ReturnStatus> AllStatuses => Enum.GetValues<ReturnStatus>();

    /// <summary>
    /// Handles GET requests for the returns and complaints page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        var result = await _orderService.GetReturnRequestsForBuyerAsync(buyerId);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        // Apply filters
        var filtered = result.ReturnRequests.AsEnumerable();

        if (Status.HasValue)
        {
            filtered = filtered.Where(r => r.Status == Status.Value);
        }

        if (FromDate.HasValue)
        {
            filtered = filtered.Where(r => r.CreatedAt >= FromDate.Value);
        }

        if (ToDate.HasValue)
        {
            // Add 1 day to include the entire ToDate
            filtered = filtered.Where(r => r.CreatedAt < ToDate.Value.AddDays(1));
        }

        Cases = filtered.OrderByDescending(r => r.CreatedAt).ToList();

        return Page();
    }

    /// <summary>
    /// Gets the CSS class for a return status badge.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "bg-warning text-dark",
        ReturnStatus.UnderReview => "bg-info",
        ReturnStatus.Approved => "bg-success",
        ReturnStatus.Rejected => "bg-danger",
        ReturnStatus.Completed => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a return status.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Pending Seller Review",
        ReturnStatus.UnderReview => "In Progress",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Resolved",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets a user-friendly case type display text.
    /// </summary>
    /// <returns>The case type display text.</returns>
    public static string GetCaseType()
    {
        return "Return Request";
    }

    /// <summary>
    /// Formats a case ID for display (first 8 characters, uppercase).
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <returns>The formatted case ID.</returns>
    public static string FormatCaseId(Guid caseId)
    {
        return caseId.ToString()[..8].ToUpperInvariant();
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
