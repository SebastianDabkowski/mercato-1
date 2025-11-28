using System.Text.Json;
using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Entities;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.Extensions.Logging;

namespace Mercato.Cart.Infrastructure;

/// <summary>
/// Service implementation for shopping cart operations.
/// </summary>
public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<CartService> _logger;

    private const string PlaceholderImage = "/images/placeholder.png";
    private static readonly string[] AllowedImagePrefixes = ["/uploads/", "/images/"];
    private static readonly JsonSerializerOptions ImageJsonOptions = new() { MaxDepth = 2 };

    /// <summary>
    /// Initializes a new instance of the <see cref="CartService"/> class.
    /// </summary>
    /// <param name="cartRepository">The cart repository.</param>
    /// <param name="productService">The product service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public CartService(
        ICartRepository cartRepository,
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AddToCartResult> AddToCartAsync(AddToCartCommand command)
    {
        var validationErrors = ValidateAddToCartCommand(command);
        if (validationErrors.Count > 0)
        {
            return AddToCartResult.Failure(validationErrors);
        }

        try
        {
            // Get or create cart for buyer
            var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId);
            if (cart == null)
            {
                cart = new Domain.Entities.Cart
                {
                    Id = Guid.NewGuid(),
                    BuyerId = command.BuyerId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                };
                await _cartRepository.AddAsync(cart);
            }

            // Check if item already exists in cart
            var existingItem = await _cartRepository.GetItemByProductIdAsync(cart.Id, command.ProductId);
            if (existingItem != null)
            {
                // Validate total quantity against current product stock
                var product = await _productService.GetProductByIdAsync(command.ProductId);
                if (product == null)
                {
                    return AddToCartResult.Failure("Product is no longer available.");
                }

                if (product.Status != ProductStatus.Active)
                {
                    return AddToCartResult.Failure("Product is no longer available for purchase.");
                }

                var newQuantity = existingItem.Quantity + command.Quantity;
                if (newQuantity > product.Stock)
                {
                    return AddToCartResult.InsufficientStock(product.Stock);
                }

                // Update quantity
                existingItem.Quantity = newQuantity;
                existingItem.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _cartRepository.UpdateItemAsync(existingItem);

                cart.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _cartRepository.UpdateAsync(cart);

                _logger.LogInformation(
                    "Updated cart item quantity for buyer {BuyerId}, product {ProductId}, new quantity {Quantity}",
                    command.BuyerId, command.ProductId, existingItem.Quantity);

                return AddToCartResult.Success(existingItem.Id, itemAlreadyExists: true);
            }

            // Get product information
            var productInfo = await _productService.GetProductByIdAsync(command.ProductId);
            if (productInfo == null)
            {
                return AddToCartResult.Failure("Product not found.");
            }

            if (productInfo.Status != ProductStatus.Active)
            {
                return AddToCartResult.Failure("Product is not available.");
            }

            // Validate quantity against available stock
            if (command.Quantity > productInfo.Stock)
            {
                return AddToCartResult.InsufficientStock(productInfo.Stock);
            }

            // Get store information
            var store = await _storeProfileService.GetStoreByIdAsync(productInfo.StoreId);
            if (store == null)
            {
                return AddToCartResult.Failure("Store not found.");
            }

            // Create new cart item with snapshotted data
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = productInfo.Id,
                StoreId = productInfo.StoreId,
                Quantity = command.Quantity,
                ProductTitle = productInfo.Title,
                ProductPrice = productInfo.Price,
                ProductImageUrl = GetFirstImageUrl(productInfo.Images),
                StoreName = store.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            await _cartRepository.AddItemAsync(cartItem);

            cart.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _cartRepository.UpdateAsync(cart);

            _logger.LogInformation(
                "Added item to cart for buyer {BuyerId}, product {ProductId}, quantity {Quantity}",
                command.BuyerId, command.ProductId, command.Quantity);

            return AddToCartResult.Success(cartItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for buyer {BuyerId}", command.BuyerId);
            return AddToCartResult.Failure("An error occurred while adding the item to cart.");
        }
    }

    /// <inheritdoc />
    public async Task<GetCartResult> GetCartAsync(GetCartQuery query)
    {
        if (string.IsNullOrEmpty(query.BuyerId))
        {
            return GetCartResult.Failure("Buyer ID is required.");
        }

        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(query.BuyerId);
            return GetCartResult.Success(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for buyer {BuyerId}", query.BuyerId);
            return GetCartResult.Failure("An error occurred while retrieving the cart.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateCartItemQuantityResult> UpdateQuantityAsync(UpdateCartItemQuantityCommand command)
    {
        var validationErrors = ValidateUpdateQuantityCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateCartItemQuantityResult.Failure(validationErrors);
        }

        try
        {
            var cartItem = await _cartRepository.GetItemByIdAsync(command.CartItemId);
            if (cartItem == null)
            {
                return UpdateCartItemQuantityResult.Failure("Cart item not found.");
            }

            // Verify ownership
            if (cartItem.Cart.BuyerId != command.BuyerId)
            {
                return UpdateCartItemQuantityResult.NotAuthorized();
            }

            if (command.Quantity <= 0)
            {
                // Remove item if quantity is zero or less
                await _cartRepository.RemoveItemAsync(cartItem);

                cartItem.Cart.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _cartRepository.UpdateAsync(cartItem.Cart);

                _logger.LogInformation(
                    "Removed cart item {CartItemId} for buyer {BuyerId} due to zero quantity",
                    command.CartItemId, command.BuyerId);
            }
            else
            {
                // Validate quantity against current product stock
                var product = await _productService.GetProductByIdAsync(cartItem.ProductId);
                if (product == null)
                {
                    return UpdateCartItemQuantityResult.Failure("Product is no longer available.");
                }

                if (product.Status != Product.Domain.Entities.ProductStatus.Active)
                {
                    return UpdateCartItemQuantityResult.Failure("Product is no longer available for purchase.");
                }

                if (command.Quantity > product.Stock)
                {
                    return UpdateCartItemQuantityResult.InsufficientStock(product.Stock);
                }

                cartItem.Quantity = command.Quantity;
                cartItem.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _cartRepository.UpdateItemAsync(cartItem);

                cartItem.Cart.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _cartRepository.UpdateAsync(cartItem.Cart);

                _logger.LogInformation(
                    "Updated cart item {CartItemId} quantity to {Quantity} for buyer {BuyerId}",
                    command.CartItemId, command.Quantity, command.BuyerId);
            }

            return UpdateCartItemQuantityResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item quantity for buyer {BuyerId}", command.BuyerId);
            return UpdateCartItemQuantityResult.Failure("An error occurred while updating the cart item.");
        }
    }

    /// <inheritdoc />
    public async Task<RemoveCartItemResult> RemoveItemAsync(RemoveCartItemCommand command)
    {
        if (string.IsNullOrEmpty(command.BuyerId))
        {
            return RemoveCartItemResult.Failure("Buyer ID is required.");
        }

        try
        {
            var cartItem = await _cartRepository.GetItemByIdAsync(command.CartItemId);
            if (cartItem == null)
            {
                return RemoveCartItemResult.Failure("Cart item not found.");
            }

            // Verify ownership
            if (cartItem.Cart.BuyerId != command.BuyerId)
            {
                return RemoveCartItemResult.NotAuthorized();
            }

            await _cartRepository.RemoveItemAsync(cartItem);

            cartItem.Cart.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _cartRepository.UpdateAsync(cartItem.Cart);

            _logger.LogInformation(
                "Removed cart item {CartItemId} for buyer {BuyerId}",
                command.CartItemId, command.BuyerId);

            return RemoveCartItemResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item for buyer {BuyerId}", command.BuyerId);
            return RemoveCartItemResult.Failure("An error occurred while removing the cart item.");
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCartItemCountAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return 0;
        }

        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(buyerId);
            if (cart == null)
            {
                return 0;
            }

            return cart.Items.Sum(i => i.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count for buyer {BuyerId}", buyerId);
            return 0;
        }
    }

    private static List<string> ValidateAddToCartCommand(AddToCartCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (command.Quantity <= 0)
        {
            errors.Add("Quantity must be greater than zero.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateQuantityCommand(UpdateCartItemQuantityCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.CartItemId == Guid.Empty)
        {
            errors.Add("Cart item ID is required.");
        }

        return errors;
    }

    private static string GetFirstImageUrl(string? images)
    {
        if (string.IsNullOrEmpty(images) || images == "[]")
        {
            return PlaceholderImage;
        }

        try
        {
            var imageArray = JsonSerializer.Deserialize<string[]>(images, ImageJsonOptions);

            if (imageArray == null || imageArray.Length == 0)
            {
                return PlaceholderImage;
            }

            var imageUrl = imageArray[0];
            if (IsValidImageUrl(imageUrl))
            {
                return imageUrl;
            }

            return PlaceholderImage;
        }
        catch (JsonException)
        {
            // Invalid JSON format for images, return placeholder
            return PlaceholderImage;
        }
    }

    private static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Prevent path traversal attacks
        if (url.Contains("..") || url.Contains("//") || url.Contains("\\"))
        {
            return false;
        }

        foreach (var prefix in AllowedImagePrefixes)
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
