using Mercato.Payments.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the order confirmation page.
/// </summary>
[Authorize(Roles = "Buyer")]
public class ConfirmationModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ConfirmationModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationModel"/> class.
    /// </summary>
    /// <param name="paymentService">The payment service.</param>
    /// <param name="logger">The logger.</param>
    public ConfirmationModel(
        IPaymentService paymentService,
        ILogger<ConfirmationModel> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the order confirmation data.
    /// </summary>
    public OrderConfirmationData? ConfirmationData { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the confirmation page.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? transactionId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Try to get confirmation from TempData first
        if (TryLoadConfirmationData())
        {
            return Page();
        }

        // If no TempData, try to fetch from transaction ID
        if (!transactionId.HasValue || transactionId.Value == Guid.Empty)
        {
            TempData["Error"] = "Order confirmation not found.";
            return RedirectToPage("Index");
        }

        var transactionResult = await _paymentService.GetTransactionAsync(transactionId.Value, buyerId);

        if (!transactionResult.Succeeded)
        {
            if (transactionResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", transactionResult.Errors);
            return Page();
        }

        if (transactionResult.Transaction == null)
        {
            ErrorMessage = "Transaction not found.";
            return Page();
        }

        // Build confirmation data from transaction
        ConfirmationData = new OrderConfirmationData
        {
            TransactionId = transactionResult.Transaction.Id,
            OrderNumber = $"ORD-{transactionResult.Transaction.Id.ToString("N")[..8].ToUpper()}",
            Amount = transactionResult.Transaction.Amount,
            PaymentMethod = transactionResult.Transaction.PaymentMethodId,
            CompletedAt = transactionResult.Transaction.CompletedAt ?? transactionResult.Transaction.CreatedAt
        };

        return Page();
    }

    private bool TryLoadConfirmationData()
    {
        if (TempData["OrderConfirmation"] is string confirmationJson)
        {
            try
            {
                ConfirmationData = JsonSerializer.Deserialize<OrderConfirmationData>(confirmationJson);
                return ConfirmationData != null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize order confirmation");
            }
        }

        return false;
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
