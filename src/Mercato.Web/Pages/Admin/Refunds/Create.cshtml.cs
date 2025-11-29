using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Refunds;

/// <summary>
/// Page model for processing a new refund (admin).
/// </summary>
public class CreateModel : PageModel
{
    private readonly IRefundService _refundService;
    private readonly IEscrowService _escrowService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="refundService">The refund service.</param>
    /// <param name="escrowService">The escrow service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IRefundService refundService,
        IEscrowService escrowService,
        ILogger<CreateModel> logger)
    {
        _refundService = refundService;
        _escrowService = escrowService;
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
    /// Gets or sets a value indicating whether this is a full refund.
    /// </summary>
    [BindProperty]
    public bool IsFullRefund { get; set; }

    /// <summary>
    /// Gets or sets the seller ID for partial refunds.
    /// </summary>
    [BindProperty]
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for partial refunds.
    /// </summary>
    [BindProperty]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the refund reason.
    /// </summary>
    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit note.
    /// </summary>
    [BindProperty]
    public string? AuditNote { get; set; }

    /// <summary>
    /// Gets or sets the escrow entries for the order.
    /// </summary>
    public IReadOnlyList<EscrowEntry> EscrowEntries { get; set; } = [];

    /// <summary>
    /// Gets or sets the total available for refund.
    /// </summary>
    public decimal TotalAvailableForRefund { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets any success message.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        if (OrderId == Guid.Empty)
        {
            ErrorMessage = "Order ID is required.";
            return Page();
        }

        await LoadEscrowEntriesAsync();
        return Page();
    }

    /// <summary>
    /// Handles POST requests for processing refunds.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (OrderId == Guid.Empty)
        {
            ErrorMessage = "Order ID is required.";
            return Page();
        }

        await LoadEscrowEntriesAsync();

        if (string.IsNullOrWhiteSpace(Reason))
        {
            ErrorMessage = "Refund reason is required.";
            return Page();
        }

        var userId = User.Identity?.Name ?? "admin";

        ProcessRefundResult result;

        if (IsFullRefund)
        {
            var command = new ProcessFullRefundCommand
            {
                OrderId = OrderId,
                PaymentTransactionId = PaymentTransactionId,
                Reason = Reason,
                InitiatedByUserId = userId,
                InitiatedByRole = "Admin",
                AuditNote = AuditNote
            };

            result = await _refundService.ProcessFullRefundAsync(command);
        }
        else
        {
            if (Amount <= 0)
            {
                ErrorMessage = "Refund amount must be greater than zero.";
                return Page();
            }

            var command = new ProcessPartialRefundCommand
            {
                OrderId = OrderId,
                PaymentTransactionId = PaymentTransactionId,
                SellerId = SellerId,
                Amount = Amount,
                Reason = Reason,
                InitiatedByUserId = userId,
                InitiatedByRole = "Admin",
                AuditNote = AuditNote
            };

            result = await _refundService.ProcessPartialRefundAsync(command);
        }

        if (!result.Succeeded)
        {
            if (result.HasProviderErrors)
            {
                ErrorMessage = $"Provider error: {result.ProviderErrorMessage}";
                _logger.LogError("Provider error during refund: {Error}", result.ProviderErrorMessage);
            }
            else
            {
                ErrorMessage = string.Join(", ", result.Errors);
            }
            return Page();
        }

        _logger.LogInformation(
            "Refund processed: RefundId={RefundId}, Type={Type}, Amount={Amount}",
            result.Refund?.Id,
            IsFullRefund ? "Full" : "Partial",
            result.Refund?.Amount);

        TempData["Success"] = $"Refund processed successfully. Amount: {result.Refund?.Amount:C}";
        return RedirectToPage("Details", new { id = result.Refund?.Id });
    }

    private async Task LoadEscrowEntriesAsync()
    {
        var result = await _escrowService.GetEscrowEntriesByOrderIdAsync(OrderId);
        if (result.Succeeded)
        {
            EscrowEntries = result.Entries;
            TotalAvailableForRefund = result.Entries
                .Where(e => e.Status == EscrowStatus.Held || e.Status == EscrowStatus.PartiallyRefunded)
                .Sum(e => e.Amount - e.RefundedAmount);
        }
    }
}
