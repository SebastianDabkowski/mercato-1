using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Application.Services;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Returns;

/// <summary>
/// Page model for resolving a return/complaint case.
/// </summary>
[Authorize(Roles = "Seller")]
public class ResolveModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IRefundService _refundService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ResolveModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="refundService">The refund service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public ResolveModel(
        IOrderService orderService,
        IRefundService refundService,
        IStoreProfileService storeProfileService,
        ILogger<ResolveModel> logger)
    {
        _orderService = orderService;
        _refundService = refundService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the return request (case) being resolved.
    /// </summary>
    public ReturnRequest? Case { get; private set; }

    /// <summary>
    /// Gets the seller's store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets the linked refund information if available.
    /// </summary>
    public CaseRefundInfo? LinkedRefund { get; private set; }

    /// <summary>
    /// Gets or sets the resolution type.
    /// </summary>
    [BindProperty]
    public CaseResolutionType ResolutionType { get; set; }

    /// <summary>
    /// Gets or sets the resolution reason.
    /// </summary>
    [BindProperty]
    public string? ResolutionReason { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for partial refunds.
    /// </summary>
    [BindProperty]
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets whether to initiate a new refund.
    /// </summary>
    [BindProperty]
    public bool InitiateNewRefund { get; set; }

    /// <summary>
    /// Gets or sets an existing refund ID to link.
    /// </summary>
    [BindProperty]
    public Guid? ExistingRefundId { get; set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets the payment transaction ID from the order.
    /// </summary>
    public Guid? PaymentTransactionId { get; private set; }

    /// <summary>
    /// Gets the maximum refundable amount.
    /// </summary>
    public decimal MaxRefundableAmount { get; private set; }

    /// <summary>
    /// Handles GET requests for the resolve page.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                return RedirectToPage("/Seller/Onboarding/Index");
            }

            var result = await _orderService.GetReturnRequestForSellerSubOrderAsync(id, Store.Id);
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

            if (Case == null)
            {
                ErrorMessage = "Case not found.";
                return Page();
            }

            // If already resolved, redirect to details
            if (Case.Status == ReturnStatus.Completed)
            {
                return RedirectToPage("/Seller/Orders/Details", new { id = Case.SellerSubOrderId });
            }

            // Load linked refund info if exists
            LinkedRefund = await _orderService.GetLinkedRefundInfoAsync(id);

            // Set payment transaction ID and max refundable amount
            if (Case.SellerSubOrder?.Order != null)
            {
                PaymentTransactionId = Case.SellerSubOrder.Order.PaymentTransactionId;
                MaxRefundableAmount = Case.SellerSubOrder.TotalAmount;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading case {CaseId} for resolution", id);
            ErrorMessage = "An error occurred while loading the case.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to resolve the case.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                return RedirectToPage("/Seller/Onboarding/Index");
            }

            // Reload the case for display
            var getResult = await _orderService.GetReturnRequestForSellerSubOrderAsync(id, Store.Id);
            if (!getResult.Succeeded)
            {
                if (getResult.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", getResult.Errors);
                return Page();
            }

            Case = getResult.ReturnRequest;

            if (Case == null)
            {
                ErrorMessage = "Case not found.";
                return Page();
            }

            // Set payment info for display
            if (Case.SellerSubOrder?.Order != null)
            {
                PaymentTransactionId = Case.SellerSubOrder.Order.PaymentTransactionId;
                MaxRefundableAmount = Case.SellerSubOrder.TotalAmount;
            }

            var command = new ResolveCaseCommand
            {
                ResolutionType = ResolutionType,
                ResolutionReason = ResolutionReason,
                RefundAmount = RefundAmount,
                ExistingRefundId = ExistingRefundId,
                InitiateNewRefund = InitiateNewRefund,
                PaymentTransactionId = PaymentTransactionId,
                SellerUserId = sellerId
            };

            var result = await _orderService.ResolveCaseAsync(id, Store.Id, command);
            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            _logger.LogInformation(
                "Case {CaseId} resolved with type {ResolutionType} by seller {SellerId}",
                id, ResolutionType, sellerId);

            TempData["Success"] = $"Case resolved successfully with resolution: {GetResolutionTypeDisplayText(ResolutionType)}.";
            
            if (result.RefundInitiated && result.LinkedRefundId.HasValue)
            {
                TempData["Success"] += $" A refund has been initiated (ID: {GetShortId(result.LinkedRefundId.Value)}).";
            }

            return RedirectToPage("/Seller/Orders/Details", new { id = Case.SellerSubOrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving case {CaseId}", id);
            ErrorMessage = "An error occurred while resolving the case.";
            return Page();
        }
    }

    /// <summary>
    /// The number of characters to display for short IDs.
    /// </summary>
    private const int ShortIdLength = 8;

    /// <summary>
    /// Gets a short display version of a GUID (first 8 characters, uppercase).
    /// </summary>
    /// <param name="id">The GUID to shorten.</param>
    /// <returns>The short ID string.</returns>
    public static string GetShortId(Guid id) => id.ToString()[..ShortIdLength].ToUpperInvariant();

    /// <summary>
    /// Gets display text for a resolution type.
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
    /// Gets display text for a return status.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Requested",
        ReturnStatus.UnderReview => "Under Review",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Resolved",
        _ => status.ToString()
    };

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
