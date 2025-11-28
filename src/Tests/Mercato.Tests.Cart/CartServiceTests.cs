using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Queries;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Cart.Infrastructure;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using CartEntity = Mercato.Cart.Domain.Entities.Cart;
using CartItemEntity = Mercato.Cart.Domain.Entities.CartItem;
using StoreEntity = Mercato.Seller.Domain.Entities.Store;
using ProductEntity = Mercato.Product.Domain.Entities.Product;

namespace Mercato.Tests.Cart;

public class CartServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestCartId = Guid.NewGuid();
    private static readonly Guid TestCartItemId = Guid.NewGuid();

    private readonly Mock<ICartRepository> _mockCartRepository;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<IStoreProfileService> _mockStoreProfileService;
    private readonly Mock<ILogger<CartService>> _mockLogger;
    private readonly CartService _service;

    public CartServiceTests()
    {
        _mockCartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        _mockProductService = new Mock<IProductService>(MockBehavior.Strict);
        _mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CartService>>();
        _service = new CartService(
            _mockCartRepository.Object,
            _mockProductService.Object,
            _mockStoreProfileService.Object,
            _mockLogger.Object);
    }

    #region AddToCartAsync Tests

    [Fact]
    public async Task AddToCartAsync_ValidCommand_NewCart_ReturnsSuccess()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = TestBuyerId,
            ProductId = TestProductId,
            Quantity = 2
        };

        var product = CreateTestProduct();
        var store = CreateTestStore();

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        _mockCartRepository.Setup(r => r.AddAsync(It.IsAny<CartEntity>()))
            .ReturnsAsync((CartEntity c) => c);

        _mockCartRepository.Setup(r => r.GetItemByProductIdAsync(It.IsAny<Guid>(), TestProductId))
            .ReturnsAsync((CartItemEntity?)null);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        _mockStoreProfileService.Setup(s => s.GetStoreByIdAsync(TestStoreId))
            .ReturnsAsync(store);

        _mockCartRepository.Setup(r => r.AddItemAsync(It.IsAny<CartItemEntity>()))
            .ReturnsAsync((CartItemEntity i) => i);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.CartItemId);
        Assert.False(result.ItemAlreadyExists);
        _mockCartRepository.Verify(r => r.AddAsync(It.IsAny<CartEntity>()), Times.Once);
        _mockCartRepository.Verify(r => r.AddItemAsync(It.Is<CartItemEntity>(i =>
            i.ProductId == TestProductId &&
            i.Quantity == 2 &&
            i.StoreId == TestStoreId &&
            i.ProductTitle == product.Title &&
            i.ProductPrice == product.Price)), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_ValidCommand_ExistingCart_UpdatesQuantity()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = TestBuyerId,
            ProductId = TestProductId,
            Quantity = 3
        };

        var existingCart = CreateTestCart();
        var existingItem = new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(existingCart);

        _mockCartRepository.Setup(r => r.GetItemByProductIdAsync(TestCartId, TestProductId))
            .ReturnsAsync(existingItem);

        _mockCartRepository.Setup(r => r.UpdateItemAsync(It.IsAny<CartItemEntity>()))
            .Returns(Task.CompletedTask);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(TestCartItemId, result.CartItemId);
        Assert.True(result.ItemAlreadyExists);
        _mockCartRepository.Verify(r => r.UpdateItemAsync(It.Is<CartItemEntity>(i =>
            i.Id == TestCartItemId &&
            i.Quantity == 5)), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = string.Empty,
            ProductId = TestProductId,
            Quantity = 1
        };

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task AddToCartAsync_EmptyProductId_ReturnsFailure()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = TestBuyerId,
            ProductId = Guid.Empty,
            Quantity = 1
        };

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task AddToCartAsync_ZeroQuantity_ReturnsFailure()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = TestBuyerId,
            ProductId = TestProductId,
            Quantity = 0
        };

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Quantity must be greater than zero.", result.Errors);
    }

    [Fact]
    public async Task AddToCartAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = TestBuyerId,
            ProductId = TestProductId,
            Quantity = 1
        };

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        _mockCartRepository.Setup(r => r.AddAsync(It.IsAny<CartEntity>()))
            .ReturnsAsync((CartEntity c) => c);

        _mockCartRepository.Setup(r => r.GetItemByProductIdAsync(It.IsAny<Guid>(), TestProductId))
            .ReturnsAsync((CartItemEntity?)null);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync((ProductEntity?)null);

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task AddToCartAsync_ProductNotActive_ReturnsFailure()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            BuyerId = TestBuyerId,
            ProductId = TestProductId,
            Quantity = 1
        };

        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        _mockCartRepository.Setup(r => r.AddAsync(It.IsAny<CartEntity>()))
            .ReturnsAsync((CartEntity c) => c);

        _mockCartRepository.Setup(r => r.GetItemByProductIdAsync(It.IsAny<Guid>(), TestProductId))
            .ReturnsAsync((CartItemEntity?)null);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.AddToCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product is not available.", result.Errors);
    }

    #endregion

    #region GetCartAsync Tests

    [Fact]
    public async Task GetCartAsync_ValidBuyerId_ReturnsCart()
    {
        // Arrange
        var query = new GetCartQuery { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        cart.Items.Add(new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        });

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.GetCartAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Cart);
        Assert.Single(result.ItemsByStore);
        Assert.Equal(2, result.TotalItemCount);
        Assert.Equal(59.98m, result.TotalPrice);
    }

    [Fact]
    public async Task GetCartAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var query = new GetCartQuery { BuyerId = string.Empty };

        // Act
        var result = await _service.GetCartAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetCartAsync_NoCart_ReturnsEmptyResult()
    {
        // Arrange
        var query = new GetCartQuery { BuyerId = TestBuyerId };

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        // Act
        var result = await _service.GetCartAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.Cart);
        Assert.Empty(result.ItemsByStore);
        Assert.Equal(0, result.TotalItemCount);
    }

    #endregion

    #region UpdateQuantityAsync Tests

    [Fact]
    public async Task UpdateQuantityAsync_ValidCommand_UpdatesQuantity()
    {
        // Arrange
        var command = new UpdateCartItemQuantityCommand
        {
            BuyerId = TestBuyerId,
            CartItemId = TestCartItemId,
            Quantity = 5
        };

        var cart = CreateTestCart();
        var cartItem = new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Cart = cart
        };

        _mockCartRepository.Setup(r => r.GetItemByIdAsync(TestCartItemId))
            .ReturnsAsync(cartItem);

        _mockCartRepository.Setup(r => r.UpdateItemAsync(It.IsAny<CartItemEntity>()))
            .Returns(Task.CompletedTask);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateQuantityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockCartRepository.Verify(r => r.UpdateItemAsync(It.Is<CartItemEntity>(i =>
            i.Id == TestCartItemId &&
            i.Quantity == 5)), Times.Once);
    }

    [Fact]
    public async Task UpdateQuantityAsync_ZeroQuantity_RemovesItem()
    {
        // Arrange
        var command = new UpdateCartItemQuantityCommand
        {
            BuyerId = TestBuyerId,
            CartItemId = TestCartItemId,
            Quantity = 0
        };

        var cart = CreateTestCart();
        var cartItem = new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Cart = cart
        };

        _mockCartRepository.Setup(r => r.GetItemByIdAsync(TestCartItemId))
            .ReturnsAsync(cartItem);

        _mockCartRepository.Setup(r => r.RemoveItemAsync(It.IsAny<CartItemEntity>()))
            .Returns(Task.CompletedTask);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateQuantityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockCartRepository.Verify(r => r.RemoveItemAsync(It.Is<CartItemEntity>(i =>
            i.Id == TestCartItemId)), Times.Once);
    }

    [Fact]
    public async Task UpdateQuantityAsync_NotAuthorized_ReturnsNotAuthorized()
    {
        // Arrange
        var command = new UpdateCartItemQuantityCommand
        {
            BuyerId = TestBuyerId,
            CartItemId = TestCartItemId,
            Quantity = 5
        };

        var otherBuyerCart = new CartEntity
        {
            Id = TestCartId,
            BuyerId = "other-buyer",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var cartItem = new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Cart = otherBuyerCart
        };

        _mockCartRepository.Setup(r => r.GetItemByIdAsync(TestCartItemId))
            .ReturnsAsync(cartItem);

        // Act
        var result = await _service.UpdateQuantityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    #endregion

    #region RemoveItemAsync Tests

    [Fact]
    public async Task RemoveItemAsync_ValidCommand_RemovesItem()
    {
        // Arrange
        var command = new RemoveCartItemCommand
        {
            BuyerId = TestBuyerId,
            CartItemId = TestCartItemId
        };

        var cart = CreateTestCart();
        var cartItem = new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Cart = cart
        };

        _mockCartRepository.Setup(r => r.GetItemByIdAsync(TestCartItemId))
            .ReturnsAsync(cartItem);

        _mockCartRepository.Setup(r => r.RemoveItemAsync(It.IsAny<CartItemEntity>()))
            .Returns(Task.CompletedTask);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RemoveItemAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockCartRepository.Verify(r => r.RemoveItemAsync(It.Is<CartItemEntity>(i =>
            i.Id == TestCartItemId)), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_NotAuthorized_ReturnsNotAuthorized()
    {
        // Arrange
        var command = new RemoveCartItemCommand
        {
            BuyerId = TestBuyerId,
            CartItemId = TestCartItemId
        };

        var otherBuyerCart = new CartEntity
        {
            Id = TestCartId,
            BuyerId = "other-buyer",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var cartItem = new CartItemEntity
        {
            Id = TestCartItemId,
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product",
            ProductPrice = 29.99m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Cart = otherBuyerCart
        };

        _mockCartRepository.Setup(r => r.GetItemByIdAsync(TestCartItemId))
            .ReturnsAsync(cartItem);

        // Act
        var result = await _service.RemoveItemAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    #endregion

    #region GetCartItemCountAsync Tests

    [Fact]
    public async Task GetCartItemCountAsync_ValidBuyerId_ReturnsCount()
    {
        // Arrange
        var cart = CreateTestCart();
        cart.Items.Add(new CartItemEntity { Quantity = 2 });
        cart.Items.Add(new CartItemEntity { Quantity = 3 });

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        // Act
        var count = await _service.GetCartItemCountAsync(TestBuyerId);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetCartItemCountAsync_EmptyBuyerId_ReturnsZero()
    {
        // Arrange & Act
        var count = await _service.GetCartItemCountAsync(string.Empty);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetCartItemCountAsync_NoCart_ReturnsZero()
    {
        // Arrange
        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        // Act
        var count = await _service.GetCartItemCountAsync(TestBuyerId);

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    #region Helper Methods

    private static ProductEntity CreateTestProduct()
    {
        return new ProductEntity
        {
            Id = TestProductId,
            StoreId = TestStoreId,
            Title = "Test Product",
            Description = "Test Description",
            Price = 29.99m,
            Stock = 100,
            Category = "Electronics",
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static StoreEntity CreateTestStore()
    {
        return new StoreEntity
        {
            Id = TestStoreId,
            SellerId = "test-seller-id",
            Name = "Test Store",
            Slug = "test-store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static CartEntity CreateTestCart()
    {
        return new CartEntity
        {
            Id = TestCartId,
            BuyerId = TestBuyerId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<CartItemEntity>()
        };
    }

    #endregion
}
