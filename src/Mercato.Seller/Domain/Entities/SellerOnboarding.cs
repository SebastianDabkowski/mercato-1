namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a seller's onboarding wizard progress and data.
/// </summary>
public class SellerOnboarding
{
    /// <summary>
    /// Gets or sets the unique identifier for the onboarding record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller's user ID (linked to IdentityUser.Id).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current step in the onboarding wizard.
    /// </summary>
    public OnboardingStep CurrentStep { get; set; } = OnboardingStep.StoreProfile;

    /// <summary>
    /// Gets or sets the overall onboarding status.
    /// </summary>
    public OnboardingStatus Status { get; set; } = OnboardingStatus.InProgress;

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    public string? StoreDescription { get; set; }

    /// <summary>
    /// Gets or sets the store logo URL.
    /// </summary>
    public string? StoreLogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the business name for verification.
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// Gets or sets the business address for verification.
    /// </summary>
    public string? BusinessAddress { get; set; }

    /// <summary>
    /// Gets or sets the tax identification number.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the business registration number.
    /// </summary>
    public string? BusinessRegistrationNumber { get; set; }

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
    /// Gets or sets the date and time when the onboarding was started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the onboarding was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the onboarding was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Checks if the store profile step has been completed.
    /// </summary>
    public bool IsStoreProfileComplete =>
        !string.IsNullOrWhiteSpace(StoreName) &&
        !string.IsNullOrWhiteSpace(StoreDescription);

    /// <summary>
    /// Checks if the verification data step has been completed.
    /// </summary>
    public bool IsVerificationDataComplete =>
        !string.IsNullOrWhiteSpace(BusinessName) &&
        !string.IsNullOrWhiteSpace(BusinessAddress) &&
        !string.IsNullOrWhiteSpace(TaxId);

    /// <summary>
    /// Checks if the payout basics step has been completed.
    /// </summary>
    public bool IsPayoutBasicsComplete =>
        !string.IsNullOrWhiteSpace(BankName) &&
        !string.IsNullOrWhiteSpace(BankAccountNumber) &&
        !string.IsNullOrWhiteSpace(AccountHolderName);
}
