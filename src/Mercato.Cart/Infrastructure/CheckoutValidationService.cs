using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Mercato.Cart.Infrastructure;

/// <summary>
/// Service implementation for checkout validation operations.
/// Validates stock availability and price changes before order creation.
/// </summary>
public class CheckoutValidationService : ICheckoutValidationService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductService _productService;
    private readonly ILogger<CheckoutValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckoutValidationService"/> class.
    /// </summary>
    /// <param name="cartRepository">The cart repository.</param>
    /// <param name="productService">The product service.</param>
    /// <param name="logger">The logger.</param>
    public CheckoutValidationService(
        ICartRepository cartRepository,
        IProductService productService,
        ILogger<CheckoutValidationService> logger)
    {
        _cartRepository = cartRepository;
        _productService = productService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidateCheckoutResult> ValidateCheckoutAsync(ValidateCheckoutCommand command)
    {
        var validationErrors = ValidateCommand(command);
        if (validationErrors.Count > 0)
        {
            return ValidateCheckoutResult.Failure(validationErrors);
        }

        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId);
            if (cart == null || cart.Items.Count == 0)
            {
                return ValidateCheckoutResult.Failure("Cart is empty.");
            }

            var stockIssues = new List<StockValidationIssue>();
            var priceChanges = new List<PriceChangeIssue>();
            var validatedItems = new List<ValidatedCartItem>();

            foreach (var cartItem in cart.Items)
            {
                var product = await _productService.GetProductByIdAsync(cartItem.ProductId);

                // Check if product is available
                if (product == null || product.Status != ProductStatus.Active)
                {
                    stockIssues.Add(new StockValidationIssue
                    {
                        CartItemId = cartItem.Id,
                        ProductId = cartItem.ProductId,
                        ProductTitle = cartItem.ProductTitle,
                        RequestedQuantity = cartItem.Quantity,
                        AvailableStock = 0,
                        IsUnavailable = true
                    });
                    continue;
                }

                // Check stock availability
                if (cartItem.Quantity > product.Stock)
                {
                    stockIssues.Add(new StockValidationIssue
                    {
                        CartItemId = cartItem.Id,
                        ProductId = cartItem.ProductId,
                        ProductTitle = cartItem.ProductTitle,
                        RequestedQuantity = cartItem.Quantity,
                        AvailableStock = product.Stock,
                        IsUnavailable = false
                    });
                }

                // Check for price changes
                if (cartItem.ProductPrice != product.Price)
                {
                    priceChanges.Add(new PriceChangeIssue
                    {
                        CartItemId = cartItem.Id,
                        ProductId = cartItem.ProductId,
                        ProductTitle = cartItem.ProductTitle,
                        OriginalPrice = cartItem.ProductPrice,
                        CurrentPrice = product.Price
                    });
                }

                // Add to validated items (with current price)
                validatedItems.Add(new ValidatedCartItem
                {
                    CartItemId = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    StoreId = cartItem.StoreId,
                    ProductTitle = product.Title,
                    UnitPrice = product.Price,
                    Quantity = cartItem.Quantity,
                    StoreName = cartItem.StoreName
                });
            }

            // If there are any stock issues or price changes, return validation failed
            if (stockIssues.Count > 0 || priceChanges.Count > 0)
            {
                _logger.LogInformation(
                    "Checkout validation failed for buyer {BuyerId}: {StockIssueCount} stock issues, {PriceChangeCount} price changes",
                    command.BuyerId, stockIssues.Count, priceChanges.Count);

                return ValidateCheckoutResult.ValidationFailed(stockIssues, priceChanges);
            }

            _logger.LogInformation(
                "Checkout validation succeeded for buyer {BuyerId} with {ItemCount} items",
                command.BuyerId, validatedItems.Count);

            return ValidateCheckoutResult.Success(validatedItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating checkout for buyer {BuyerId}", command.BuyerId);
            return ValidateCheckoutResult.Failure("An error occurred while validating checkout.");
        }
    }

    /// <inheritdoc />
    public async Task UpdateCartPricesToCurrentAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return;
        }

        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(buyerId);
            if (cart == null || cart.Items.Count == 0)
            {
                return;
            }

            var updated = false;
            foreach (var cartItem in cart.Items)
            {
                var product = await _productService.GetProductByIdAsync(cartItem.ProductId);
                if (product != null && product.Status == ProductStatus.Active && cartItem.ProductPrice != product.Price)
                {
                    cartItem.ProductPrice = product.Price;
                    cartItem.LastUpdatedAt = DateTimeOffset.UtcNow;
                    await _cartRepository.UpdateItemAsync(cartItem);
                    updated = true;
                }
            }

            if (updated)
            {
                cart.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _cartRepository.UpdateAsync(cart);

                _logger.LogInformation(
                    "Updated cart prices to current for buyer {BuyerId}",
                    buyerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart prices for buyer {BuyerId}", buyerId);
        }
    }

    private static List<string> ValidateCommand(ValidateCheckoutCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        return errors;
    }
}
