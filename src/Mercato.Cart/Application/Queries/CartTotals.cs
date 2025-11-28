namespace Mercato.Cart.Application.Queries;

/// <summary>
/// Represents cart totals including items, shipping, discounts, and the total amount payable.
/// This class is designed to be reusable by checkout and order modules.
/// </summary>
public class CartTotals
{
    /// <summary>
    /// Gets the subtotal of all items in the cart (quantity × price for each item).
    /// </summary>
    public decimal ItemsSubtotal { get; init; }

    /// <summary>
    /// Gets the total shipping cost aggregated from all sellers.
    /// </summary>
    public decimal TotalShipping { get; init; }

    /// <summary>
    /// Gets the discount amount applied from a promo code.
    /// </summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>
    /// Gets the applied promo code string.
    /// Null if no promo code is applied.
    /// </summary>
    public string? AppliedPromoCode { get; init; }

    /// <summary>
    /// Gets the description of the applied promo code.
    /// Null if no promo code is applied.
    /// </summary>
    public string? AppliedPromoCodeDescription { get; init; }

    /// <summary>
    /// Gets the total amount payable by the buyer (items subtotal - discount + shipping).
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the total number of items in the cart.
    /// </summary>
    public int TotalItemCount { get; init; }

    /// <summary>
    /// Gets the shipping costs grouped by store.
    /// </summary>
    public IReadOnlyDictionary<Guid, StoreShippingCost> ShippingByStore { get; init; } = new Dictionary<Guid, StoreShippingCost>();

    /// <summary>
    /// Creates a new CartTotals instance with the calculated values (without discount).
    /// </summary>
    /// <param name="itemsSubtotal">The items subtotal.</param>
    /// <param name="shippingByStore">The shipping costs by store.</param>
    /// <param name="totalItemCount">The total item count.</param>
    /// <returns>A new CartTotals instance.</returns>
    public static CartTotals Create(
        decimal itemsSubtotal,
        IReadOnlyDictionary<Guid, StoreShippingCost> shippingByStore,
        int totalItemCount)
    {
        return Create(itemsSubtotal, shippingByStore, totalItemCount, 0, null, null);
    }

    /// <summary>
    /// Creates a new CartTotals instance with the calculated values including discount.
    /// </summary>
    /// <param name="itemsSubtotal">The items subtotal.</param>
    /// <param name="shippingByStore">The shipping costs by store.</param>
    /// <param name="totalItemCount">The total item count.</param>
    /// <param name="discountAmount">The discount amount.</param>
    /// <param name="appliedPromoCode">The applied promo code string.</param>
    /// <param name="appliedPromoCodeDescription">The description of the applied promo code.</param>
    /// <returns>A new CartTotals instance.</returns>
    public static CartTotals Create(
        decimal itemsSubtotal,
        IReadOnlyDictionary<Guid, StoreShippingCost> shippingByStore,
        int totalItemCount,
        decimal discountAmount,
        string? appliedPromoCode,
        string? appliedPromoCodeDescription)
    {
        var totalShipping = shippingByStore.Values.Sum(s => s.ShippingCost);
        var totalAmount = itemsSubtotal - discountAmount + totalShipping;
        
        // Ensure total amount is not negative
        if (totalAmount < 0)
        {
            totalAmount = 0;
        }

        return new CartTotals
        {
            ItemsSubtotal = itemsSubtotal,
            TotalShipping = totalShipping,
            DiscountAmount = discountAmount,
            AppliedPromoCode = appliedPromoCode,
            AppliedPromoCodeDescription = appliedPromoCodeDescription,
            TotalAmount = totalAmount,
            TotalItemCount = totalItemCount,
            ShippingByStore = shippingByStore
        };
    }
}

/// <summary>
/// Represents shipping cost details for a specific store.
/// </summary>
public class StoreShippingCost
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
    /// Gets or sets the calculated shipping cost for this store.
    /// </summary>
    public decimal ShippingCost { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether free shipping was applied.
    /// </summary>
    public bool IsFreeShipping { get; init; }

    /// <summary>
    /// Gets or sets the amount needed to reach free shipping threshold.
    /// Null if free shipping threshold is not configured or already reached.
    /// </summary>
    public decimal? AmountToFreeShipping { get; init; }
}
