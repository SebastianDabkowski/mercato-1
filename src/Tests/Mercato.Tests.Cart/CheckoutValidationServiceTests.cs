using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Cart.Infrastructure;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using CartEntity = Mercato.Cart.Domain.Entities.Cart;
using CartItemEntity = Mercato.Cart.Domain.Entities.CartItem;
using ProductEntity = Mercato.Product.Domain.Entities.Product;

namespace Mercato.Tests.Cart;

public class CheckoutValidationServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestProductId2 = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestCartId = Guid.NewGuid();
    private static readonly Guid TestCartItemId = Guid.NewGuid();
    private static readonly Guid TestCartItemId2 = Guid.NewGuid();

    private readonly Mock<ICartRepository> _mockCartRepository;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<CheckoutValidationService>> _mockLogger;
    private readonly CheckoutValidationService _service;

    public CheckoutValidationServiceTests()
    {
        _mockCartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        _mockProductService = new Mock<IProductService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CheckoutValidationService>>();
        _service = new CheckoutValidationService(
            _mockCartRepository.Object,
            _mockProductService.Object,
            _mockLogger.Object);
    }

    #region ValidateCheckoutAsync Tests

    [Fact]
    public async Task ValidateCheckoutAsync_ValidCartWithAvailableProducts_ReturnsSuccess()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.StockIssues);
        Assert.Empty(result.PriceChanges);
        Assert.Single(result.ValidatedItems);
        Assert.Equal(product.Price, result.ValidatedItems[0].UnitPrice);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = string.Empty };

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_EmptyCart_ReturnsFailure()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cart is empty.", result.Errors);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_InsufficientStock_ReturnsStockIssue()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cartItem.Quantity = 10;
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();
        product.Stock = 5;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasStockIssues);
        Assert.Single(result.StockIssues);
        Assert.Equal(10, result.StockIssues[0].RequestedQuantity);
        Assert.Equal(5, result.StockIssues[0].AvailableStock);
        Assert.False(result.StockIssues[0].IsOutOfStock);
        Assert.False(result.StockIssues[0].IsUnavailable);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_OutOfStock_ReturnsStockIssueWithZeroStock()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();
        product.Stock = 0;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasStockIssues);
        Assert.Single(result.StockIssues);
        Assert.True(result.StockIssues[0].IsOutOfStock);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_ProductUnavailable_ReturnsUnavailableStockIssue()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync((ProductEntity?)null);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasStockIssues);
        Assert.Single(result.StockIssues);
        Assert.True(result.StockIssues[0].IsUnavailable);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_ProductNotActive_ReturnsUnavailableStockIssue()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasStockIssues);
        Assert.Single(result.StockIssues);
        Assert.True(result.StockIssues[0].IsUnavailable);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_PriceIncreased_ReturnsPriceChangeIssue()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cartItem.ProductPrice = 25.00m;
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();
        product.Price = 35.00m;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasPriceChanges);
        Assert.Single(result.PriceChanges);
        Assert.Equal(25.00m, result.PriceChanges[0].OriginalPrice);
        Assert.Equal(35.00m, result.PriceChanges[0].CurrentPrice);
        Assert.True(result.PriceChanges[0].PriceIncreased);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_PriceDecreased_ReturnsPriceChangeIssue()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cartItem.ProductPrice = 35.00m;
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();
        product.Price = 25.00m;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasPriceChanges);
        Assert.Single(result.PriceChanges);
        Assert.Equal(35.00m, result.PriceChanges[0].OriginalPrice);
        Assert.Equal(25.00m, result.PriceChanges[0].CurrentPrice);
        Assert.False(result.PriceChanges[0].PriceIncreased);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_BothStockAndPriceIssues_ReturnsBothIssues()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();

        var cartItem1 = CreateTestCartItem();
        cartItem1.Quantity = 10;
        cart.Items.Add(cartItem1);

        var cartItem2 = new CartItemEntity
        {
            Id = TestCartItemId2,
            CartId = TestCartId,
            ProductId = TestProductId2,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product 2",
            ProductPrice = 50.00m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        cart.Items.Add(cartItem2);

        var product1 = CreateTestProduct();
        product1.Stock = 5;

        var product2 = new ProductEntity
        {
            Id = TestProductId2,
            StoreId = TestStoreId,
            Title = "Test Product 2",
            Price = 60.00m,
            Stock = 100,
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product1);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId2))
            .ReturnsAsync(product2);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasStockIssues);
        Assert.True(result.HasPriceChanges);
        Assert.Single(result.StockIssues);
        Assert.Single(result.PriceChanges);
    }

    [Fact]
    public async Task ValidateCheckoutAsync_MultipleItems_ValidatesAllItems()
    {
        // Arrange
        var command = new ValidateCheckoutCommand { BuyerId = TestBuyerId };
        var cart = CreateTestCart();

        var cartItem1 = CreateTestCartItem();
        cart.Items.Add(cartItem1);

        var cartItem2 = new CartItemEntity
        {
            Id = TestCartItemId2,
            CartId = TestCartId,
            ProductId = TestProductId2,
            StoreId = TestStoreId,
            Quantity = 2,
            ProductTitle = "Test Product 2",
            ProductPrice = 50.00m,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        cart.Items.Add(cartItem2);

        var product1 = CreateTestProduct();
        var product2 = new ProductEntity
        {
            Id = TestProductId2,
            StoreId = TestStoreId,
            Title = "Test Product 2",
            Price = 50.00m,
            Stock = 100,
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product1);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId2))
            .ReturnsAsync(product2);

        // Act
        var result = await _service.ValidateCheckoutAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.StockIssues);
        Assert.Empty(result.PriceChanges);
        Assert.Equal(2, result.ValidatedItems.Count);
    }

    #endregion

    #region UpdateCartPricesToCurrentAsync Tests

    [Fact]
    public async Task UpdateCartPricesToCurrentAsync_PriceChanged_UpdatesCartItem()
    {
        // Arrange
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cartItem.ProductPrice = 25.00m;
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();
        product.Price = 35.00m;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        _mockCartRepository.Setup(r => r.UpdateItemAsync(It.IsAny<CartItemEntity>()))
            .Returns(Task.CompletedTask);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateCartPricesToCurrentAsync(TestBuyerId);

        // Assert
        _mockCartRepository.Verify(r => r.UpdateItemAsync(It.Is<CartItemEntity>(i =>
            i.ProductPrice == 35.00m)), Times.Once);
    }

    [Fact]
    public async Task UpdateCartPricesToCurrentAsync_NoPriceChange_DoesNotUpdate()
    {
        // Arrange
        var cart = CreateTestCart();
        var cartItem = CreateTestCartItem();
        cart.Items.Add(cartItem);

        var product = CreateTestProduct();

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockProductService.Setup(s => s.GetProductByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        await _service.UpdateCartPricesToCurrentAsync(TestBuyerId);

        // Assert
        _mockCartRepository.Verify(r => r.UpdateItemAsync(It.IsAny<CartItemEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCartPricesToCurrentAsync_EmptyBuyerId_DoesNothing()
    {
        // Arrange & Act
        await _service.UpdateCartPricesToCurrentAsync(string.Empty);

        // Assert
        _mockCartRepository.Verify(r => r.GetByBuyerIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCartPricesToCurrentAsync_NoCart_DoesNothing()
    {
        // Arrange
        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((CartEntity?)null);

        // Act
        await _service.UpdateCartPricesToCurrentAsync(TestBuyerId);

        // Assert
        _mockCartRepository.Verify(r => r.UpdateItemAsync(It.IsAny<CartItemEntity>()), Times.Never);
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

    private static CartItemEntity CreateTestCartItem()
    {
        return new CartItemEntity
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
    }

    #endregion
}
