using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for seller onboarding operations.
/// </summary>
public interface ISellerOnboardingRepository
{
    /// <summary>
    /// Gets the onboarding record for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The onboarding record if found; otherwise, null.</returns>
    Task<SellerOnboarding?> GetBySellerIdAsync(string sellerId);

    /// <summary>
    /// Gets an onboarding record by its ID.
    /// </summary>
    /// <param name="id">The onboarding record ID.</param>
    /// <returns>The onboarding record if found; otherwise, null.</returns>
    Task<SellerOnboarding?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new onboarding record.
    /// </summary>
    /// <param name="onboarding">The onboarding record to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(SellerOnboarding onboarding);

    /// <summary>
    /// Updates an existing onboarding record.
    /// </summary>
    /// <param name="onboarding">The onboarding record to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(SellerOnboarding onboarding);

    /// <summary>
    /// Checks if a seller has an existing onboarding record.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>True if an onboarding record exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string sellerId);
}
