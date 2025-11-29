using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Returns;

/// <summary>
/// Page model for the seller's case details page with messaging.
/// </summary>
[Authorize(Roles = "Seller")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IRefundService _refundService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="refundService">The refund service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IOrderService orderService,
        IRefundService refundService,
        IStoreProfileService storeProfileService,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _refundService = refundService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the return request (case).
    /// </summary>
    public ReturnRequest? Case { get; private set; }

    /// <summary>
    /// Gets the seller's store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets the refunds linked to the order.
    /// </summary>
    public IReadOnlyList<Refund> Refunds { get; private set; } = [];

    /// <summary>
    /// Gets the total refunded amount.
    /// </summary>
    public decimal TotalRefunded { get; private set; }

    /// <summary>
    /// Gets the case messages.
    /// </summary>
    public IReadOnlyList<CaseMessageDto> Messages { get; private set; } = [];

    /// <summary>
    /// Gets or sets the new message content.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Message content is required.")]
    [StringLength(2000, ErrorMessage = "Message must not exceed 2000 characters.")]
    public string? NewMessageContent { get; set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the case details page.
    /// </summary>
    /// <param name="id">The case (return request) ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
        if (Store == null)
        {
            return RedirectToPage("/Seller/Onboarding/Index");
        }

        var result = await _orderService.GetReturnRequestForSellerAsync(id, Store.Id);

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

        // Load messages
        if (Case != null)
        {
            var messagesResult = await _orderService.GetCaseMessagesAsync(id, sellerId, "Seller", Store.Id);
            if (messagesResult.Succeeded)
            {
                Messages = messagesResult.Messages;
            }

            // Mark activity as viewed
            await _orderService.MarkCaseActivityViewedAsync(id, sellerId, "Seller", Store.Id);
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to add a new message.
    /// </summary>
    /// <param name="id">The case (return request) ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
        if (Store == null)
        {
            return RedirectToPage("/Seller/Onboarding/Index");
        }

        // Load the case for display regardless of validation
        var caseResult = await _orderService.GetReturnRequestForSellerAsync(id, Store.Id);
        if (!caseResult.Succeeded)
        {
            if (caseResult.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", caseResult.Errors);
            return Page();
        }

        Case = caseResult.ReturnRequest;

        // Load refunds
        if (Case?.SellerSubOrder?.Order != null)
        {
            var refundsResult = await _refundService.GetRefundsByOrderIdAsync(Case.SellerSubOrder.Order.Id);
            if (refundsResult.Succeeded)
            {
                Refunds = refundsResult.Refunds;
                TotalRefunded = refundsResult.TotalRefunded;
            }
        }

        // Load existing messages
        var messagesResult = await _orderService.GetCaseMessagesAsync(id, sellerId, "Seller", Store.Id);
        if (messagesResult.Succeeded)
        {
            Messages = messagesResult.Messages;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = id,
            SenderUserId = sellerId,
            SenderRole = "Seller",
            Content = NewMessageContent!
        };

        var result = await _orderService.AddCaseMessageAsync(command, Store.Id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        SuccessMessage = "Message sent successfully.";
        NewMessageContent = null;

        // Reload messages after adding
        messagesResult = await _orderService.GetCaseMessagesAsync(id, sellerId, "Seller", Store.Id);
        if (messagesResult.Succeeded)
        {
            Messages = messagesResult.Messages;
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to set the case status to UnderReview (request more information).
    /// </summary>
    /// <param name="id">The case (return request) ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostSetUnderReviewAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
        if (Store == null)
        {
            return RedirectToPage("/Seller/Onboarding/Index");
        }

        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = ReturnStatus.UnderReview,
            SellerNotes = "Requesting more information from the buyer."
        };

        var result = await _orderService.UpdateReturnRequestStatusAsync(id, Store.Id, command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            TempData["Error"] = string.Join(", ", result.Errors);
        }
        else
        {
            TempData["Success"] = "Case status updated to Under Review. The buyer has been notified.";
        }

        return RedirectToPage(new { id });
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
        ReturnStatus.Requested => "Pending Review",
        ReturnStatus.UnderReview => "Under Review",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Resolved",
        _ => status.ToString()
    };

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
    /// Number of characters to display for short IDs.
    /// </summary>
    private const int ShortIdLength = 8;

    /// <summary>
    /// Formats a case ID for display (first 8 characters, uppercase).
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <returns>The formatted case ID.</returns>
    public static string FormatCaseId(Guid caseId)
    {
        return caseId.ToString()[..ShortIdLength].ToUpperInvariant();
    }

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
