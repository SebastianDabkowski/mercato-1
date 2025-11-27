using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for payout settings operations.
/// </summary>
public interface IPayoutSettingsRepository
{
    /// <summary>
    /// Gets the payout settings for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The payout settings if found; otherwise, null.</returns>
    Task<PayoutSettings?> GetBySellerIdAsync(string sellerId);

    /// <summary>
    /// Gets payout settings by its ID.
    /// </summary>
    /// <param name="id">The payout settings ID.</param>
    /// <returns>The payout settings if found; otherwise, null.</returns>
    Task<PayoutSettings?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates new payout settings.
    /// </summary>
    /// <param name="payoutSettings">The payout settings to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(PayoutSettings payoutSettings);

    /// <summary>
    /// Updates existing payout settings.
    /// </summary>
    /// <param name="payoutSettings">The payout settings to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(PayoutSettings payoutSettings);

    /// <summary>
    /// Checks if payout settings exist for a seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>True if payout settings exist; otherwise, false.</returns>
    Task<bool> ExistsAsync(string sellerId);
}
