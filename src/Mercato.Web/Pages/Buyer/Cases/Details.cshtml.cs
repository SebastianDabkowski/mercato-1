using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Buyer.Cases;

/// <summary>
/// Page model for the buyer's case details page.
/// </summary>
[Authorize(Roles = "Buyer")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IRefundService _refundService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="refundService">The refund service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IOrderService orderService,
        IRefundService refundService,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the return request (case).
    /// </summary>
    public ReturnRequest? Case { get; private set; }

    /// <summary>
    /// Gets the refunds linked to the order.
    /// </summary>
    public IReadOnlyList<Refund> Refunds { get; private set; } = [];

    /// <summary>
    /// Gets the total refunded amount.
    /// </summary>
    public decimal TotalRefunded { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the case details page.
    /// </summary>
    /// <param name="id">The case (return request) ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        if (!id.HasValue || id.Value == Guid.Empty)
        {
            TempData["Error"] = "Case not found.";
            return RedirectToPage("Index");
        }

        var result = await _orderService.GetReturnRequestAsync(id.Value, buyerId);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Case = result.ReturnRequest;

        // Try to load refund information if the case is associated with an order
        if (Case?.SellerSubOrder?.Order != null)
        {
            var refundsResult = await _refundService.GetRefundsByOrderIdAsync(Case.SellerSubOrder.Order.Id);
            if (refundsResult.Succeeded)
            {
                Refunds = refundsResult.Refunds;
                TotalRefunded = refundsResult.TotalRefunded;
            }
        }

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
    /// Gets a user-friendly resolution summary based on the case status.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The resolution summary.</returns>
    public static string GetResolutionSummary(ReturnStatus status) => status switch
    {
        ReturnStatus.Approved => "Your return request has been approved by the seller.",
        ReturnStatus.Rejected => "Your return request has been declined by the seller.",
        ReturnStatus.Completed => "This case has been resolved.",
        _ => string.Empty
    };

    /// <summary>
    /// Gets a user-friendly resolution summary based on the case status and resolution type.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <param name="resolutionType">The resolution type.</param>
    /// <returns>The resolution summary.</returns>
    public static string GetResolutionSummaryForType(ReturnStatus status, CaseResolutionType? resolutionType)
    {
        if (!resolutionType.HasValue)
        {
            return GetResolutionSummary(status);
        }

        return resolutionType.Value switch
        {
            CaseResolutionType.FullRefund => "A full refund has been issued for your order.",
            CaseResolutionType.PartialRefund => "A partial refund has been issued for your order.",
            CaseResolutionType.Replacement => "A replacement item will be sent to you.",
            CaseResolutionType.Repair => "Your item will be repaired and returned to you.",
            CaseResolutionType.NoRefund => "Your return request has been declined.",
            _ => GetResolutionSummary(status)
        };
    }

    /// <summary>
    /// Gets display text for a case resolution type.
    /// </summary>
    /// <param name="type">The resolution type.</param>
    /// <returns>The display text.</returns>
    public static string GetResolutionTypeDisplayText(CaseResolutionType type) => type switch
    {
        CaseResolutionType.FullRefund => "Full Refund",
        CaseResolutionType.PartialRefund => "Partial Refund",
        CaseResolutionType.Replacement => "Replacement",
        CaseResolutionType.Repair => "Repair",
        CaseResolutionType.NoRefund => "No Refund",
        _ => type.ToString()
    };

    /// <summary>
    /// Gets the CSS class for a resolution type badge.
    /// </summary>
    /// <param name="type">The resolution type.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetResolutionTypeBadgeClass(CaseResolutionType type) => type switch
    {
        CaseResolutionType.FullRefund => "bg-success",
        CaseResolutionType.PartialRefund => "bg-info",
        CaseResolutionType.Replacement => "bg-primary",
        CaseResolutionType.Repair => "bg-primary",
        CaseResolutionType.NoRefund => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the CSS class for a refund status badge.
    /// </summary>
    /// <param name="status">The refund status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetRefundStatusBadgeClass(RefundStatus status) => status switch
    {
        RefundStatus.Pending => "bg-warning text-dark",
        RefundStatus.Processing => "bg-info",
        RefundStatus.Completed => "bg-success",
        RefundStatus.Failed => "bg-danger",
        RefundStatus.Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a refund status.
    /// </summary>
    /// <param name="status">The refund status.</param>
    /// <returns>The display text.</returns>
    public static string GetRefundStatusDisplayText(RefundStatus status) => status switch
    {
        RefundStatus.Pending => "Pending",
        RefundStatus.Processing => "Processing",
        RefundStatus.Completed => "Completed",
        RefundStatus.Failed => "Failed",
        RefundStatus.Cancelled => "Cancelled",
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

    /// <summary>
    /// Formats a refund ID for display (first 8 characters, uppercase).
    /// </summary>
    /// <param name="refundId">The refund ID.</param>
    /// <returns>The formatted refund ID.</returns>
    public static string FormatRefundId(Guid refundId)
    {
        return refundId.ToString()[..8].ToUpperInvariant();
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
