using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for seller onboarding operations.
/// </summary>
public interface ISellerOnboardingService
{
    /// <summary>
    /// Gets or creates an onboarding record for a seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The onboarding record.</returns>
    Task<SellerOnboarding> GetOrCreateOnboardingAsync(string sellerId);

    /// <summary>
    /// Gets the onboarding record for a seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The onboarding record if found; otherwise, null.</returns>
    Task<SellerOnboarding?> GetOnboardingAsync(string sellerId);

    /// <summary>
    /// Saves store profile data for a seller's onboarding.
    /// </summary>
    /// <param name="command">The save store profile command.</param>
    /// <returns>The result of the save operation.</returns>
    Task<SaveOnboardingStepResult> SaveStoreProfileAsync(SaveStoreProfileCommand command);

    /// <summary>
    /// Saves verification data for a seller's onboarding.
    /// </summary>
    /// <param name="command">The save verification data command.</param>
    /// <returns>The result of the save operation.</returns>
    Task<SaveOnboardingStepResult> SaveVerificationDataAsync(SaveVerificationDataCommand command);

    /// <summary>
    /// Saves payout basics for a seller's onboarding.
    /// </summary>
    /// <param name="command">The save payout basics command.</param>
    /// <returns>The result of the save operation.</returns>
    Task<SaveOnboardingStepResult> SavePayoutBasicsAsync(SavePayoutBasicsCommand command);

    /// <summary>
    /// Completes the onboarding wizard and sets the seller to pending verification.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The result of the complete operation.</returns>
    Task<CompleteOnboardingResult> CompleteOnboardingAsync(string sellerId);

    /// <summary>
    /// Checks if a seller has completed onboarding.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>True if the seller has completed onboarding; otherwise, false.</returns>
    Task<bool> IsOnboardingCompleteAsync(string sellerId);

    /// <summary>
    /// Gets validation errors for the store profile step.
    /// </summary>
    /// <param name="onboarding">The onboarding record.</param>
    /// <returns>A list of validation error messages.</returns>
    IReadOnlyList<string> GetStoreProfileValidationErrors(SellerOnboarding onboarding);

    /// <summary>
    /// Gets validation errors for the verification data step.
    /// </summary>
    /// <param name="onboarding">The onboarding record.</param>
    /// <returns>A list of validation error messages.</returns>
    IReadOnlyList<string> GetVerificationDataValidationErrors(SellerOnboarding onboarding);

    /// <summary>
    /// Gets validation errors for the payout basics step.
    /// </summary>
    /// <param name="onboarding">The onboarding record.</param>
    /// <returns>A list of validation error messages.</returns>
    IReadOnlyList<string> GetPayoutBasicsValidationErrors(SellerOnboarding onboarding);
}
