namespace Mercato.Cart.Domain.Entities;

/// <summary>
/// Represents the scope of a promo code.
/// </summary>
public enum PromoCodeScope
{
    /// <summary>
    /// A platform-wide promo code issued by Mercato that applies to all orders.
    /// </summary>
    Platform = 0,

    /// <summary>
    /// A seller-specific promo code that only applies to items from a specific seller/store.
    /// </summary>
    Seller = 1
}
