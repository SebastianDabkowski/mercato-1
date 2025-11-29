using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Seller.Refunds;

/// <summary>
/// Page model for seller to request a refund.
/// </summary>
public class CreateModel : PageModel
{
    private readonly IRefundService _refundService;
    private readonly IEscrowService _escrowService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="refundService">The refund service.</param>
    /// <param name="escrowService">The escrow service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IRefundService refundService,
        IEscrowService escrowService,
        IStoreProfileService storeProfileService,
        ILogger<CreateModel> logger)
    {
        _refundService = refundService;
        _escrowService = escrowService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the order ID for the refund.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    [BindProperty]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the refund reason.
    /// </summary>
    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller's escrow entry.
    /// </summary>
    public EscrowEntry? SellerEscrowEntry { get; set; }

    /// <summary>
    /// Gets or sets the seller's store ID.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the maximum refundable amount.
    /// </summary>
    public decimal MaxRefundableAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the seller is eligible to refund.
    /// </summary>
    public bool IsEligible { get; set; }

    /// <summary>
    /// Gets or sets the ineligibility reason if not eligible.
    /// </summary>
    public string? IneligibilityReason { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Seller/Login");
        }

        if (OrderId == Guid.Empty)
        {
            ErrorMessage = "Order ID is required.";
            return Page();
        }

        // Get seller's store
        var store = await _storeProfileService.GetStoreBySellerIdAsync(userId);
        if (store == null)
        {
            return RedirectToPage("/Seller/Onboarding/Index");
        }

        SellerId = store.Id;

        // Load seller's escrow entry
        var escrowResult = await _escrowService.GetEscrowEntriesByOrderIdAsync(OrderId);
        if (escrowResult.Succeeded)
        {
            SellerEscrowEntry = escrowResult.Entries.FirstOrDefault(e => e.SellerId == SellerId);
        }

        if (SellerEscrowEntry == null)
        {
            ErrorMessage = "No escrow entry found for your store on this order.";
            return Page();
        }

        // Check eligibility
        var eligibilityResult = await _refundService.CheckSellerRefundEligibilityAsync(new CheckRefundEligibilityCommand
        {
            OrderId = OrderId,
            SellerId = SellerId,
            Amount = SellerEscrowEntry.Amount - SellerEscrowEntry.RefundedAmount
        });

        if (eligibilityResult.Succeeded)
        {
            IsEligible = eligibilityResult.IsEligible;
            MaxRefundableAmount = eligibilityResult.MaxRefundableAmount;
            IneligibilityReason = eligibilityResult.IneligibilityReason;
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests for processing refunds.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Seller/Login");
        }

        if (OrderId == Guid.Empty)
        {
            ErrorMessage = "Order ID is required.";
            return Page();
        }

        // Get seller's store
        var store = await _storeProfileService.GetStoreBySellerIdAsync(userId);
        if (store == null)
        {
            return RedirectToPage("/Seller/Onboarding/Index");
        }

        SellerId = store.Id;

        // Load escrow entry for display
        var escrowResult = await _escrowService.GetEscrowEntriesByOrderIdAsync(OrderId);
        if (escrowResult.Succeeded)
        {
            SellerEscrowEntry = escrowResult.Entries.FirstOrDefault(e => e.SellerId == SellerId);
        }

        if (SellerEscrowEntry == null)
        {
            ErrorMessage = "No escrow entry found for your store on this order.";
            return Page();
        }

        // Check eligibility
        var eligibilityResult = await _refundService.CheckSellerRefundEligibilityAsync(new CheckRefundEligibilityCommand
        {
            OrderId = OrderId,
            SellerId = SellerId,
            Amount = Amount
        });

        if (eligibilityResult.Succeeded)
        {
            IsEligible = eligibilityResult.IsEligible;
            MaxRefundableAmount = eligibilityResult.MaxRefundableAmount;
            IneligibilityReason = eligibilityResult.IneligibilityReason;
        }

        if (!IsEligible)
        {
            ErrorMessage = IneligibilityReason ?? "You are not eligible to process this refund.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            ErrorMessage = "Refund reason is required.";
            return Page();
        }

        if (Amount <= 0 || Amount > MaxRefundableAmount)
        {
            ErrorMessage = $"Refund amount must be between $0.01 and {MaxRefundableAmount:C}.";
            return Page();
        }

        // Process the refund
        var command = new ProcessPartialRefundCommand
        {
            OrderId = OrderId,
            PaymentTransactionId = PaymentTransactionId,
            SellerId = SellerId,
            Amount = Amount,
            Reason = Reason,
            InitiatedByUserId = userId,
            InitiatedByRole = "Seller",
            AuditNote = $"Seller-initiated refund from {store.Name}"
        };

        var result = await _refundService.ProcessPartialRefundAsync(command);

        if (!result.Succeeded)
        {
            if (result.HasProviderErrors)
            {
                ErrorMessage = $"Provider error: {result.ProviderErrorMessage}";
                _logger.LogError("Provider error during seller refund: {Error}", result.ProviderErrorMessage);
            }
            else
            {
                ErrorMessage = string.Join(", ", result.Errors);
            }
            return Page();
        }

        _logger.LogInformation(
            "Seller refund processed: RefundId={RefundId}, SellerId={SellerId}, Amount={Amount}",
            result.Refund?.Id,
            SellerId,
            Amount);

        TempData["Success"] = $"Refund of {Amount:C} processed successfully.";
        return RedirectToPage("/Seller/Orders/Details", new { orderId = OrderId });
    }
}
