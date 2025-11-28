using Mercato.Cart.Application.Queries;
using Mercato.Cart.Infrastructure;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using CartItemEntity = Mercato.Cart.Domain.Entities.CartItem;

namespace Mercato.Tests.Cart;

public class ShippingCalculatorTests
{
    private static readonly Guid TestStoreId1 = Guid.NewGuid();
    private static readonly Guid TestStoreId2 = Guid.NewGuid();

    private readonly Mock<IShippingRuleRepository> _mockShippingRuleRepository;
    private readonly Mock<ILogger<ShippingCalculator>> _mockLogger;
    private readonly ShippingCalculator _calculator;

    public ShippingCalculatorTests()
    {
        _mockShippingRuleRepository = new Mock<IShippingRuleRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ShippingCalculator>>();
        _calculator = new ShippingCalculator(
            _mockShippingRuleRepository.Object,
            _mockLogger.Object);
    }

    #region CalculateShippingAsync Tests

    [Fact]
    public async Task CalculateShippingAsync_EmptyCart_ReturnsEmptyDictionary()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>();

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CalculateShippingAsync_NoShippingRule_ReturnsDefaultRate()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 29.99m, 2)
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ShippingRule>());

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Single(result);
        Assert.Equal(5.99m, result[TestStoreId1].ShippingCost);
        Assert.False(result[TestStoreId1].IsFreeShipping);
    }

    [Fact]
    public async Task CalculateShippingAsync_WithFlatRate_ReturnsFlatRate()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 29.99m, 2)
        };

        var shippingRules = new Dictionary<Guid, ShippingRule>
        {
            [TestStoreId1] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId1,
                FlatRate = 7.50m,
                PerItemRate = 0,
                FreeShippingThreshold = null
            }
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(shippingRules);

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Single(result);
        Assert.Equal(7.50m, result[TestStoreId1].ShippingCost);
        Assert.False(result[TestStoreId1].IsFreeShipping);
    }

    [Fact]
    public async Task CalculateShippingAsync_WithPerItemRate_ReturnsCorrectTotal()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 10.00m, 3)
        };

        var shippingRules = new Dictionary<Guid, ShippingRule>
        {
            [TestStoreId1] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId1,
                FlatRate = 5.00m,
                PerItemRate = 1.00m,
                FreeShippingThreshold = null
            }
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(shippingRules);

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Single(result);
        // Flat rate (5.00) + Per item (1.00 * 3 items) = 8.00
        Assert.Equal(8.00m, result[TestStoreId1].ShippingCost);
    }

    [Fact]
    public async Task CalculateShippingAsync_MeetsFreeShippingThreshold_ReturnsFreeShipping()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 60.00m, 1) // Subtotal = 60.00
        };

        var shippingRules = new Dictionary<Guid, ShippingRule>
        {
            [TestStoreId1] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId1,
                FlatRate = 7.50m,
                PerItemRate = 0,
                FreeShippingThreshold = 50.00m
            }
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(shippingRules);

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Single(result);
        Assert.Equal(0m, result[TestStoreId1].ShippingCost);
        Assert.True(result[TestStoreId1].IsFreeShipping);
        Assert.Null(result[TestStoreId1].AmountToFreeShipping);
    }

    [Fact]
    public async Task CalculateShippingAsync_BelowFreeShippingThreshold_ReturnsAmountNeeded()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 30.00m, 1) // Subtotal = 30.00
        };

        var shippingRules = new Dictionary<Guid, ShippingRule>
        {
            [TestStoreId1] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId1,
                FlatRate = 7.50m,
                PerItemRate = 0,
                FreeShippingThreshold = 50.00m
            }
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(shippingRules);

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Single(result);
        Assert.Equal(7.50m, result[TestStoreId1].ShippingCost);
        Assert.False(result[TestStoreId1].IsFreeShipping);
        Assert.Equal(20.00m, result[TestStoreId1].AmountToFreeShipping);
    }

    [Fact]
    public async Task CalculateShippingAsync_MultipleStores_CalculatesEachSeparately()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 30.00m, 2),
            CreateTestCartItemsByStore(TestStoreId2, "Store 2", 75.00m, 1)
        };

        var shippingRules = new Dictionary<Guid, ShippingRule>
        {
            [TestStoreId1] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId1,
                FlatRate = 5.00m,
                PerItemRate = 0,
                FreeShippingThreshold = 100.00m
            },
            [TestStoreId2] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId2,
                FlatRate = 10.00m,
                PerItemRate = 0,
                FreeShippingThreshold = 50.00m
            }
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(shippingRules);

        // Act
        var result = await _calculator.CalculateShippingAsync(itemsByStore);

        // Assert
        Assert.Equal(2, result.Count);

        // Store 1: Subtotal 60.00 < threshold 100.00, so pays flat rate 5.00
        Assert.Equal(5.00m, result[TestStoreId1].ShippingCost);
        Assert.False(result[TestStoreId1].IsFreeShipping);
        Assert.Equal(40.00m, result[TestStoreId1].AmountToFreeShipping);

        // Store 2: Subtotal 75.00 >= threshold 50.00, so free shipping
        Assert.Equal(0m, result[TestStoreId2].ShippingCost);
        Assert.True(result[TestStoreId2].IsFreeShipping);
    }

    #endregion

    #region Helper Methods

    private static CartItemsByStore CreateTestCartItemsByStore(Guid storeId, string storeName, decimal price, int quantity)
    {
        return new CartItemsByStore
        {
            StoreId = storeId,
            StoreName = storeName,
            Items = new List<CartItemEntity>
            {
                new CartItemEntity
                {
                    Id = Guid.NewGuid(),
                    CartId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    StoreId = storeId,
                    Quantity = quantity,
                    ProductTitle = "Test Product",
                    ProductPrice = price,
                    StoreName = storeName,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };
    }

    #endregion
}
