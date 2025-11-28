namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Result of applying a promo code to a cart.
/// </summary>
public class ApplyPromoCodeResult
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
    /// Gets a value indicating whether the failure was due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the calculated discount amount.
    /// </summary>
    public decimal DiscountAmount { get; private init; }

    /// <summary>
    /// Gets the ID of the applied promo code.
    /// </summary>
    public Guid? PromoCodeId { get; private init; }

    /// <summary>
    /// Gets the description of the applied promo code.
    /// </summary>
    public string? PromoCodeDescription { get; private init; }

    /// <summary>
    /// Gets the applied promo code string.
    /// </summary>
    public string? AppliedPromoCode { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="promoCodeId">The ID of the applied promo code.</param>
    /// <param name="promoCode">The promo code string.</param>
    /// <param name="description">The promo code description.</param>
    /// <param name="discountAmount">The calculated discount amount.</param>
    /// <returns>A successful result.</returns>
    public static ApplyPromoCodeResult Success(
        Guid promoCodeId,
        string promoCode,
        string description,
        decimal discountAmount) => new()
    {
        Succeeded = true,
        Errors = [],
        PromoCodeId = promoCodeId,
        AppliedPromoCode = promoCode,
        PromoCodeDescription = description,
        DiscountAmount = discountAmount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a failed result for invalid promo code.
    /// </summary>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult InvalidCode() => Failure("The promo code is invalid or does not exist.");

    /// <summary>
    /// Creates a failed result for expired promo code.
    /// </summary>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult ExpiredCode() => Failure("This promo code has expired.");

    /// <summary>
    /// Creates a failed result when a promo code is already applied.
    /// </summary>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult AlreadyApplied() => 
        Failure("A promo code is already applied. Remove it first to apply a different code.");

    /// <summary>
    /// Creates a failed result when minimum order amount is not met.
    /// </summary>
    /// <param name="minimumAmount">The required minimum order amount.</param>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult MinimumNotMet(decimal minimumAmount) => 
        Failure($"This promo code requires a minimum order of {minimumAmount:C}.");

    /// <summary>
    /// Creates a failed result when the promo code is not applicable to the cart items.
    /// </summary>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult NotApplicable() => 
        Failure("This promo code is not applicable to the items in your cart.");

    /// <summary>
    /// Creates a failed result for usage limit exceeded.
    /// </summary>
    /// <returns>A failed result.</returns>
    public static ApplyPromoCodeResult UsageLimitReached() => 
        Failure("This promo code has reached its usage limit.");

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ApplyPromoCodeResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to modify this cart."]
    };
}
