namespace Mercato.Analytics.Domain.Entities;

/// <summary>
/// Defines the types of events that can be tracked for analytics.
/// </summary>
public enum AnalyticsEventType
{
    /// <summary>
    /// User performed a product search.
    /// </summary>
    Search = 1,

    /// <summary>
    /// User viewed a product details page.
    /// </summary>
    ProductView = 2,

    /// <summary>
    /// User added a product to cart.
    /// </summary>
    AddToCart = 3,

    /// <summary>
    /// User started the checkout process.
    /// </summary>
    CheckoutStart = 4,

    /// <summary>
    /// User completed an order.
    /// </summary>
    OrderCompletion = 5
}
