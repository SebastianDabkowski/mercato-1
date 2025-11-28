using Mercato.Cart.Application.Queries;
using Mercato.Cart.Infrastructure;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using CartItemEntity = Mercato.Cart.Domain.Entities.CartItem;

namespace Mercato.Tests.Cart;

/// <summary>
/// Unit tests for <see cref="ShippingMethodService"/>.
/// </summary>
public class ShippingMethodServiceTests
{
    private static readonly Guid TestStoreId1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestStoreId2 = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<IShippingRuleRepository> _mockShippingRuleRepository;
    private readonly Mock<ILogger<ShippingMethodService>> _mockLogger;
    private readonly ShippingMethodService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodServiceTests"/> class.
    /// </summary>
    public ShippingMethodServiceTests()
    {
        _mockShippingRuleRepository = new Mock<IShippingRuleRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ShippingMethodService>>();
        _service = new ShippingMethodService(
            _mockShippingRuleRepository.Object,
            _mockLogger.Object);
    }

    #region GetShippingMethodsAsync Tests

    [Fact]
    public async Task GetShippingMethodsAsync_EmptyStoreIds_ReturnsEmptyResult()
    {
        // Arrange
        var storeIds = new List<Guid>();
        var itemsByStore = new List<CartItemsByStore>();

        // Act
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.MethodsByStore);
    }

    [Fact]
    public async Task GetShippingMethodsAsync_SingleStore_ReturnsStandardAndExpressMethods()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1 };
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 29.99m, 2)
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ShippingRule>());

        // Act
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.MethodsByStore);
        Assert.True(result.MethodsByStore.ContainsKey(TestStoreId1));

        var methods = result.MethodsByStore[TestStoreId1];
        Assert.Equal(2, methods.Count);
        Assert.Contains(methods, m => m.Id == "standard");
        Assert.Contains(methods, m => m.Id == "express");
    }

    [Fact]
    public async Task GetShippingMethodsAsync_NoShippingRule_ReturnsDefaultRates()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1 };
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 29.99m, 2)
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ShippingRule>());

        // Act
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        Assert.True(result.Succeeded);
        var methods = result.MethodsByStore[TestStoreId1];

        var standardMethod = methods.First(m => m.Id == "standard");
        var expressMethod = methods.First(m => m.Id == "express");

        Assert.Equal(5.99m, standardMethod.Cost); // Default standard rate
        Assert.Equal(12.99m, expressMethod.Cost); // Default express rate
    }

    [Fact]
    public async Task GetShippingMethodsAsync_WithShippingRule_ReturnsCalculatedRates()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1 };
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
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        Assert.True(result.Succeeded);
        var methods = result.MethodsByStore[TestStoreId1];

        var standardMethod = methods.First(m => m.Id == "standard");
        var expressMethod = methods.First(m => m.Id == "express");

        // Standard: 5.00 + (1.00 * 3) = 8.00
        Assert.Equal(8.00m, standardMethod.Cost);
        // Express: (5.00 + (1.00 * 3)) * 2 = 16.00
        Assert.Equal(16.00m, expressMethod.Cost);
    }

    [Fact]
    public async Task GetShippingMethodsAsync_MeetsFreeShippingThreshold_ReturnsFreeStandardShipping()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1 };
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
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        Assert.True(result.Succeeded);
        var methods = result.MethodsByStore[TestStoreId1];

        var standardMethod = methods.First(m => m.Id == "standard");
        var expressMethod = methods.First(m => m.Id == "express");

        // Standard is free when threshold is met
        Assert.Equal(0m, standardMethod.Cost);
        // Express is still calculated (7.50 * 2 = 15.00)
        Assert.Equal(15.00m, expressMethod.Cost);
    }

    [Fact]
    public async Task GetShippingMethodsAsync_MultipleStores_ReturnsMethodsForEach()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1, TestStoreId2 };
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
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.MethodsByStore.Count);

        // Store 1: Subtotal 60.00 < threshold 100.00, so pays shipping
        var store1Methods = result.MethodsByStore[TestStoreId1];
        Assert.Equal(5.00m, store1Methods.First(m => m.Id == "standard").Cost);

        // Store 2: Subtotal 75.00 >= threshold 50.00, so free standard shipping
        var store2Methods = result.MethodsByStore[TestStoreId2];
        Assert.Equal(0m, store2Methods.First(m => m.Id == "standard").Cost);
    }

    [Fact]
    public async Task GetShippingMethodsAsync_StandardMethodIsDefault()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1 };
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 29.99m, 1)
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ShippingRule>());

        // Act
        var result = await _service.GetShippingMethodsAsync(storeIds, itemsByStore);

        // Assert
        var methods = result.MethodsByStore[TestStoreId1];
        var standardMethod = methods.First(m => m.Id == "standard");
        var expressMethod = methods.First(m => m.Id == "express");

        Assert.True(standardMethod.IsDefault);
        Assert.False(expressMethod.IsDefault);
    }

    #endregion

    #region ValidateShippingMethodSelection Tests

    [Fact]
    public void ValidateShippingMethodSelection_AllStoresHaveValidMethods_ReturnsTrue()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1, TestStoreId2 };
        var selectedMethods = new Dictionary<Guid, string>
        {
            { TestStoreId1, "standard" },
            { TestStoreId2, "express" }
        };

        // Act
        var result = _service.ValidateShippingMethodSelection(selectedMethods, storeIds);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateShippingMethodSelection_MissingStore_ReturnsFalse()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1, TestStoreId2 };
        var selectedMethods = new Dictionary<Guid, string>
        {
            { TestStoreId1, "standard" }
            // Missing TestStoreId2
        };

        // Act
        var result = _service.ValidateShippingMethodSelection(selectedMethods, storeIds);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateShippingMethodSelection_InvalidMethodId_ReturnsFalse()
    {
        // Arrange
        var storeIds = new List<Guid> { TestStoreId1 };
        var selectedMethods = new Dictionary<Guid, string>
        {
            { TestStoreId1, "invalid_method" }
        };

        // Act
        var result = _service.ValidateShippingMethodSelection(selectedMethods, storeIds);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateShippingMethodSelection_EmptyStoreIds_ReturnsTrue()
    {
        // Arrange
        var storeIds = new List<Guid>();
        var selectedMethods = new Dictionary<Guid, string>();

        // Act
        var result = _service.ValidateShippingMethodSelection(selectedMethods, storeIds);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetTotalShippingCostAsync Tests

    [Fact]
    public async Task GetTotalShippingCostAsync_SingleStore_ReturnsCorrectCost()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 10.00m, 3)
        };

        var selectedMethods = new Dictionary<Guid, string>
        {
            { TestStoreId1, "standard" }
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
        var result = await _service.GetTotalShippingCostAsync(selectedMethods, itemsByStore);

        // Assert
        // Standard: 5.00 + (1.00 * 3) = 8.00
        Assert.Equal(8.00m, result);
    }

    [Fact]
    public async Task GetTotalShippingCostAsync_ExpressMethod_ReturnsHigherCost()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 10.00m, 3)
        };

        var selectedMethods = new Dictionary<Guid, string>
        {
            { TestStoreId1, "express" }
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
        var result = await _service.GetTotalShippingCostAsync(selectedMethods, itemsByStore);

        // Assert
        // Express: (5.00 + (1.00 * 3)) * 2 = 16.00
        Assert.Equal(16.00m, result);
    }

    [Fact]
    public async Task GetTotalShippingCostAsync_MultipleStores_ReturnsSum()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 30.00m, 2),
            CreateTestCartItemsByStore(TestStoreId2, "Store 2", 20.00m, 1)
        };

        var selectedMethods = new Dictionary<Guid, string>
        {
            { TestStoreId1, "standard" },
            { TestStoreId2, "express" }
        };

        var shippingRules = new Dictionary<Guid, ShippingRule>
        {
            [TestStoreId1] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId1,
                FlatRate = 5.00m,
                PerItemRate = 0,
                FreeShippingThreshold = null
            },
            [TestStoreId2] = new ShippingRule
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId2,
                FlatRate = 7.00m,
                PerItemRate = 0,
                FreeShippingThreshold = null
            }
        };

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(shippingRules);

        // Act
        var result = await _service.GetTotalShippingCostAsync(selectedMethods, itemsByStore);

        // Assert
        // Store 1 standard: 5.00, Store 2 express: 7.00 * 2 = 14.00
        // Total: 5.00 + 14.00 = 19.00
        Assert.Equal(19.00m, result);
    }

    [Fact]
    public async Task GetTotalShippingCostAsync_MissingMethod_DefaultsToStandard()
    {
        // Arrange
        var itemsByStore = new List<CartItemsByStore>
        {
            CreateTestCartItemsByStore(TestStoreId1, "Store 1", 30.00m, 1)
        };

        var selectedMethods = new Dictionary<Guid, string>();

        _mockShippingRuleRepository.Setup(r => r.GetByStoreIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ShippingRule>());

        // Act
        var result = await _service.GetTotalShippingCostAsync(selectedMethods, itemsByStore);

        // Assert
        // Default standard rate when no rule
        Assert.Equal(5.99m, result);
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
