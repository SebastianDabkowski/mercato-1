using Mercato.Cart.Application.Commands;

namespace Mercato.Cart.Application.Services;

/// <summary>
/// Service interface for promo code operations.
/// </summary>
public interface IPromoCodeService
{
    /// <summary>
    /// Applies a promo code to a cart.
    /// </summary>
    /// <param name="command">The apply promo code command.</param>
    /// <returns>The result of the apply operation.</returns>
    Task<ApplyPromoCodeResult> ApplyPromoCodeAsync(ApplyPromoCodeCommand command);

    /// <summary>
    /// Applies a promo code to a guest cart.
    /// </summary>
    /// <param name="command">The apply promo code command.</param>
    /// <returns>The result of the apply operation.</returns>
    Task<ApplyPromoCodeResult> ApplyPromoCodeToGuestCartAsync(ApplyPromoCodeCommand command);

    /// <summary>
    /// Removes a promo code from a cart.
    /// </summary>
    /// <param name="command">The remove promo code command.</param>
    /// <returns>The result of the remove operation.</returns>
    Task<RemovePromoCodeResult> RemovePromoCodeAsync(RemovePromoCodeCommand command);

    /// <summary>
    /// Removes a promo code from a guest cart.
    /// </summary>
    /// <param name="command">The remove promo code command.</param>
    /// <returns>The result of the remove operation.</returns>
    Task<RemovePromoCodeResult> RemovePromoCodeFromGuestCartAsync(RemovePromoCodeCommand command);

    /// <summary>
    /// Calculates the discount for a cart with an applied promo code.
    /// </summary>
    /// <param name="cart">The cart to calculate the discount for.</param>
    /// <returns>The discount information containing the discount amount and promo code details.</returns>
    Task<PromoCodeDiscountInfo> CalculateDiscountAsync(Domain.Entities.Cart cart);
}

/// <summary>
/// Contains discount information for a cart.
/// </summary>
public class PromoCodeDiscountInfo
{
    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the applied promo code string.
    /// </summary>
    public string? AppliedPromoCode { get; set; }

    /// <summary>
    /// Gets or sets the description of the applied promo code.
    /// </summary>
    public string? AppliedPromoCodeDescription { get; set; }

    /// <summary>
    /// Gets or sets the ID of the applied promo code.
    /// </summary>
    public Guid? AppliedPromoCodeId { get; set; }

    /// <summary>
    /// Creates an empty discount info indicating no discount.
    /// </summary>
    /// <returns>An empty discount info.</returns>
    public static PromoCodeDiscountInfo None() => new()
    {
        DiscountAmount = 0,
        AppliedPromoCode = null,
        AppliedPromoCodeDescription = null,
        AppliedPromoCodeId = null
    };

    /// <summary>
    /// Creates a discount info with the specified values.
    /// </summary>
    /// <param name="discountAmount">The discount amount.</param>
    /// <param name="promoCodeId">The promo code ID.</param>
    /// <param name="promoCode">The promo code string.</param>
    /// <param name="description">The promo code description.</param>
    /// <returns>A discount info with the specified values.</returns>
    public static PromoCodeDiscountInfo Create(
        decimal discountAmount,
        Guid promoCodeId,
        string promoCode,
        string description) => new()
    {
        DiscountAmount = discountAmount,
        AppliedPromoCodeId = promoCodeId,
        AppliedPromoCode = promoCode,
        AppliedPromoCodeDescription = description
    };
}
