using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using CartItemEntity = Mercato.Cart.Domain.Entities.CartItem;

namespace Mercato.Tests.Cart;

public class CommissionCalculatorTests
{
    private static readonly Guid TestStoreId1 = Guid.NewGuid();
    private static readonly Guid TestStoreId2 = Guid.NewGuid();

    private readonly Mock<ILogger<CommissionCalculator>> _mockLogger;
    private readonly CommissionCalculator _calculator;

    public CommissionCalculatorTests()
    {
        _mockLogger = new Mock<ILogger<CommissionCalculator>>();
        _calculator = new CommissionCalculator(_mockLogger.Object);
    }

    #region CalculateCommissionsAsync Tests

    [Fact]
    public async Task CalculateCommissionsAsync_SingleStore_CalculatesCorrectCommission()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 100.00m, 1)
        };

        var shippingByStore = new Dictionary<Guid, StoreShippingCost>
        {
            [TestStoreId1] = new StoreShippingCost
            {
                StoreId = TestStoreId1,
                StoreName = "Store 1",
                ShippingCost = 10.00m,
                IsFreeShipping = false
            }
        };

        var cartTotals = CartTotals.Create(100.00m, shippingByStore, 1);

        // Act
        var result = await _calculator.CalculateCommissionsAsync(cartTotals, itemsByStore);

        // Assert
        Assert.Single(result);
        var commission = result[TestStoreId1];
        Assert.Equal(TestStoreId1, commission.StoreId);
        Assert.Equal(110.00m, commission.GrossAmount); // 100 items + 10 shipping
        Assert.Equal(0.10m, commission.CommissionRate);
        Assert.Equal(11.00m, commission.CommissionAmount); // 10% of 110
        Assert.Equal(99.00m, commission.NetPayout); // 110 - 11
    }

    [Fact]
    public async Task CalculateCommissionsAsync_MultipleStores_CalculatesEachSeparately()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 50.00m, 2), // Subtotal = 100
            CreateTestCartItemsByStore(TestStoreId2, "Store 2", 75.00m, 1)  // Subtotal = 75
        };

        var shippingByStore = new Dictionary<Guid, StoreShippingCost>
        {
            [TestStoreId1] = new StoreShippingCost
            {
                StoreId = TestStoreId1,
                StoreName = "Store 1",
                ShippingCost = 5.00m,
                IsFreeShipping = false
            },
            [TestStoreId2] = new StoreShippingCost
            {
                StoreId = TestStoreId2,
                StoreName = "Store 2",
                ShippingCost = 0m,
                IsFreeShipping = true
            }
        };

        var cartTotals = CartTotals.Create(175.00m, shippingByStore, 3);

        // Act
        var result = await _calculator.CalculateCommissionsAsync(cartTotals, itemsByStore);

        // Assert
        Assert.Equal(2, result.Count);

        // Store 1: Gross = 100 + 5 = 105, Commission = 10.50, Net = 94.50
        var commission1 = result[TestStoreId1];
        Assert.Equal(105.00m, commission1.GrossAmount);
        Assert.Equal(10.50m, commission1.CommissionAmount);
        Assert.Equal(94.50m, commission1.NetPayout);

        // Store 2: Gross = 75 + 0 = 75, Commission = 7.50, Net = 67.50
        var commission2 = result[TestStoreId2];
        Assert.Equal(75.00m, commission2.GrossAmount);
        Assert.Equal(7.50m, commission2.CommissionAmount);
        Assert.Equal(67.50m, commission2.NetPayout);
    }

    [Fact]
    public async Task CalculateCommissionsAsync_NoShipping_CalculatesOnItemsOnly()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 200.00m, 1)
        };

        var shippingByStore = new Dictionary<Guid, StoreShippingCost>();
        var cartTotals = CartTotals.Create(200.00m, shippingByStore, 1);

        // Act
        var result = await _calculator.CalculateCommissionsAsync(cartTotals, itemsByStore);

        // Assert
        Assert.Single(result);
        var commission = result[TestStoreId1];
        Assert.Equal(200.00m, commission.GrossAmount);
        Assert.Equal(20.00m, commission.CommissionAmount);
        Assert.Equal(180.00m, commission.NetPayout);
    }

    [Fact]
    public async Task CalculateCommissionsAsync_RoundsToTwoDecimalPlaces()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 33.33m, 1)
        };

        var shippingByStore = new Dictionary<Guid, StoreShippingCost>
        {
            [TestStoreId1] = new StoreShippingCost
            {
                StoreId = TestStoreId1,
                StoreName = "Store 1",
                ShippingCost = 5.55m,
                IsFreeShipping = false
            }
        };

        var cartTotals = CartTotals.Create(33.33m, shippingByStore, 1);

        // Act
        var result = await _calculator.CalculateCommissionsAsync(cartTotals, itemsByStore);

        // Assert
        Assert.Single(result);
        var commission = result[TestStoreId1];
        // Gross = 33.33 + 5.55 = 38.88
        // Commission = 38.88 * 0.10 = 3.888 -> rounded to 3.89
        Assert.Equal(38.88m, commission.GrossAmount);
        Assert.Equal(3.89m, commission.CommissionAmount);
        Assert.Equal(34.99m, commission.NetPayout);
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
