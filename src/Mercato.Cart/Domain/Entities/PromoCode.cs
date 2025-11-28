namespace Mercato.Cart.Domain.Entities;

/// <summary>
/// Represents a promo code that can be applied to a cart for discounts.
/// </summary>
public class PromoCode
{
    /// <summary>
    /// Gets or sets the unique identifier for the promo code.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the promo code string that users enter.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the promo code for display purposes.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of discount (percentage or fixed amount).
    /// </summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// Gets or sets the discount value. For percentage discounts, this is the percentage (e.g., 10 for 10%).
    /// For fixed amount discounts, this is the monetary value.
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum order amount required to use this promo code.
    /// Null means no minimum requirement.
    /// </summary>
    public decimal? MinimumOrderAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum discount amount for percentage-based discounts.
    /// Null means no cap on the discount.
    /// </summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the scope of the promo code (platform-wide or seller-specific).
    /// </summary>
    public PromoCodeScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the seller ID for seller-scoped promo codes.
    /// Null for platform-wide codes.
    /// </summary>
    public string? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the store ID for seller-scoped promo codes.
    /// Null for platform-wide codes.
    /// </summary>
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the promo code becomes valid.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the promo code expires.
    /// Null means no expiration.
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of times this promo code can be used.
    /// Null means unlimited uses.
    /// </summary>
    public int? UsageLimit { get; set; }

    /// <summary>
    /// Gets or sets the current number of times this promo code has been used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the promo code is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the promo code was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the promo code was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Determines whether the promo code is currently valid based on dates, usage, and active status.
    /// </summary>
    /// <param name="currentTime">The current time to check against.</param>
    /// <returns>True if the promo code is valid; otherwise, false.</returns>
    public bool IsValid(DateTimeOffset currentTime)
    {
        if (!IsActive)
        {
            return false;
        }

        if (currentTime < StartDate)
        {
            return false;
        }

        if (EndDate.HasValue && currentTime > EndDate.Value)
        {
            return false;
        }

        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the promo code can be applied to the specified order amount.
    /// </summary>
    /// <param name="orderAmount">The order amount to check against the minimum requirement.</param>
    /// <returns>True if the minimum order requirement is met; otherwise, false.</returns>
    public bool MeetsMinimumOrderAmount(decimal orderAmount)
    {
        if (!MinimumOrderAmount.HasValue)
        {
            return true;
        }

        return orderAmount >= MinimumOrderAmount.Value;
    }

    /// <summary>
    /// Calculates the discount amount for a given order subtotal.
    /// </summary>
    /// <param name="subtotal">The order subtotal to apply the discount to.</param>
    /// <returns>The calculated discount amount.</returns>
    public decimal CalculateDiscount(decimal subtotal)
    {
        decimal discount;

        if (DiscountType == DiscountType.Percentage)
        {
            discount = subtotal * (DiscountValue / 100m);

            // Apply maximum discount cap if set
            if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            {
                discount = MaxDiscountAmount.Value;
            }
        }
        else
        {
            // Fixed amount discount
            discount = DiscountValue;
        }

        // Ensure discount doesn't exceed subtotal
        if (discount > subtotal)
        {
            discount = subtotal;
        }

        return Math.Round(discount, 2);
    }
}
