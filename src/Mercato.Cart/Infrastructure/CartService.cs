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
    private readonly IShippingCalculator _shippingCalculator;
    private readonly IPromoCodeService _promoCodeService;
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
    /// <param name="shippingCalculator">The shipping calculator.</param>
    /// <param name="promoCodeService">The promo code service.</param>
    /// <param name="logger">The logger.</param>
    public CartService(
        ICartRepository cartRepository,
        IProductService productService,
        IStoreProfileService storeProfileService,
        IShippingCalculator shippingCalculator,
        IPromoCodeService promoCodeService,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _productService = productService;
        _storeProfileService = storeProfileService;
        _shippingCalculator = shippingCalculator;
        _promoCodeService = promoCodeService;
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
            var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId!);
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

            return await AddItemToCartAsync(cart, command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for buyer {BuyerId}", command.BuyerId);
            return AddToCartResult.Failure("An error occurred while adding the item to cart.");
        }
    }

    /// <inheritdoc />
    public async Task<AddToCartResult> AddToGuestCartAsync(AddToCartCommand command)
    {
        var validationErrors = ValidateAddToGuestCartCommand(command);
        if (validationErrors.Count > 0)
        {
            return AddToCartResult.Failure(validationErrors);
        }

        try
        {
            // Get or create cart for guest
            var cart = await _cartRepository.GetByGuestCartIdAsync(command.GuestCartId!);
            if (cart == null)
            {
                cart = new Domain.Entities.Cart
                {
                    Id = Guid.NewGuid(),
                    GuestCartId = command.GuestCartId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                };
                await _cartRepository.AddAsync(cart);
            }

            return await AddItemToCartAsync(cart, command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to guest cart {GuestCartId}", command.GuestCartId);
            return AddToCartResult.Failure("An error occurred while adding the item to cart.");
        }
    }

    private async Task<AddToCartResult> AddItemToCartAsync(Domain.Entities.Cart cart, AddToCartCommand command)
    {
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
                "Updated cart item quantity for cart {CartId}, product {ProductId}, new quantity {Quantity}",
                cart.Id, command.ProductId, existingItem.Quantity);

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
            "Added item to cart {CartId}, product {ProductId}, quantity {Quantity}",
            cart.Id, command.ProductId, command.Quantity);

        return AddToCartResult.Success(cartItem.Id);
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
            return await BuildCartResultAsync(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for buyer {BuyerId}", query.BuyerId);
            return GetCartResult.Failure("An error occurred while retrieving the cart.");
        }
    }

    /// <inheritdoc />
    public async Task<GetCartResult> GetGuestCartAsync(string guestCartId)
    {
        if (string.IsNullOrEmpty(guestCartId))
        {
            return GetCartResult.Failure("Guest cart ID is required.");
        }

        try
        {
            var cart = await _cartRepository.GetByGuestCartIdAsync(guestCartId);
            return await BuildCartResultAsync(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guest cart {GuestCartId}", guestCartId);
            return GetCartResult.Failure("An error occurred while retrieving the cart.");
        }
    }

    private async Task<GetCartResult> BuildCartResultAsync(Domain.Entities.Cart? cart)
    {
        if (cart == null || cart.Items.Count == 0)
        {
            return GetCartResult.Success(cart);
        }

        // Build items by store for shipping calculation
        var itemsByStore = cart.Items
            .GroupBy(i => new { i.StoreId, i.StoreName })
            .Select(g => new CartItemsByStore
            {
                StoreId = g.Key.StoreId,
                StoreName = g.Key.StoreName,
                Items = g.ToList()
            })
            .ToList();

        // Calculate shipping costs
        var shippingByStore = await _shippingCalculator.CalculateShippingAsync(itemsByStore);

        // Calculate promo code discount
        var discountInfo = await _promoCodeService.CalculateDiscountAsync(cart);

        return GetCartResult.Success(cart, shippingByStore, discountInfo);
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

            return await UpdateItemQuantityAsync(cartItem, command.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item quantity for buyer {BuyerId}", command.BuyerId);
            return UpdateCartItemQuantityResult.Failure("An error occurred while updating the cart item.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateCartItemQuantityResult> UpdateGuestQuantityAsync(UpdateCartItemQuantityCommand command, string guestCartId)
    {
        if (string.IsNullOrEmpty(guestCartId))
        {
            return UpdateCartItemQuantityResult.Failure("Guest cart ID is required.");
        }

        if (command.CartItemId == Guid.Empty)
        {
            return UpdateCartItemQuantityResult.Failure("Cart item ID is required.");
        }

        try
        {
            var cartItem = await _cartRepository.GetItemByIdAsync(command.CartItemId);
            if (cartItem == null)
            {
                return UpdateCartItemQuantityResult.Failure("Cart item not found.");
            }

            // Verify ownership (guest cart)
            if (cartItem.Cart.GuestCartId != guestCartId)
            {
                return UpdateCartItemQuantityResult.NotAuthorized();
            }

            return await UpdateItemQuantityAsync(cartItem, command.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item quantity for guest cart {GuestCartId}", guestCartId);
            return UpdateCartItemQuantityResult.Failure("An error occurred while updating the cart item.");
        }
    }

    private async Task<UpdateCartItemQuantityResult> UpdateItemQuantityAsync(CartItem cartItem, int quantity)
    {
        if (quantity <= 0)
        {
            // Remove item if quantity is zero or less
            await _cartRepository.RemoveItemAsync(cartItem);

            cartItem.Cart.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _cartRepository.UpdateAsync(cartItem.Cart);

            _logger.LogInformation(
                "Removed cart item {CartItemId} due to zero quantity",
                cartItem.Id);
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

            if (quantity > product.Stock)
            {
                return UpdateCartItemQuantityResult.InsufficientStock(product.Stock);
            }

            cartItem.Quantity = quantity;
            cartItem.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _cartRepository.UpdateItemAsync(cartItem);

            cartItem.Cart.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _cartRepository.UpdateAsync(cartItem.Cart);

            _logger.LogInformation(
                "Updated cart item {CartItemId} quantity to {Quantity}",
                cartItem.Id, quantity);
        }

        return UpdateCartItemQuantityResult.Success();
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

            return await RemoveItemFromCartAsync(cartItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item for buyer {BuyerId}", command.BuyerId);
            return RemoveCartItemResult.Failure("An error occurred while removing the cart item.");
        }
    }

    /// <inheritdoc />
    public async Task<RemoveCartItemResult> RemoveGuestItemAsync(RemoveCartItemCommand command, string guestCartId)
    {
        if (string.IsNullOrEmpty(guestCartId))
        {
            return RemoveCartItemResult.Failure("Guest cart ID is required.");
        }

        try
        {
            var cartItem = await _cartRepository.GetItemByIdAsync(command.CartItemId);
            if (cartItem == null)
            {
                return RemoveCartItemResult.Failure("Cart item not found.");
            }

            // Verify ownership (guest cart)
            if (cartItem.Cart.GuestCartId != guestCartId)
            {
                return RemoveCartItemResult.NotAuthorized();
            }

            return await RemoveItemFromCartAsync(cartItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item for guest cart {GuestCartId}", guestCartId);
            return RemoveCartItemResult.Failure("An error occurred while removing the cart item.");
        }
    }

    private async Task<RemoveCartItemResult> RemoveItemFromCartAsync(CartItem cartItem)
    {
        await _cartRepository.RemoveItemAsync(cartItem);

        cartItem.Cart.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _cartRepository.UpdateAsync(cartItem.Cart);

        _logger.LogInformation("Removed cart item {CartItemId}", cartItem.Id);

        return RemoveCartItemResult.Success();
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

    /// <inheritdoc />
    public async Task<int> GetGuestCartItemCountAsync(string guestCartId)
    {
        if (string.IsNullOrEmpty(guestCartId))
        {
            return 0;
        }

        try
        {
            var cart = await _cartRepository.GetByGuestCartIdAsync(guestCartId);
            if (cart == null)
            {
                return 0;
            }

            return cart.Items.Sum(i => i.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count for guest cart {GuestCartId}", guestCartId);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<MergeGuestCartResult> MergeGuestCartAsync(MergeGuestCartCommand command)
    {
        var validationErrors = ValidateMergeGuestCartCommand(command);
        if (validationErrors.Count > 0)
        {
            return MergeGuestCartResult.Failure(validationErrors);
        }

        try
        {
            // Get the guest cart
            var guestCart = await _cartRepository.GetByGuestCartIdAsync(command.GuestCartId);
            if (guestCart == null || guestCart.Items.Count == 0)
            {
                _logger.LogInformation(
                    "No guest cart found or guest cart is empty for {GuestCartId}",
                    command.GuestCartId);
                return MergeGuestCartResult.NoGuestCart();
            }

            // Get or create the user cart
            var userCart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId);
            if (userCart == null)
            {
                userCart = new Domain.Entities.Cart
                {
                    Id = Guid.NewGuid(),
                    BuyerId = command.BuyerId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                };
                await _cartRepository.AddAsync(userCart);
            }

            var itemsMerged = 0;

            // Merge each item from guest cart to user cart
            foreach (var guestItem in guestCart.Items.ToList())
            {
                // Check if the same product exists in the user cart
                var existingItem = await _cartRepository.GetItemByProductIdAsync(userCart.Id, guestItem.ProductId);

                // Validate that the product is still available
                var product = await _productService.GetProductByIdAsync(guestItem.ProductId);
                if (product == null || product.Status != ProductStatus.Active)
                {
                    _logger.LogInformation(
                        "Skipping merge of product {ProductId} - no longer available",
                        guestItem.ProductId);
                    continue;
                }

                if (existingItem != null)
                {
                    // Sum quantities (respecting stock limits)
                    var newQuantity = Math.Min(existingItem.Quantity + guestItem.Quantity, product.Stock);
                    existingItem.Quantity = newQuantity;
                    existingItem.LastUpdatedAt = DateTimeOffset.UtcNow;
                    await _cartRepository.UpdateItemAsync(existingItem);

                    _logger.LogInformation(
                        "Merged guest cart item, product {ProductId}, new quantity {Quantity}",
                        guestItem.ProductId, newQuantity);
                }
                else
                {
                    // Create new item in user cart (respecting stock limits)
                    var quantity = Math.Min(guestItem.Quantity, product.Stock);
                    var newItem = new CartItem
                    {
                        Id = Guid.NewGuid(),
                        CartId = userCart.Id,
                        ProductId = guestItem.ProductId,
                        StoreId = guestItem.StoreId,
                        Quantity = quantity,
                        ProductTitle = guestItem.ProductTitle,
                        ProductPrice = guestItem.ProductPrice,
                        ProductImageUrl = guestItem.ProductImageUrl,
                        StoreName = guestItem.StoreName,
                        CreatedAt = DateTimeOffset.UtcNow,
                        LastUpdatedAt = DateTimeOffset.UtcNow
                    };
                    await _cartRepository.AddItemAsync(newItem);

                    _logger.LogInformation(
                        "Added guest cart item to user cart, product {ProductId}, quantity {Quantity}",
                        guestItem.ProductId, quantity);
                }

                itemsMerged++;
            }

            // Update user cart timestamp
            userCart.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _cartRepository.UpdateAsync(userCart);

            // Delete the guest cart
            await _cartRepository.DeleteAsync(guestCart);

            _logger.LogInformation(
                "Successfully merged guest cart {GuestCartId} into user cart for buyer {BuyerId}, {ItemsMerged} items merged",
                command.GuestCartId, command.BuyerId, itemsMerged);

            return MergeGuestCartResult.Success(itemsMerged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging guest cart {GuestCartId} for buyer {BuyerId}",
                command.GuestCartId, command.BuyerId);
            return MergeGuestCartResult.Failure("An error occurred while merging the carts.");
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

    private static List<string> ValidateAddToGuestCartCommand(AddToCartCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.GuestCartId))
        {
            errors.Add("Guest cart ID is required.");
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

    private static List<string> ValidateMergeGuestCartCommand(MergeGuestCartCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (string.IsNullOrEmpty(command.GuestCartId))
        {
            errors.Add("Guest cart ID is required.");
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
