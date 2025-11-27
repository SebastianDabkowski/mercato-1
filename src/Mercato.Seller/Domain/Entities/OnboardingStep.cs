namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the steps in the seller onboarding wizard.
/// </summary>
public enum OnboardingStep
{
    /// <summary>
    /// First step: Store profile basics (store name, description, etc.).
    /// </summary>
    StoreProfile = 0,

    /// <summary>
    /// Second step: Verification data (business details, identity verification).
    /// </summary>
    VerificationData = 1,

    /// <summary>
    /// Third step: Payout basics (bank account, payment method).
    /// </summary>
    PayoutBasics = 2,

    /// <summary>
    /// Onboarding is complete.
    /// </summary>
    Completed = 3
}
