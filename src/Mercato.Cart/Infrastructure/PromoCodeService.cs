using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Entities;
using Mercato.Cart.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Cart.Infrastructure;

/// <summary>
/// Service implementation for promo code operations.
/// </summary>
public class PromoCodeService : IPromoCodeService
{
    private readonly ICartRepository _cartRepository;
    private readonly IPromoCodeRepository _promoCodeRepository;
    private readonly ILogger<PromoCodeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromoCodeService"/> class.
    /// </summary>
    /// <param name="cartRepository">The cart repository.</param>
    /// <param name="promoCodeRepository">The promo code repository.</param>
    /// <param name="logger">The logger.</param>
    public PromoCodeService(
        ICartRepository cartRepository,
        IPromoCodeRepository promoCodeRepository,
        ILogger<PromoCodeService> logger)
    {
        _cartRepository = cartRepository;
        _promoCodeRepository = promoCodeRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApplyPromoCodeResult> ApplyPromoCodeAsync(ApplyPromoCodeCommand command)
    {
        var validationErrors = ValidateApplyPromoCodeCommand(command, isGuest: false);
        if (validationErrors.Count > 0)
        {
            return ApplyPromoCodeResult.Failure(validationErrors);
        }

        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId!);
            if (cart == null)
            {
                return ApplyPromoCodeResult.Failure("Cart not found.");
            }

            return await ApplyPromoCodeToCartAsync(cart, command.PromoCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promo code for buyer {BuyerId}", command.BuyerId);
            return ApplyPromoCodeResult.Failure("An error occurred while applying the promo code.");
        }
    }

    /// <inheritdoc />
    public async Task<ApplyPromoCodeResult> ApplyPromoCodeToGuestCartAsync(ApplyPromoCodeCommand command)
    {
        var validationErrors = ValidateApplyPromoCodeCommand(command, isGuest: true);
        if (validationErrors.Count > 0)
        {
            return ApplyPromoCodeResult.Failure(validationErrors);
        }

        try
        {
            var cart = await _cartRepository.GetByGuestCartIdAsync(command.GuestCartId!);
            if (cart == null)
            {
                return ApplyPromoCodeResult.Failure("Cart not found.");
            }

            return await ApplyPromoCodeToCartAsync(cart, command.PromoCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promo code for guest cart {GuestCartId}", command.GuestCartId);
            return ApplyPromoCodeResult.Failure("An error occurred while applying the promo code.");
        }
    }

    private async Task<ApplyPromoCodeResult> ApplyPromoCodeToCartAsync(Domain.Entities.Cart cart, string promoCodeString)
    {
        // Check if a promo code is already applied
        if (cart.AppliedPromoCodeId.HasValue)
        {
            return ApplyPromoCodeResult.AlreadyApplied();
        }

        // Check if cart has items
        if (cart.Items.Count == 0)
        {
            return ApplyPromoCodeResult.Failure("Cannot apply promo code to an empty cart.");
        }

        // Find the promo code
        var promoCode = await _promoCodeRepository.GetByCodeAsync(promoCodeString);
        if (promoCode == null)
        {
            return ApplyPromoCodeResult.InvalidCode();
        }

        // Validate the promo code
        var validationResult = ValidatePromoCode(promoCode, cart);
        if (!validationResult.IsValid)
        {
            return validationResult.Result!;
        }

        // Calculate the discount
        var discountAmount = CalculateDiscountForCart(promoCode, cart);

        // Apply the promo code to the cart
        cart.AppliedPromoCodeId = promoCode.Id;
        cart.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _cartRepository.UpdateAsync(cart);

        _logger.LogInformation(
            "Applied promo code {PromoCode} to cart {CartId}, discount amount: {DiscountAmount}",
            promoCode.Code, cart.Id, discountAmount);

        return ApplyPromoCodeResult.Success(
            promoCode.Id,
            promoCode.Code,
            promoCode.Description,
            discountAmount);
    }

    /// <inheritdoc />
    public async Task<RemovePromoCodeResult> RemovePromoCodeAsync(RemovePromoCodeCommand command)
    {
        var validationErrors = ValidateRemovePromoCodeCommand(command, isGuest: false);
        if (validationErrors.Count > 0)
        {
            return RemovePromoCodeResult.Failure(validationErrors);
        }

        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId!);
            if (cart == null)
            {
                return RemovePromoCodeResult.Failure("Cart not found.");
            }

            return await RemovePromoCodeFromCartAsync(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing promo code for buyer {BuyerId}", command.BuyerId);
            return RemovePromoCodeResult.Failure("An error occurred while removing the promo code.");
        }
    }

