using Mercato.Orders.Application.Commands;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Orders;

public class ProductReviewServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();
    private static readonly Guid TestItemId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();

    private readonly Mock<IProductReviewRepository> _mockProductReviewRepository;
    private readonly Mock<ISellerSubOrderRepository> _mockSellerSubOrderRepository;
    private readonly Mock<ILogger<ProductReviewService>> _mockLogger;
    private readonly ProductReviewService _service;

    public ProductReviewServiceTests()
    {
        _mockProductReviewRepository = new Mock<IProductReviewRepository>(MockBehavior.Strict);
        _mockSellerSubOrderRepository = new Mock<ISellerSubOrderRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ProductReviewService>>();

        _service = new ProductReviewService(
            _mockProductReviewRepository.Object,
            _mockSellerSubOrderRepository.Object,
            _mockLogger.Object);
    }

    #region SubmitReviewAsync Tests

    [Fact]
    public async Task SubmitReviewAsync_ValidReview_CreatesReview()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);
        _mockProductReviewRepository.Setup(r => r.ExistsForItemAsync(TestItemId, TestBuyerId))
            .ReturnsAsync(false);
        _mockProductReviewRepository.Setup(r => r.GetBuyerLastReviewTimeAsync(TestBuyerId))
            .ReturnsAsync((DateTimeOffset?)null);
        _mockProductReviewRepository.Setup(r => r.AddAsync(It.IsAny<ProductReview>()))
            .ReturnsAsync((ProductReview r) => r);

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReviewId);
        _mockProductReviewRepository.Verify(r => r.AddAsync(It.Is<ProductReview>(pr =>
            pr.BuyerId == TestBuyerId &&
            pr.ProductId == TestProductId &&
            pr.Rating == 5 &&
            pr.ReviewText == "Great product!" &&
            pr.Status == ReviewStatus.Published)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task SubmitReviewAsync_InvalidRating_ReturnsFailure(int invalidRating)
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = invalidRating,
            ReviewText = "Test review"
        };

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Rating must be between 1 and 5.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_EmptyReviewText_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 4,
            ReviewText = ""
        };

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review text is required.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhitespaceReviewText_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 4,
            ReviewText = "   "
        };

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review text is required.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_TooLongReviewText_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 4,
            ReviewText = new string('a', 2001)
        };

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review text must not exceed 2000 characters.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_ItemNotDelivered_ReturnsFailure()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Shipped);
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Reviews can only be submitted for delivered items.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_DuplicateReview_ReturnsFailure()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);
        _mockProductReviewRepository.Setup(r => r.ExistsForItemAsync(TestItemId, TestBuyerId))
            .ReturnsAsync(true); // Review already exists

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("You have already submitted a review for this item.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);
        item.SellerSubOrder.Order.BuyerId = "other-buyer"; // Different buyer
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task SubmitReviewAsync_RateLimited_ReturnsFailure()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        var recentReviewTime = DateTimeOffset.UtcNow.AddSeconds(-30); // 30 seconds ago (within 60 second limit)

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);
        _mockProductReviewRepository.Setup(r => r.ExistsForItemAsync(TestItemId, TestBuyerId))
            .ReturnsAsync(false);
        _mockProductReviewRepository.Setup(r => r.GetBuyerLastReviewTimeAsync(TestBuyerId))
            .ReturnsAsync(recentReviewTime);

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Please wait") && e.Contains("seconds"));
    }

    [Fact]
    public async Task SubmitReviewAsync_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync((SellerSubOrderItem?)null);

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Item not found.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = TestItemId,
            BuyerId = "",
            Rating = 5,
            ReviewText = "Great product!"
        };

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task SubmitReviewAsync_EmptyItemId_ReturnsFailure()
    {
        // Arrange
        var command = new SubmitProductReviewCommand
        {
            SellerSubOrderItemId = Guid.Empty,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!"
        };

        // Act
        var result = await _service.SubmitReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Item ID is required.", result.Errors);
    }

    #endregion

    #region CanSubmitReviewAsync Tests

    [Fact]
    public async Task CanSubmitReviewAsync_DeliveredItem_ReturnsYes()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);
        _mockProductReviewRepository.Setup(r => r.ExistsForItemAsync(TestItemId, TestBuyerId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CanSubmitReviewAsync(TestItemId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.CanSubmit);
        Assert.Null(result.BlockedReason);
    }

    [Fact]
    public async Task CanSubmitReviewAsync_NotDeliveredItem_ReturnsNo()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Shipped);

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);

        // Act
        var result = await _service.CanSubmitReviewAsync(TestItemId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanSubmit);
        Assert.Equal("Reviews can only be submitted for delivered items.", result.BlockedReason);
    }

    [Fact]
    public async Task CanSubmitReviewAsync_ExistingReview_ReturnsNo()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);
        _mockProductReviewRepository.Setup(r => r.ExistsForItemAsync(TestItemId, TestBuyerId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanSubmitReviewAsync(TestItemId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanSubmit);
        Assert.Equal("You have already submitted a review for this item.", result.BlockedReason);
    }

    [Fact]
    public async Task CanSubmitReviewAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var item = CreateTestItem(SellerSubOrderItemStatus.Delivered);
        item.SellerSubOrder.Order.BuyerId = "other-buyer";

        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync(item);

        // Act
        var result = await _service.CanSubmitReviewAsync(TestItemId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CanSubmitReviewAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.CanSubmitReviewAsync(TestItemId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task CanSubmitReviewAsync_EmptyItemId_ReturnsFailure()
    {
        // Act
        var result = await _service.CanSubmitReviewAsync(Guid.Empty, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Item ID is required.", result.Errors);
    }

    [Fact]
    public async Task CanSubmitReviewAsync_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        _mockSellerSubOrderRepository.Setup(r => r.GetItemByIdAsync(TestItemId))
            .ReturnsAsync((SellerSubOrderItem?)null);

        // Act
        var result = await _service.CanSubmitReviewAsync(TestItemId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Item not found.", result.Errors);
    }

    #endregion

    #region GetReviewsByProductIdAsync Tests

    [Fact]
    public async Task GetReviewsByProductIdAsync_ValidProductId_ReturnsReviews()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReview(),
            CreateTestReview()
        };

        _mockProductReviewRepository.Setup(r => r.GetByProductIdAsync(TestProductId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetReviewsByProductIdAsync(TestProductId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Reviews.Count);
    }

    [Fact]
    public async Task GetReviewsByProductIdAsync_EmptyProductId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReviewsByProductIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetReviewsByProductIdAsync_NoReviews_ReturnsEmptyList()
    {
        // Arrange
        _mockProductReviewRepository.Setup(r => r.GetByProductIdAsync(TestProductId))
            .ReturnsAsync(new List<ProductReview>());

        // Act
        var result = await _service.GetReviewsByProductIdAsync(TestProductId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Reviews);
    }

    #endregion

    #region GetReviewsByBuyerIdAsync Tests

    [Fact]
    public async Task GetReviewsByBuyerIdAsync_ValidBuyerId_ReturnsReviews()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReview(),
            CreateTestReview()
        };

        _mockProductReviewRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetReviewsByBuyerIdAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Reviews.Count);
    }

    [Fact]
    public async Task GetReviewsByBuyerIdAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReviewsByBuyerIdAsync("");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetReviewsByBuyerIdAsync_NoReviews_ReturnsEmptyList()
    {
        // Arrange
        _mockProductReviewRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<ProductReview>());

        // Act
        var result = await _service.GetReviewsByBuyerIdAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Reviews);
    }

    #endregion

    #region Helper Methods

    private static SellerSubOrderItem CreateTestItem(SellerSubOrderItemStatus status)
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
            Status = SellerSubOrderStatus.Delivered,
            ItemsSubtotal = 95m,
            ShippingCost = 5m,
            TotalAmount = 100m,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1),
            Order = order
        };

        var item = new SellerSubOrderItem
        {
            Id = TestItemId,
            SellerSubOrderId = TestSubOrderId,
            ProductId = TestProductId,
            ProductTitle = "Test Product",
            UnitPrice = 95m,
            Quantity = 1,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            SellerSubOrder = subOrder
        };

        if (status == SellerSubOrderItemStatus.Delivered)
        {
            item.DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1);
        }

        subOrder.Items = new List<SellerSubOrderItem> { item };
        order.SellerSubOrders = new List<SellerSubOrder> { subOrder };

        return item;
    }

    private static ProductReview CreateTestReview()
    {
        return new ProductReview
        {
            Id = Guid.NewGuid(),
            OrderId = TestOrderId,
            SellerSubOrderId = TestSubOrderId,
            SellerSubOrderItemId = TestItemId,
            ProductId = TestProductId,
            StoreId = TestStoreId,
            BuyerId = TestBuyerId,
            Rating = 5,
            ReviewText = "Great product!",
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
