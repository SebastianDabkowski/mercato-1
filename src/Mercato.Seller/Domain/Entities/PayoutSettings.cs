namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a seller's payout settings for receiving funds from sales.
/// </summary>
public class PayoutSettings
{
    /// <summary>
    /// Gets or sets the unique identifier for the payout settings.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller's user ID (linked to IdentityUser.Id).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred payout method.
    /// </summary>
    public PayoutMethod PreferredPayoutMethod { get; set; } = PayoutMethod.BankTransfer;

    /// <summary>
    /// Gets or sets the bank name for payouts.
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the bank account number for payouts.
    /// </summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the bank routing number for payouts.
    /// </summary>
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the account holder name for payouts.
    /// </summary>
    public string? AccountHolderName { get; set; }

    /// <summary>
    /// Gets or sets the SWIFT/BIC code for international transfers.
    /// </summary>
    public string? SwiftCode { get; set; }

    /// <summary>
    /// Gets or sets the IBAN for international transfers.
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// Gets or sets the payment account email (for PayPal, etc.).
    /// </summary>
    public string? PaymentAccountEmail { get; set; }

    /// <summary>
    /// Gets or sets the payment account ID (for Stripe Connect, etc.).
    /// </summary>
    public string? PaymentAccountId { get; set; }

    /// <summary>
    /// Gets or sets whether the payout settings are complete and valid.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout settings were created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout settings were last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Checks if bank transfer payout method is complete.
    /// </summary>
    public bool IsBankTransferComplete =>
        !string.IsNullOrWhiteSpace(BankName) &&
        !string.IsNullOrWhiteSpace(BankAccountNumber) &&
        !string.IsNullOrWhiteSpace(AccountHolderName);

    /// <summary>
    /// Checks if payment account payout method is complete.
    /// </summary>
    public bool IsPaymentAccountComplete =>
        !string.IsNullOrWhiteSpace(PaymentAccountEmail) ||
        !string.IsNullOrWhiteSpace(PaymentAccountId);
}
