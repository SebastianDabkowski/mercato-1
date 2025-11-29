namespace Mercato.Buyer.Application.Services;

/// <summary>
/// Service interface for sending email notifications to buyers.
/// </summary>
public interface IBuyerEmailNotificationService
{
    /// <summary>
    /// Sends a welcome email to a newly registered buyer.
    /// </summary>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="buyerName">The buyer's display name (optional).</param>
    /// <returns>The result of the email send operation.</returns>
    Task<BuyerEmailResult> SendRegistrationWelcomeEmailAsync(string buyerEmail, string? buyerName = null);

    /// <summary>
    /// Sends a payout confirmation email to a buyer.
    /// </summary>
    /// <param name="command">The payout email command.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<BuyerEmailResult> SendPayoutConfirmationEmailAsync(SendPayoutEmailCommand command);

    /// <summary>
    /// Sends a refund confirmation email to a buyer.
    /// </summary>
    /// <param name="command">The refund email command.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<BuyerEmailResult> SendRefundConfirmationEmailAsync(SendRefundEmailCommand command);
}

/// <summary>
/// Command for sending payout confirmation emails.
/// </summary>
public class SendPayoutEmailCommand
{
    /// <summary>
    /// Gets or sets the buyer's email address.
    /// </summary>
    public string BuyerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buyer's display name (optional).
    /// </summary>
    public string? BuyerName { get; set; }

    /// <summary>
    /// Gets or sets the payout amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the payout reference number.
    /// </summary>
    public string PayoutReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the payout was processed.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the payment method description (e.g., "Bank Account ending in 1234").
    /// </summary>
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// Command for sending refund confirmation emails.
/// </summary>
public class SendRefundEmailCommand
{
    /// <summary>
    /// Gets or sets the buyer's email address.
    /// </summary>
    public string BuyerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buyer's display name (optional).
    /// </summary>
    public string? BuyerName { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the refund reference number.
    /// </summary>
    public string RefundReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the refund was processed.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this is a full refund.
    /// </summary>
    public bool IsFullRefund { get; set; }
}

/// <summary>
/// Result of sending a buyer email.
/// </summary>
public class BuyerEmailResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static BuyerEmailResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static BuyerEmailResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static BuyerEmailResult Failure(string error) => Failure([error]);
}
