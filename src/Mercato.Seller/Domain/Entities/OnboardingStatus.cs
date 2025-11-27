namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the overall status of a seller's onboarding.
/// </summary>
public enum OnboardingStatus
{
    /// <summary>
    /// Onboarding is in progress.
    /// </summary>
    InProgress = 0,

    /// <summary>
    /// Onboarding is complete, awaiting verification.
    /// </summary>
    PendingVerification = 1,

    /// <summary>
    /// Seller has been verified and activated.
    /// </summary>
    Verified = 2,

    /// <summary>
    /// Onboarding was abandoned.
    /// </summary>
    Abandoned = 3
}
