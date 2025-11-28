using Mercato.Cart.Application.Queries;

namespace Mercato.Cart.Application.Services;

/// <summary>
/// Service interface for calculating internal platform commissions.
/// Commission calculations are for internal use only and not visible to buyers.
/// </summary>
public interface ICommissionCalculator
{
    /// <summary>
    /// Calculates the commission breakdown for seller payouts.
    /// This is used internally by the payments/settlements module.
    /// </summary>
    /// <param name="cartTotals">The cart totals including items and shipping.</param>
    /// <param name="itemsByStore">The cart items grouped by store.</param>
    /// <returns>A dictionary of store IDs to their commission details.</returns>
    Task<IReadOnlyDictionary<Guid, StoreCommission>> CalculateCommissionsAsync(
        CartTotals cartTotals,
        IReadOnlyList<CartItemsByStore> itemsByStore);
}

/// <summary>
/// Represents commission details for a specific store's order.
/// This is for internal use in seller payout calculations.
/// </summary>
public class StoreCommission
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; init; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the gross amount before commission (item subtotal + shipping).
    /// </summary>
    public decimal GrossAmount { get; init; }

    /// <summary>
    /// Gets or sets the commission amount taken by the platform.
    /// </summary>
    public decimal CommissionAmount { get; init; }

    /// <summary>
    /// Gets or sets the commission rate applied (as a decimal, e.g., 0.10 for 10%).
    /// </summary>
    public decimal CommissionRate { get; init; }

    /// <summary>
    /// Gets or sets the net payout amount to the seller (gross - commission).
    /// </summary>
    public decimal NetPayout { get; init; }
}
