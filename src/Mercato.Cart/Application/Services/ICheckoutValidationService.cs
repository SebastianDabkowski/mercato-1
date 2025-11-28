using Mercato.Cart.Application.Commands;

namespace Mercato.Cart.Application.Services;

/// <summary>
/// Service interface for checkout validation operations.
/// Validates stock availability and price changes before order creation.
/// </summary>
public interface ICheckoutValidationService
{
    /// <summary>
    /// Validates all cart items for checkout, checking stock availability and price changes.
    /// This method should be called before placing an order to ensure all items are valid.
    /// </summary>
    /// <param name="command">The validate checkout command.</param>
    /// <returns>The validation result containing any stock or price issues.</returns>
    Task<ValidateCheckoutResult> ValidateCheckoutAsync(ValidateCheckoutCommand command);

    /// <summary>
    /// Updates cart item prices to match current product prices.
    /// Should be called when the buyer confirms acceptance of price changes.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateCartPricesToCurrentAsync(string buyerId);
}
