using Mercato.Cart.Domain.Entities;

namespace Mercato.Cart.Domain.Interfaces;

/// <summary>
/// Repository interface for promo code data access operations.
/// </summary>
public interface IPromoCodeRepository
{
    /// <summary>
    /// Gets a promo code by its code string.
    /// </summary>
    /// <param name="code">The promo code string.</param>
    /// <returns>The promo code if found; otherwise, null.</returns>
    Task<PromoCode?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets a promo code by its unique identifier.
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <returns>The promo code if found; otherwise, null.</returns>
    Task<PromoCode?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds a new promo code to the repository.
    /// </summary>
    /// <param name="promoCode">The promo code to add.</param>
    /// <returns>The added promo code.</returns>
    Task<PromoCode> AddAsync(PromoCode promoCode);

    /// <summary>
    /// Updates an existing promo code.
    /// </summary>
    /// <param name="promoCode">The promo code to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(PromoCode promoCode);

    /// <summary>
    /// Increments the usage count of a promo code.
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IncrementUsageCountAsync(Guid id);
}
