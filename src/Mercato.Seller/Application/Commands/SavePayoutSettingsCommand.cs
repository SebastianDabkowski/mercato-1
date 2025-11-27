using System.ComponentModel.DataAnnotations;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for saving or updating payout settings.
/// </summary>
public class SavePayoutSettingsCommand
{
    /// <summary>
    /// Gets or sets the seller's user ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred payout method.
    /// </summary>
    public PayoutMethod PreferredPayoutMethod { get; set; } = PayoutMethod.BankTransfer;

    /// <summary>
    /// Gets or sets the bank name.
    /// </summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Bank name must be between 2 and 200 characters.")]
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Bank account number must be between 5 and 50 characters.")]
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the bank routing number.
    /// </summary>
    [StringLength(20, ErrorMessage = "Bank routing number must be at most 20 characters.")]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the account holder name.
    /// </summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Account holder name must be between 2 and 200 characters.")]
    public string? AccountHolderName { get; set; }

    /// <summary>
    /// Gets or sets the SWIFT/BIC code.
    /// </summary>
    [StringLength(11, ErrorMessage = "SWIFT code must be at most 11 characters.")]
    public string? SwiftCode { get; set; }

    /// <summary>
    /// Gets or sets the IBAN.
    /// </summary>
    [StringLength(34, ErrorMessage = "IBAN must be at most 34 characters.")]
    public string? Iban { get; set; }

    /// <summary>
    /// Gets or sets the payment account email.
    /// </summary>
    [StringLength(254, ErrorMessage = "Payment account email must be at most 254 characters.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string? PaymentAccountEmail { get; set; }

    /// <summary>
    /// Gets or sets the payment account ID.
    /// </summary>
    [StringLength(100, ErrorMessage = "Payment account ID must be at most 100 characters.")]
    public string? PaymentAccountId { get; set; }
}
