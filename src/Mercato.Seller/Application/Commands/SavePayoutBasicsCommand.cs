using System.ComponentModel.DataAnnotations;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for saving payout basics in the onboarding wizard.
/// </summary>
public class SavePayoutBasicsCommand
{
    /// <summary>
    /// Gets or sets the seller's user ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bank name.
    /// </summary>
    [Required(ErrorMessage = "Bank name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Bank name must be between 2 and 200 characters.")]
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    [Required(ErrorMessage = "Bank account number is required.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Bank account number must be between 5 and 50 characters.")]
    public string BankAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bank routing number (optional).
    /// </summary>
    [StringLength(20, ErrorMessage = "Bank routing number must be at most 20 characters.")]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the account holder name.
    /// </summary>
    [Required(ErrorMessage = "Account holder name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Account holder name must be between 2 and 200 characters.")]
    public string AccountHolderName { get; set; } = string.Empty;
}
