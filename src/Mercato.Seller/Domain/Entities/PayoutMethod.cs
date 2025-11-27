namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the available payout methods for sellers.
/// </summary>
public enum PayoutMethod
{
    /// <summary>
    /// Bank transfer via SWIFT/IBAN.
    /// </summary>
    BankTransfer = 0,

    /// <summary>
    /// Payout to integrated payment account (e.g., PayPal, Stripe).
    /// </summary>
    PaymentAccount = 1
}
