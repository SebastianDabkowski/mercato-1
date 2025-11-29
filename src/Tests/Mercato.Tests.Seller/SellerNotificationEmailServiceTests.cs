using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Seller;

public class SellerNotificationEmailServiceTests
{
    private readonly Mock<ILogger<SellerNotificationEmailService>> _mockLogger;
    private readonly SellerEmailSettings _emailSettings;
    private readonly SellerNotificationEmailService _service;

    public SellerNotificationEmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<SellerNotificationEmailService>>();
        _emailSettings = new SellerEmailSettings
        {
            SenderEmail = "noreply@mercato.com",
            SenderName = "Mercato Marketplace",
            BaseUrl = "https://mercato.com"
        };
        _service = new SellerNotificationEmailService(
            _mockLogger.Object,
            Options.Create(_emailSettings));
    }

    #region SendNewOrderNotificationAsync Tests

    [Fact]
    public async Task SendNewOrderNotificationAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var subOrder = CreateTestSubOrder();
        var parentOrder = CreateTestOrder();
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendNewOrderNotificationAsync(subOrder, parentOrder, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendNewOrderNotificationAsync_EmptySellerEmail_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSubOrder();
        var parentOrder = CreateTestOrder();
        var sellerEmail = string.Empty;

        // Act
        var result = await _service.SendNewOrderNotificationAsync(subOrder, parentOrder, sellerEmail);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller email is required"));
    }

    [Fact]
    public async Task SendNewOrderNotificationAsync_NullSellerEmail_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSubOrder();
        var parentOrder = CreateTestOrder();
        string? sellerEmail = null;

        // Act
        var result = await _service.SendNewOrderNotificationAsync(subOrder, parentOrder, sellerEmail!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller email is required"));
    }

    [Fact]
    public async Task SendNewOrderNotificationAsync_WithMultipleItems_ReturnsSuccess()
    {
        // Arrange
        var subOrder = CreateTestSubOrderWithMultipleItems();
        var parentOrder = CreateTestOrder();
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendNewOrderNotificationAsync(subOrder, parentOrder, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
    }

    #endregion

    #region SendReturnOrComplaintNotificationAsync Tests

    [Fact]
    public async Task SendReturnOrComplaintNotificationAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var subOrder = CreateTestSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder);
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendReturnOrComplaintNotificationAsync(returnRequest, subOrder, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendReturnOrComplaintNotificationAsync_EmptySellerEmail_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder);
        var sellerEmail = string.Empty;

        // Act
        var result = await _service.SendReturnOrComplaintNotificationAsync(returnRequest, subOrder, sellerEmail);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller email is required"));
    }

    [Fact]
    public async Task SendReturnOrComplaintNotificationAsync_ComplaintType_ReturnsSuccess()
    {
        // Arrange
        var subOrder = CreateTestSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder, CaseType.Complaint);
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendReturnOrComplaintNotificationAsync(returnRequest, subOrder, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SendReturnOrComplaintNotificationAsync_WithCaseItems_ReturnsSuccess()
    {
        // Arrange
        var subOrder = CreateTestSubOrderWithMultipleItems();
        var returnRequest = CreateTestReturnRequestWithItems(subOrder);
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendReturnOrComplaintNotificationAsync(returnRequest, subOrder, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
    }

    #endregion

    #region SendPayoutProcessedNotificationAsync Tests

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var payout = CreateTestPayout();
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_EmptySellerEmail_ReturnsFailure()
    {
        // Arrange
        var payout = CreateTestPayout();
        var sellerEmail = string.Empty;

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller email is required"));
    }

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_NullSellerEmail_ReturnsFailure()
    {
        // Arrange
        var payout = CreateTestPayout();
        string? sellerEmail = null;

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller email is required"));
    }

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_PayoutWithCompletedAt_ReturnsSuccess()
    {
        // Arrange
        var payout = CreateTestPayout();
        payout.CompletedAt = DateTimeOffset.UtcNow;
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_PayoutWithoutCompletedAt_ReturnsSuccess()
    {
        // Arrange
        var payout = CreateTestPayout();
        payout.CompletedAt = null;
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
    }

    #endregion

    #region Helper Methods

    private static SellerSubOrder CreateTestSubOrder()
    {
        var subOrderId = Guid.NewGuid();
        return new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            StoreName = "Test Store",
            SubOrderNumber = "ORD-12345678-S1",
            Status = SellerSubOrderStatus.New,
            ItemsSubtotal = 100.00m,
            ShippingCost = 10.00m,
            TotalAmount = 110.00m,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<SellerSubOrderItem>
            {
                new SellerSubOrderItem
                {
                    Id = Guid.NewGuid(),
                    SellerSubOrderId = subOrderId,
                    ProductId = Guid.NewGuid(),
                    ProductTitle = "Test Product",
                    UnitPrice = 50.00m,
                    Quantity = 2,
                    Status = SellerSubOrderItemStatus.New,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };
    }

    private static SellerSubOrder CreateTestSubOrderWithMultipleItems()
    {
        var subOrderId = Guid.NewGuid();
        return new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            StoreName = "Test Store",
            SubOrderNumber = "ORD-12345678-S1",
            Status = SellerSubOrderStatus.New,
            ItemsSubtotal = 150.00m,
            ShippingCost = 15.00m,
            TotalAmount = 165.00m,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<SellerSubOrderItem>
            {
                new SellerSubOrderItem
                {
                    Id = Guid.NewGuid(),
                    SellerSubOrderId = subOrderId,
                    ProductId = Guid.NewGuid(),
                    ProductTitle = "Test Product 1",
                    UnitPrice = 50.00m,
                    Quantity = 2,
                    Status = SellerSubOrderItemStatus.New,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                },
                new SellerSubOrderItem
                {
                    Id = Guid.NewGuid(),
                    SellerSubOrderId = subOrderId,
                    ProductId = Guid.NewGuid(),
                    ProductTitle = "Test Product 2",
                    UnitPrice = 25.00m,
                    Quantity = 2,
                    Status = SellerSubOrderItemStatus.New,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };
    }

    private static Order CreateTestOrder()
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = "test-buyer-id",
            OrderNumber = "ORD-12345678",
            Status = OrderStatus.New,
            ItemsSubtotal = 100.00m,
            ShippingTotal = 10.00m,
            TotalAmount = 110.00m,
            DeliveryFullName = "John Doe",
            DeliveryAddressLine1 = "123 Main St",
            DeliveryCity = "Test City",
            DeliveryPostalCode = "12345",
            DeliveryCountry = "USA",
            BuyerEmail = "buyer@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static ReturnRequest CreateTestReturnRequest(SellerSubOrder subOrder, CaseType caseType = CaseType.Return)
    {
        return new ReturnRequest
        {
            Id = Guid.NewGuid(),
            CaseNumber = "CASE-12345678",
            CaseType = caseType,
            SellerSubOrderId = subOrder.Id,
            BuyerId = "test-buyer-id",
            Status = ReturnStatus.Requested,
            Reason = "Product was defective",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            CaseItems = new List<CaseItem>()
        };
    }

    private static ReturnRequest CreateTestReturnRequestWithItems(SellerSubOrder subOrder)
    {
        var returnRequest = new ReturnRequest
        {
            Id = Guid.NewGuid(),
            CaseNumber = "CASE-12345678",
            CaseType = CaseType.Return,
            SellerSubOrderId = subOrder.Id,
            BuyerId = "test-buyer-id",
            Status = ReturnStatus.Requested,
            Reason = "Product was defective",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            CaseItems = subOrder.Items.Select(item => new CaseItem
            {
                Id = Guid.NewGuid(),
                ReturnRequestId = Guid.NewGuid(),
                SellerSubOrderItemId = item.Id,
                Quantity = item.Quantity,
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList()
        };
        return returnRequest;
    }

    private static Payout CreateTestPayout()
    {
        return new Payout
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = 500.00m,
            Currency = "USD",
            Status = PayoutStatus.Paid,
            ScheduleFrequency = PayoutScheduleFrequency.Weekly,
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1),
            ProcessingStartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}

public class PayoutNotificationServiceTests
{
    private readonly Mock<ILogger<PayoutNotificationService>> _mockLogger;
    private readonly SellerEmailSettings _emailSettings;
    private readonly PayoutNotificationService _service;

    public PayoutNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<PayoutNotificationService>>();
        _emailSettings = new SellerEmailSettings
        {
            SenderEmail = "noreply@mercato.com",
            SenderName = "Mercato Marketplace",
            BaseUrl = "https://mercato.com"
        };
        _service = new PayoutNotificationService(
            _mockLogger.Object,
            Options.Create(_emailSettings));
    }

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = 500.00m,
            Currency = "USD",
            Status = PayoutStatus.Paid,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        var sellerEmail = "seller@example.com";

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendPayoutProcessedNotificationAsync_EmptyEmail_ReturnsFailure()
    {
        // Arrange
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = 500.00m,
            Currency = "USD"
        };
        var sellerEmail = string.Empty;

        // Act
        var result = await _service.SendPayoutProcessedNotificationAsync(payout, sellerEmail);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller email is required"));
    }
}

public class StoreEmailProviderTests
{
    private readonly Mock<Mercato.Seller.Domain.Interfaces.IStoreRepository> _mockStoreRepository;
    private readonly StoreEmailProvider _service;

    public StoreEmailProviderTests()
    {
        _mockStoreRepository = new Mock<Mercato.Seller.Domain.Interfaces.IStoreRepository>(MockBehavior.Strict);
        _service = new StoreEmailProvider(_mockStoreRepository.Object);
    }

    [Fact]
    public async Task GetStoreEmailAsync_ValidStoreId_ReturnsEmail()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var store = new Mercato.Seller.Domain.Entities.Store
        {
            Id = storeId,
            SellerId = "seller-123",
            Name = "Test Store",
            Slug = "test-store",
            ContactEmail = "seller@example.com"
        };

        _mockStoreRepository.Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        // Act
        var result = await _service.GetStoreEmailAsync(storeId);

        // Assert
        Assert.Equal("seller@example.com", result);
    }

    [Fact]
    public async Task GetStoreEmailAsync_EmptyStoreId_ReturnsNull()
    {
        // Arrange
        var storeId = Guid.Empty;

        // Act
        var result = await _service.GetStoreEmailAsync(storeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoreEmailAsync_StoreNotFound_ReturnsNull()
    {
        // Arrange
        var storeId = Guid.NewGuid();

        _mockStoreRepository.Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync((Mercato.Seller.Domain.Entities.Store?)null);

        // Act
        var result = await _service.GetStoreEmailAsync(storeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoreEmailAsync_StoreWithNoEmail_ReturnsNull()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var store = new Mercato.Seller.Domain.Entities.Store
        {
            Id = storeId,
            SellerId = "seller-123",
            Name = "Test Store",
            Slug = "test-store",
            ContactEmail = null
        };

        _mockStoreRepository.Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        // Act
        var result = await _service.GetStoreEmailAsync(storeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoreEmailsAsync_ValidStoreIds_ReturnsEmails()
    {
        // Arrange
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();
        var stores = new List<Mercato.Seller.Domain.Entities.Store>
        {
            new()
            {
                Id = storeId1,
                SellerId = "seller-1",
                Name = "Store 1",
                Slug = "store-1",
                ContactEmail = "seller1@example.com"
            },
            new()
            {
                Id = storeId2,
                SellerId = "seller-2",
                Name = "Store 2",
                Slug = "store-2",
                ContactEmail = "seller2@example.com"
            }
        };

        _mockStoreRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetStoreEmailsAsync(new[] { storeId1, storeId2 });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("seller1@example.com", result[storeId1]);
        Assert.Equal("seller2@example.com", result[storeId2]);
    }

    [Fact]
    public async Task GetStoreEmailsAsync_EmptyStoreIds_ReturnsEmptyDictionary()
    {
        // Arrange
        var storeIds = Array.Empty<Guid>();

        // Act
        var result = await _service.GetStoreEmailsAsync(storeIds);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStoreEmailsAsync_StoresWithNoEmail_ReturnsOnlyStoresWithEmail()
    {
        // Arrange
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();
        var stores = new List<Mercato.Seller.Domain.Entities.Store>
        {
            new()
            {
                Id = storeId1,
                SellerId = "seller-1",
                Name = "Store 1",
                Slug = "store-1",
                ContactEmail = "seller1@example.com"
            },
            new()
            {
                Id = storeId2,
                SellerId = "seller-2",
                Name = "Store 2",
                Slug = "store-2",
                ContactEmail = null // No email
            }
        };

        _mockStoreRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetStoreEmailsAsync(new[] { storeId1, storeId2 });

        // Assert
        Assert.Single(result);
        Assert.Equal("seller1@example.com", result[storeId1]);
        Assert.False(result.ContainsKey(storeId2));
    }
}

public class SellerEmailProviderTests
{
    private readonly Mock<Mercato.Seller.Domain.Interfaces.IStoreRepository> _mockStoreRepository;
    private readonly SellerEmailProvider _service;

    public SellerEmailProviderTests()
    {
        _mockStoreRepository = new Mock<Mercato.Seller.Domain.Interfaces.IStoreRepository>(MockBehavior.Strict);
        _service = new SellerEmailProvider(_mockStoreRepository.Object);
    }

    [Fact]
    public async Task GetSellerEmailAsync_ValidSellerId_ReturnsEmail()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var store = new Mercato.Seller.Domain.Entities.Store
        {
            Id = sellerId,
            SellerId = "seller-123",
            Name = "Test Store",
            Slug = "test-store",
            ContactEmail = "seller@example.com"
        };

        _mockStoreRepository.Setup(r => r.GetByIdAsync(sellerId))
            .ReturnsAsync(store);

        // Act
        var result = await _service.GetSellerEmailAsync(sellerId);

        // Assert
        Assert.Equal("seller@example.com", result);
    }

    [Fact]
    public async Task GetSellerEmailAsync_EmptySellerId_ReturnsNull()
    {
        // Arrange
        var sellerId = Guid.Empty;

        // Act
        var result = await _service.GetSellerEmailAsync(sellerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSellerEmailsAsync_ValidSellerIds_ReturnsEmails()
    {
        // Arrange
        var sellerId1 = Guid.NewGuid();
        var sellerId2 = Guid.NewGuid();
        var stores = new List<Mercato.Seller.Domain.Entities.Store>
        {
            new()
            {
                Id = sellerId1,
                SellerId = "seller-1",
                Name = "Store 1",
                Slug = "store-1",
                ContactEmail = "seller1@example.com"
            },
            new()
            {
                Id = sellerId2,
                SellerId = "seller-2",
                Name = "Store 2",
                Slug = "store-2",
                ContactEmail = "seller2@example.com"
            }
        };

        _mockStoreRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetSellerEmailsAsync(new[] { sellerId1, sellerId2 });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("seller1@example.com", result[sellerId1]);
        Assert.Equal("seller2@example.com", result[sellerId2]);
    }

    [Fact]
    public async Task GetSellerEmailsAsync_EmptySellerIds_ReturnsEmptyDictionary()
    {
        // Arrange
        var sellerIds = Array.Empty<Guid>();

        // Act
        var result = await _service.GetSellerEmailsAsync(sellerIds);

        // Assert
        Assert.Empty(result);
    }
}
