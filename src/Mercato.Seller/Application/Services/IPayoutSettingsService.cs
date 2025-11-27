using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for payout settings management operations.
/// </summary>
public interface IPayoutSettingsService
{
    /// <summary>
    /// Gets the payout settings for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The payout settings if found; otherwise, null.</returns>
    Task<PayoutSettings?> GetPayoutSettingsAsync(string sellerId);

    /// <summary>
    /// Gets or creates payout settings for a seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The payout settings.</returns>
    Task<PayoutSettings> GetOrCreatePayoutSettingsAsync(string sellerId);

    /// <summary>
    /// Saves payout settings for a seller.
    /// </summary>
    /// <param name="command">The save payout settings command.</param>
    /// <returns>The result of the save operation.</returns>
    Task<SavePayoutSettingsResult> SavePayoutSettingsAsync(SavePayoutSettingsCommand command);

    /// <summary>
    /// Checks if a seller has complete payout settings.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>True if the seller has complete payout settings; otherwise, false.</returns>
    Task<bool> HasCompletePayoutSettingsAsync(string sellerId);

    /// <summary>
    /// Gets validation errors for payout settings.
    /// </summary>
    /// <param name="payoutSettings">The payout settings to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    IReadOnlyList<string> GetPayoutSettingsValidationErrors(PayoutSettings payoutSettings);
}
