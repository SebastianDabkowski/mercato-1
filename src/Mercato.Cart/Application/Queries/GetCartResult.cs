using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Entities;

namespace Mercato.Cart.Application.Queries;

/// <summary>
/// Result of getting a buyer's cart.
/// </summary>
public class GetCartResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the cart if found.
    /// </summary>
    public Domain.Entities.Cart? Cart { get; private init; }

    /// <summary>
    /// Gets the cart items grouped by store.
    /// </summary>
    public IReadOnlyList<CartItemsByStore> ItemsByStore { get; private init; } = [];

    /// <summary>
    /// Gets the total number of items in the cart.
    /// </summary>
    public int TotalItemCount { get; private init; }

    /// <summary>
    /// Gets the total price of all items in the cart (items subtotal only, before shipping).
    /// </summary>
    public decimal TotalPrice { get; private init; }

    /// <summary>
    /// Gets the cart totals including shipping costs.
    /// </summary>
    public CartTotals? Totals { get; private init; }

    /// <summary>
    /// Creates a successful result with the cart and calculated totals.
    /// </summary>
    /// <param name="cart">The cart.</param>
    /// <param name="shippingByStore">The shipping costs by store. If null, shipping is not calculated.</param>
    /// <returns>A successful result.</returns>
    public static GetCartResult Success(
        Domain.Entities.Cart? cart,
        IReadOnlyDictionary<Guid, StoreShippingCost>? shippingByStore = null)
    {
        return Success(cart, shippingByStore, PromoCodeDiscountInfo.None());
    }

    /// <summary>
    /// Creates a successful result with the cart, calculated totals, and promo code discount.
    /// </summary>
    /// <param name="cart">The cart.</param>
    /// <param name="shippingByStore">The shipping costs by store. If null, shipping is not calculated.</param>
    /// <param name="discountInfo">The promo code discount information.</param>
    /// <returns>A successful result.</returns>
    public static GetCartResult Success(
        Domain.Entities.Cart? cart,
        IReadOnlyDictionary<Guid, StoreShippingCost>? shippingByStore,
        PromoCodeDiscountInfo discountInfo)
    {
        if (cart == null || cart.Items.Count == 0)
        {
            return new GetCartResult
            {
                Succeeded = true,
                Errors = [],
                Cart = cart,
                ItemsByStore = [],
                TotalItemCount = 0,
                TotalPrice = 0,
                Totals = null
            };
        }

        var itemsByStore = cart.Items
            .GroupBy(i => new { i.StoreId, i.StoreName })
            .Select(g =>
            {
                var storeGroup = new CartItemsByStore
                {
                    StoreId = g.Key.StoreId,
                    StoreName = g.Key.StoreName,
                    Items = g.ToList()
                };

                // Set shipping cost for this store if available
                if (shippingByStore != null && shippingByStore.TryGetValue(g.Key.StoreId, out var storeShipping))
                {
                    storeGroup.ShippingCost = storeShipping.ShippingCost;
                    storeGroup.IsFreeShipping = storeShipping.IsFreeShipping;
                    storeGroup.AmountToFreeShipping = storeShipping.AmountToFreeShipping;
                }

                return storeGroup;
            })
            .OrderBy(g => g.StoreName)
            .ToList();

        var totalItemCount = cart.Items.Sum(i => i.Quantity);
        var itemsSubtotal = cart.Items.Sum(i => i.ProductPrice * i.Quantity);

        // Calculate totals with shipping and discount if shipping data is provided
        CartTotals? totals = null;
        if (shippingByStore != null)
        {
            totals = CartTotals.Create(
                itemsSubtotal, 
                shippingByStore, 
                totalItemCount,
                discountInfo.DiscountAmount,
                discountInfo.AppliedPromoCode,
                discountInfo.AppliedPromoCodeDescription);
        }

        return new GetCartResult
        {
            Succeeded = true,
            Errors = [],
            Cart = cart,
            ItemsByStore = itemsByStore,
            TotalItemCount = totalItemCount,
            TotalPrice = itemsSubtotal,
            Totals = totals
        };
    }

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCartResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCartResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Represents a group of cart items from a single store.
/// </summary>
public class CartItemsByStore
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the items from this store.
    /// </summary>
    public IReadOnlyList<CartItem> Items { get; set; } = [];

    /// <summary>
    /// Gets the subtotal for items from this store.
    /// </summary>
    public decimal Subtotal => Items.Sum(i => i.ProductPrice * i.Quantity);

    /// <summary>
    /// Gets or sets the shipping cost for this store.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether free shipping is applied.
    /// </summary>
    public bool IsFreeShipping { get; set; }

    /// <summary>
    /// Gets or sets the amount needed to reach free shipping threshold.
    /// Null if free shipping is not configured or already applied.
    /// </summary>
    public decimal? AmountToFreeShipping { get; set; }

    /// <summary>
    /// Gets the total for this store (subtotal + shipping).
    /// </summary>
    public decimal StoreTotal => Subtotal + ShippingCost;
}
