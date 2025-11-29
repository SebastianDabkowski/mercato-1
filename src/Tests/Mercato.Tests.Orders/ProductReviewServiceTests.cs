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

    #region GetReviewsByProductIdPagedAsync Tests

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_ValidQuery_ReturnsPagedReviews()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReviewWithRating(5),
            CreateTestReviewWithRating(4)
        };

        _mockProductReviewRepository.Setup(r => r.GetPagedByProductIdAsync(
                TestProductId, 1, 10, ReviewSortOption.Newest))
            .ReturnsAsync((reviews, 2, 4.5));

        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 10,
            SortBy = ReviewSortOption.Newest
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Reviews.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(4.5, result.AverageRating);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_WithPagination_CalculatesPagesCorrectly()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReviewWithRating(5),
            CreateTestReviewWithRating(4)
        };

        _mockProductReviewRepository.Setup(r => r.GetPagedByProductIdAsync(
                TestProductId, 2, 2, ReviewSortOption.Newest))
            .ReturnsAsync((reviews, 5, 4.2));

        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 2,
            PageSize = 2,
            SortBy = ReviewSortOption.Newest
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages); // 5 reviews / 2 per page = 3 pages
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_EmptyProductId_ReturnsFailure()
    {
        // Arrange
        var query = new GetProductReviewsQuery
        {
            ProductId = Guid.Empty,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_InvalidPage_ReturnsFailure()
    {
        // Arrange
        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 0,
            PageSize = 10
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page must be at least 1.", result.Errors);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_InvalidPageSize_ReturnsFailure()
    {
        // Arrange
        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 0
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_PageSizeTooLarge_ReturnsFailure()
    {
        // Arrange
        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 101
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_NoReviews_ReturnsEmptyResult()
    {
        // Arrange
        _mockProductReviewRepository.Setup(r => r.GetPagedByProductIdAsync(
                TestProductId, 1, 10, ReviewSortOption.Newest))
            .ReturnsAsync((new List<ProductReview>(), 0, null));

        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Reviews);
        Assert.Equal(0, result.TotalCount);
        Assert.Null(result.AverageRating);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_HighestRatingSort_PassesSortOption()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReviewWithRating(5),
            CreateTestReviewWithRating(4)
        };

        _mockProductReviewRepository.Setup(r => r.GetPagedByProductIdAsync(
                TestProductId, 1, 10, ReviewSortOption.HighestRating))
            .ReturnsAsync((reviews, 2, 4.5));

        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 10,
            SortBy = ReviewSortOption.HighestRating
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockProductReviewRepository.Verify(r => r.GetPagedByProductIdAsync(
            TestProductId, 1, 10, ReviewSortOption.HighestRating), Times.Once);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_LowestRatingSort_PassesSortOption()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReviewWithRating(1),
            CreateTestReviewWithRating(2)
        };

        _mockProductReviewRepository.Setup(r => r.GetPagedByProductIdAsync(
                TestProductId, 1, 10, ReviewSortOption.LowestRating))
            .ReturnsAsync((reviews, 2, 1.5));

        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 10,
            SortBy = ReviewSortOption.LowestRating
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockProductReviewRepository.Verify(r => r.GetPagedByProductIdAsync(
            TestProductId, 1, 10, ReviewSortOption.LowestRating), Times.Once);
    }

    [Fact]
    public async Task GetReviewsByProductIdPagedAsync_CalculatesAverageRating_ReturnsCorrectValue()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReviewWithRating(5),
            CreateTestReviewWithRating(3),
            CreateTestReviewWithRating(4)
        };

        // Average of 5, 3, 4 = 4.0
        _mockProductReviewRepository.Setup(r => r.GetPagedByProductIdAsync(
                TestProductId, 1, 10, ReviewSortOption.Newest))
            .ReturnsAsync((reviews, 3, 4.0));

        var query = new GetProductReviewsQuery
        {
            ProductId = TestProductId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetReviewsByProductIdPagedAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(4.0, result.AverageRating);
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

    private static ProductReview CreateTestReviewWithRating(int rating)
    {
        return new ProductReview
        {
            Id = Guid.NewGuid(),
            OrderId = TestOrderId,
            SellerSubOrderId = TestSubOrderId,
            SellerSubOrderItemId = Guid.NewGuid(),
            ProductId = TestProductId,
            StoreId = TestStoreId,
            BuyerId = TestBuyerId,
            Rating = rating,
            ReviewText = $"Review with {rating} stars.",
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
