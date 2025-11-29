using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Notifications;

/// <summary>
/// Page model for the notifications center page.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        INotificationService notificationService,
        ILogger<IndexModel> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of notifications for the current page.
    /// </summary>
    public IReadOnlyList<Notification> Notifications { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of notifications matching the filter.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage { get; private set; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private set; } = DefaultPageSize;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Gets or sets the filter for read/unread notifications.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    /// <summary>
    /// Gets the unread count for display.
    /// </summary>
    public int UnreadCount { get; private set; }

    /// <summary>
    /// Handles GET requests for the notifications page.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        bool? isRead = Filter?.ToLowerInvariant() switch
        {
            "unread" => false,
            "read" => true,
            _ => null
        };

        var result = await _notificationService.GetUserNotificationsAsync(userId, isRead, page, DefaultPageSize);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Notifications = result.Notifications;
        TotalCount = result.TotalCount;
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;

        UnreadCount = await _notificationService.GetUnreadCountAsync(userId);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to mark a notification as read.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <returns>The action result.</returns>
    public async Task<IActionResult> OnPostMarkAsReadAsync(Guid id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId);

        if (result.IsNotAuthorized)
        {
            return Forbid();
        }

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(", ", result.Errors);
        }

        return RedirectToPage(new { Filter });
    }

    /// <summary>
    /// Handles POST requests to mark all notifications as read.
    /// </summary>
    /// <returns>The action result.</returns>
    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(", ", result.Errors);
        }
        else
        {
            TempData["Success"] = $"Marked {result.MarkedCount} notification(s) as read.";
        }

        return RedirectToPage(new { Filter });
    }

    /// <summary>
    /// Gets the CSS class for a notification type icon.
    /// </summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The CSS icon class.</returns>
    public static string GetTypeIconClass(NotificationType type) => type switch
    {
        NotificationType.OrderPlaced => "bi-bag-check",
        NotificationType.OrderShipped => "bi-truck",
        NotificationType.OrderDelivered => "bi-box-seam",
        NotificationType.ReturnRequested => "bi-arrow-return-left",
        NotificationType.ReturnApproved => "bi-check-circle",
        NotificationType.ReturnRejected => "bi-x-circle",
        NotificationType.PayoutProcessed => "bi-cash-stack",
        NotificationType.Message => "bi-envelope",
        NotificationType.SystemUpdate => "bi-gear",
        _ => "bi-bell"
    };

    /// <summary>
    /// Gets the CSS class for a notification type badge.
    /// </summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The CSS badge class.</returns>
    public static string GetTypeBadgeClass(NotificationType type) => type switch
    {
        NotificationType.OrderPlaced => "bg-success",
        NotificationType.OrderShipped => "bg-info",
        NotificationType.OrderDelivered => "bg-primary",
        NotificationType.ReturnRequested => "bg-warning text-dark",
        NotificationType.ReturnApproved => "bg-success",
        NotificationType.ReturnRejected => "bg-danger",
        NotificationType.PayoutProcessed => "bg-success",
        NotificationType.Message => "bg-secondary",
        NotificationType.SystemUpdate => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a notification type.
    /// </summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The display text.</returns>
    public static string GetTypeDisplayText(NotificationType type) => type switch
    {
        NotificationType.OrderPlaced => "Order Placed",
        NotificationType.OrderShipped => "Order Shipped",
        NotificationType.OrderDelivered => "Order Delivered",
        NotificationType.ReturnRequested => "Return Requested",
        NotificationType.ReturnApproved => "Return Approved",
        NotificationType.ReturnRejected => "Return Rejected",
        NotificationType.PayoutProcessed => "Payout Processed",
        NotificationType.Message => "Message",
        NotificationType.SystemUpdate => "System Update",
        _ => type.ToString()
    };

    /// <summary>
    /// Gets the query string for pagination links that preserves filter state.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>The query string.</returns>
    public string GetPaginationQueryString(int page)
    {
        var queryParams = new List<string> { $"page={page}" };

        if (!string.IsNullOrEmpty(Filter))
        {
            queryParams.Add($"Filter={Filter}");
        }

        return "?" + string.Join("&", queryParams);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