    /// <inheritdoc />
    public async Task<RemovePromoCodeResult> RemovePromoCodeFromGuestCartAsync(RemovePromoCodeCommand command)
    {
        var validationErrors = ValidateRemovePromoCodeCommand(command, isGuest: true);
        if (validationErrors.Count > 0)
        {
            return RemovePromoCodeResult.Failure(validationErrors);
        }

        try
        {
            var cart = await _cartRepository.GetByGuestCartIdAsync(command.GuestCartId!);
            if (cart == null)
            {
                return RemovePromoCodeResult.Failure("Cart not found.");
            }

            return await RemovePromoCodeFromCartAsync(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing promo code for guest cart {GuestCartId}", command.GuestCartId);
            return RemovePromoCodeResult.Failure("An error occurred while removing the promo code.");
        }
    }

    private async Task<RemovePromoCodeResult> RemovePromoCodeFromCartAsync(Domain.Entities.Cart cart)
    {
        if (!cart.AppliedPromoCodeId.HasValue)
        {
            return RemovePromoCodeResult.Success(); // No promo code to remove
        }

        cart.AppliedPromoCodeId = null;
        cart.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _cartRepository.UpdateAsync(cart);

        _logger.LogInformation("Removed promo code from cart {CartId}", cart.Id);

        return RemovePromoCodeResult.Success();
    }

    /// <inheritdoc />
    public async Task<PromoCodeDiscountInfo> CalculateDiscountAsync(Domain.Entities.Cart cart)
    {
        if (!cart.AppliedPromoCodeId.HasValue)
        {
            return PromoCodeDiscountInfo.None();
        }

        var promoCode = await _promoCodeRepository.GetByIdAsync(cart.AppliedPromoCodeId.Value);
        if (promoCode == null)
        {
            return PromoCodeDiscountInfo.None();
        }

        // Validate promo code is still valid
        if (!promoCode.IsValid(DateTimeOffset.UtcNow))
        {
            return PromoCodeDiscountInfo.None();
        }

        // Calculate the discount
        var discountAmount = CalculateDiscountForCart(promoCode, cart);

        return PromoCodeDiscountInfo.Create(
            discountAmount,
            promoCode.Id,
            promoCode.Code,
            promoCode.Description);
    }

    private static (bool IsValid, ApplyPromoCodeResult? Result) ValidatePromoCode(PromoCode promoCode, Domain.Entities.Cart cart)
    {
        var currentTime = DateTimeOffset.UtcNow;

        // Check if promo code is active and within valid date range
        if (!promoCode.IsValid(currentTime))
        {
            if (!promoCode.IsActive)
            {
                return (false, ApplyPromoCodeResult.InvalidCode());
            }

            if (currentTime < promoCode.StartDate)
            {
                return (false, ApplyPromoCodeResult.Failure("This promo code is not yet active."));
            }

            if (promoCode.EndDate.HasValue && currentTime > promoCode.EndDate.Value)
            {
                return (false, ApplyPromoCodeResult.ExpiredCode());
            }

            if (promoCode.UsageLimit.HasValue && promoCode.UsageCount >= promoCode.UsageLimit.Value)
            {
                return (false, ApplyPromoCodeResult.UsageLimitReached());
            }
        }

        // Calculate the subtotal for the applicable items
        var applicableSubtotal = CalculateApplicableSubtotal(promoCode, cart);

        if (applicableSubtotal == 0)
        {
            return (false, ApplyPromoCodeResult.NotApplicable());
        }

        // Check minimum order amount
        if (!promoCode.MeetsMinimumOrderAmount(applicableSubtotal))
        {
            return (false, ApplyPromoCodeResult.MinimumNotMet(promoCode.MinimumOrderAmount!.Value));
        }

        return (true, null);
    }

    private static decimal CalculateApplicableSubtotal(PromoCode promoCode, Domain.Entities.Cart cart)
    {
        if (promoCode.Scope == PromoCodeScope.Platform)
        {
            // Platform-wide code applies to all items
            return cart.Items.Sum(i => i.ProductPrice * i.Quantity);
        }

        // Seller-specific code applies only to items from that store
        if (!promoCode.StoreId.HasValue)
        {
            return 0;
        }

        return cart.Items
            .Where(i => i.StoreId == promoCode.StoreId.Value)
            .Sum(i => i.ProductPrice * i.Quantity);
    }

    private static decimal CalculateDiscountForCart(PromoCode promoCode, Domain.Entities.Cart cart)
    {
        var applicableSubtotal = CalculateApplicableSubtotal(promoCode, cart);
        return promoCode.CalculateDiscount(applicableSubtotal);
    }

    private static List<string> ValidateApplyPromoCodeCommand(ApplyPromoCodeCommand command, bool isGuest)
    {
        var errors = new List<string>();

        if (isGuest)
        {
            if (string.IsNullOrEmpty(command.GuestCartId))
            {
                errors.Add("Guest cart ID is required.");
            }
        }
        else
        {
            if (string.IsNullOrEmpty(command.BuyerId))
            {
                errors.Add("Buyer ID is required.");
            }
        }

        if (string.IsNullOrWhiteSpace(command.PromoCode))
        {
            errors.Add("Promo code is required.");
        }

        return errors;
    }

    private static List<string> ValidateRemovePromoCodeCommand(RemovePromoCodeCommand command, bool isGuest)
    {
        var errors = new List<string>();

        if (isGuest)
        {
            if (string.IsNullOrEmpty(command.GuestCartId))
            {
                errors.Add("Guest cart ID is required.");
            }
        }
        else
        {
            if (string.IsNullOrEmpty(command.BuyerId))
            {
                errors.Add("Buyer ID is required.");
            }
        }

        return errors;
    }
}
