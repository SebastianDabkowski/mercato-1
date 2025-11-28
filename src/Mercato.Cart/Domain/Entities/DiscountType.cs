namespace Mercato.Cart.Domain.Entities;

/// <summary>
/// Represents the type of discount applied by a promo code.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// A percentage-based discount off the order total.
    /// </summary>
    Percentage = 0,

    /// <summary>
    /// A fixed monetary amount discount off the order total.
    /// </summary>
    FixedAmount = 1
}
