using Mercato.Orders.Application.Commands;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Orders;

public class SellerRatingServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();

    private readonly Mock<ISellerRatingRepository> _mockSellerRatingRepository;
    private readonly Mock<ISellerSubOrderRepository> _mockSellerSubOrderRepository;
    private readonly Mock<ILogger<SellerRatingService>> _mockLogger;
    private readonly SellerRatingService _service;

    public SellerRatingServiceTests()
    {
        _mockSellerRatingRepository = new Mock<ISellerRatingRepository>(MockBehavior.Strict);
        _mockSellerSubOrderRepository = new Mock<ISellerSubOrderRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<SellerRatingService>>();

        _service = new SellerRatingService(
            _mockSellerRatingRepository.Object,
            _mockSellerSubOrderRepository.Object,
            _mockLogger.Object);
    }

    #region SubmitRatingAsync Tests

    [Fact]
    public async Task SubmitRatingAsync_ValidRating_CreatesRating()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerRatingRepository.Setup(r => r.ExistsForSubOrderAsync(TestSubOrderId, TestBuyerId))
            .ReturnsAsync(false);
        _mockSellerRatingRepository.Setup(r => r.GetBuyerLastRatingTimeAsync(TestBuyerId))
            .ReturnsAsync((DateTimeOffset?)null);
        _mockSellerRatingRepository.Setup(r => r.AddAsync(It.IsAny<SellerRating>()))
            .ReturnsAsync((SellerRating r) => r);

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.RatingId);
        _mockSellerRatingRepository.Verify(r => r.AddAsync(It.Is<SellerRating>(sr =>
            sr.BuyerId == TestBuyerId &&
            sr.StoreId == TestStoreId &&
            sr.Rating == 5 &&
            sr.SellerSubOrderId == TestSubOrderId)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task SubmitRatingAsync_InvalidRating_ReturnsFailure(int invalidRating)
    {
        // Arrange
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = invalidRating
        };

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Rating must be between 1 and 5.", result.Errors);
    }

    [Fact]
    public async Task SubmitRatingAsync_SubOrderNotDelivered_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Shipped);
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Ratings can only be submitted for delivered sub-orders.", result.Errors);
    }

    [Fact]
    public async Task SubmitRatingAsync_DuplicateRating_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerRatingRepository.Setup(r => r.ExistsForSubOrderAsync(TestSubOrderId, TestBuyerId))
            .ReturnsAsync(true); // Rating already exists

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("You have already submitted a rating for this seller on this order.", result.Errors);
    }

    [Fact]
    public async Task SubmitRatingAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);
        subOrder.Order.BuyerId = "other-buyer"; // Different buyer
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task SubmitRatingAsync_RateLimited_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        var recentRatingTime = DateTimeOffset.UtcNow.AddSeconds(-30); // 30 seconds ago (within 60 second limit)

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerRatingRepository.Setup(r => r.ExistsForSubOrderAsync(TestSubOrderId, TestBuyerId))
            .ReturnsAsync(false);
        _mockSellerRatingRepository.Setup(r => r.GetBuyerLastRatingTimeAsync(TestBuyerId))
            .ReturnsAsync(recentRatingTime);

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Please wait") && e.Contains("seconds"));
    }

    [Fact]
    public async Task SubmitRatingAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    [Fact]
    public async Task SubmitRatingAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = "",
            Rating = 5
        };

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task SubmitRatingAsync_EmptySubOrderId_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitSellerRatingCommand
        {
            SellerSubOrderId = Guid.Empty,
            BuyerId = TestBuyerId,
            Rating = 5
        };

        // Act
        var result = await _service.SubmitRatingAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order ID is required.", result.Errors);
    }

    #endregion

    #region CanSubmitRatingAsync Tests

    [Fact]
    public async Task CanSubmitRatingAsync_DeliveredSubOrder_ReturnsYes()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerRatingRepository.Setup(r => r.ExistsForSubOrderAsync(TestSubOrderId, TestBuyerId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CanSubmitRatingAsync(TestSubOrderId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.CanSubmit);
        Assert.Null(result.BlockedReason);
    }

    [Fact]
    public async Task CanSubmitRatingAsync_NotDeliveredSubOrder_ReturnsNo()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Shipped);

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CanSubmitRatingAsync(TestSubOrderId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanSubmit);
        Assert.Equal("Ratings can only be submitted for delivered sub-orders.", result.BlockedReason);
    }

    [Fact]
    public async Task CanSubmitRatingAsync_ExistingRating_ReturnsNo()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerRatingRepository.Setup(r => r.ExistsForSubOrderAsync(TestSubOrderId, TestBuyerId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanSubmitRatingAsync(TestSubOrderId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanSubmit);
        Assert.Equal("You have already submitted a rating for this seller on this order.", result.BlockedReason);
    }

    [Fact]
    public async Task CanSubmitRatingAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestSubOrder(SellerSubOrderStatus.Delivered);
        subOrder.Order.BuyerId = "other-buyer";

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CanSubmitRatingAsync(TestSubOrderId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CanSubmitRatingAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.CanSubmitRatingAsync(TestSubOrderId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task CanSubmitRatingAsync_EmptySubOrderId_ReturnsFailure()
    {
        // Act
        var result = await _service.CanSubmitRatingAsync(Guid.Empty, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order ID is required.", result.Errors);
    }

    [Fact]
    public async Task CanSubmitRatingAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.CanSubmitRatingAsync(TestSubOrderId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    #endregion

    #region GetRatingsByStoreIdAsync Tests

    [Fact]
    public async Task GetRatingsByStoreIdAsync_ValidStoreId_ReturnsRatings()
    {
        // Arrange
        var ratings = new List<SellerRating>
        {
            CreateTestRating(),
            CreateTestRating()
        };

        _mockSellerRatingRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(ratings);

        // Act
        var result = await _service.GetRatingsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Ratings.Count);
    }

    [Fact]
    public async Task GetRatingsByStoreIdAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetRatingsByStoreIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetRatingsByStoreIdAsync_NoRatings_ReturnsEmptyList()
    {
        // Arrange
        _mockSellerRatingRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(new List<SellerRating>());

        // Act
        var result = await _service.GetRatingsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Ratings);
    }

    #endregion

    #region GetRatingsByBuyerIdAsync Tests

    [Fact]
    public async Task GetRatingsByBuyerIdAsync_ValidBuyerId_ReturnsRatings()
    {
        // Arrange
        var ratings = new List<SellerRating>
        {
            CreateTestRating(),
            CreateTestRating()
        };

        _mockSellerRatingRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(ratings);

        // Act
        var result = await _service.GetRatingsByBuyerIdAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Ratings.Count);
    }

    [Fact]
    public async Task GetRatingsByBuyerIdAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetRatingsByBuyerIdAsync("");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetRatingsByBuyerIdAsync_NoRatings_ReturnsEmptyList()
    {
        // Arrange
        _mockSellerRatingRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<SellerRating>());

        // Act
        var result = await _service.GetRatingsByBuyerIdAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Ratings);
    }

    #endregion

    #region GetAverageRatingForStoreAsync Tests

    [Fact]
    public async Task GetAverageRatingForStoreAsync_ValidStoreId_ReturnsAverageRating()
    {
        // Arrange
        _mockSellerRatingRepository.Setup(r => r.GetAverageRatingForStoreAsync(TestStoreId))
            .ReturnsAsync(4.5);

        // Act
        var result = await _service.GetAverageRatingForStoreAsync(TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(4.5, result.AverageRating);
    }

    [Fact]
    public async Task GetAverageRatingForStoreAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetAverageRatingForStoreAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetAverageRatingForStoreAsync_NoRatings_ReturnsNull()
    {
        // Arrange
        _mockSellerRatingRepository.Setup(r => r.GetAverageRatingForStoreAsync(TestStoreId))
            .ReturnsAsync((double?)null);

        // Act
        var result = await _service.GetAverageRatingForStoreAsync(TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.AverageRating);
    }

    #endregion

    #region Helper Methods

    private static SellerSubOrder CreateTestSubOrder(SellerSubOrderStatus status)
    {
        var order = new Order
        {
            Id = TestOrderId,
            BuyerId = TestBuyerId,
            OrderNumber = "ORD-12345678",
            Status = OrderStatus.Delivered,
            TotalAmount = 100m,
            ItemsSubtotal = 95m,
            ShippingTotal = 5m,
            DeliveryFullName = "Test Buyer",
            DeliveryAddressLine1 = "123 Test St",
            DeliveryCity = "Test City",
            DeliveryPostalCode = "12345",
            DeliveryCountry = "US",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var subOrder = new SellerSubOrder
        {
            Id = TestSubOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            StoreName = "Test Store",
            SubOrderNumber = "ORD-12345678-S1",
            Status = status,
            ItemsSubtotal = 95m,
            ShippingCost = 5m,
            TotalAmount = 100m,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Order = order
        };

        if (status == SellerSubOrderStatus.Delivered)
        {
            subOrder.DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1);
        }

        order.SellerSubOrders = new List<SellerSubOrder> { subOrder };

        return subOrder;
    }

    private static SellerRating CreateTestRating()
    {
        return new SellerRating
        {
            Id = Guid.NewGuid(),
            OrderId = TestOrderId,
            SellerSubOrderId = TestSubOrderId,
            StoreId = TestStoreId,
            BuyerId = TestBuyerId,
            Rating = 5,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
