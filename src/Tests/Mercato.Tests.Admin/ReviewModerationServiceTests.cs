using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

/// <summary>
/// Unit tests for the ReviewModerationService.
/// </summary>
public class ReviewModerationServiceTests
{
    private readonly Mock<IProductReviewRepository> _mockProductReviewRepository;
    private readonly Mock<IAdminAuditRepository> _mockAdminAuditRepository;
    private readonly Mock<ILogger<ReviewModerationService>> _mockLogger;
    private readonly ReviewModerationService _service;

    public ReviewModerationServiceTests()
    {
        _mockProductReviewRepository = new Mock<IProductReviewRepository>(MockBehavior.Strict);
        _mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ReviewModerationService>>();

        _service = new ReviewModerationService(
            _mockProductReviewRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProductReviewRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReviewModerationService(
            null!,
            _mockAdminAuditRepository.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAdminAuditRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReviewModerationService(
            _mockProductReviewRepository.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReviewModerationService(
            _mockProductReviewRepository.Object,
            _mockAdminAuditRepository.Object,
            null!));
    }

    #endregion

    #region GetReviewsAsync Tests

    [Fact]
    public async Task GetReviewsAsync_ReturnsSuccessWithReviews()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                BuyerId = "buyer-123456789",
                Rating = 5,
                ReviewText = "Great product!",
                Status = ReviewStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReviewStatus>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((reviews, 1));

        var query = new AdminReviewFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Reviews);
        Assert.Equal(5, result.Reviews[0].Rating);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetReviewsAsync_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var statuses = new List<ReviewStatus> { ReviewStatus.Pending, ReviewStatus.Published };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                "test",
                statuses,
                fromDate,
                toDate,
                2,
                10))
            .ReturnsAsync((new List<ProductReview>(), 0));

        var query = new AdminReviewFilterQuery
        {
            SearchTerm = "test",
            Statuses = statuses,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockProductReviewRepository.Verify(r => r.GetAllFilteredAsync(
            "test", statuses, fromDate, toDate, 2, 10), Times.Once);
    }

    [Fact]
    public async Task GetReviewsAsync_TruncatesLongReviewText()
    {
        // Arrange
        var longText = new string('A', 150);
        var reviews = new List<ProductReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                BuyerId = "buyer-123",
                Rating = 4,
                ReviewText = longText,
                Status = ReviewStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReviewStatus>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((reviews, 1));

        var query = new AdminReviewFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.EndsWith("...", result.Reviews[0].ReviewTextPreview);
        Assert.True(result.Reviews[0].ReviewTextPreview.Length <= 103); // 100 chars + "..."
    }

    [Fact]
    public async Task GetReviewsAsync_GeneratesBuyerAlias()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                BuyerId = "buyer-123456789",
                Rating = 3,
                ReviewText = "Good",
                Status = ReviewStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReviewStatus>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((reviews, 1));

        var query = new AdminReviewFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.StartsWith("Buyer-", result.Reviews[0].BuyerAlias);
        Assert.Contains("***", result.Reviews[0].BuyerAlias);
    }

    #endregion

    #region GetReviewDetailsAsync Tests

    [Fact]
    public async Task GetReviewDetailsAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            OrderId = Guid.NewGuid(),
            SellerSubOrderId = Guid.NewGuid(),
            SellerSubOrderItemId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123456789",
            Rating = 5,
            ReviewText = "Excellent product!",
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        // Act
        var result = await _service.GetReviewDetailsAsync(reviewId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReviewDetails);
        Assert.Equal(reviewId, result.ReviewDetails.Id);
        Assert.Equal("Excellent product!", result.ReviewDetails.ReviewText);
    }

    [Fact]
    public async Task GetReviewDetailsAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync((ProductReview?)null);

        // Act
        var result = await _service.GetReviewDetailsAsync(reviewId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review not found.", result.Errors);
    }

    #endregion

    #region ModerateReviewAsync Tests

    [Fact]
    public async Task ModerateReviewAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123",
            Rating = 3,
            ReviewText = "Average product",
            Status = ReviewStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        _mockProductReviewRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ProductReview>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new ModerateReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user",
            NewStatus = ReviewStatus.Published,
            ModerationReason = "Approved - content is appropriate"
        };

        // Act
        var result = await _service.ModerateReviewAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ReviewStatus.Published, review.Status);
        _mockAdminAuditRepository.Verify(r => r.AddAsync(It.IsAny<AdminAuditLog>()), Times.Once);
    }

    [Fact]
    public async Task ModerateReviewAsync_WithEmptyReviewId_ReturnsValidationError()
    {
        // Arrange
        var command = new ModerateReviewCommand
        {
            ReviewId = Guid.Empty,
            AdminUserId = "admin-user",
            NewStatus = ReviewStatus.Published,
            ModerationReason = "Test reason"
        };

        // Act
        var result = await _service.ModerateReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review ID is required.", result.Errors);
    }

    [Fact]
    public async Task ModerateReviewAsync_WithEmptyAdminUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new ModerateReviewCommand
        {
            ReviewId = Guid.NewGuid(),
            AdminUserId = "",
            NewStatus = ReviewStatus.Published,
            ModerationReason = "Test reason"
        };

        // Act
        var result = await _service.ModerateReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Admin user ID is required.", result.Errors);
    }

    [Fact]
    public async Task ModerateReviewAsync_WithEmptyModerationReason_ReturnsValidationError()
    {
        // Arrange
        var command = new ModerateReviewCommand
        {
            ReviewId = Guid.NewGuid(),
            AdminUserId = "admin-user",
            NewStatus = ReviewStatus.Published,
            ModerationReason = ""
        };

        // Act
        var result = await _service.ModerateReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Moderation reason is required.", result.Errors);
    }

    [Fact]
    public async Task ModerateReviewAsync_ReviewNotFound_ReturnsFailure()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync((ProductReview?)null);

        var command = new ModerateReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user",
            NewStatus = ReviewStatus.Published,
            ModerationReason = "Test reason"
        };

        // Act
        var result = await _service.ModerateReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review not found.", result.Errors);
    }

    [Fact]
    public async Task ModerateReviewAsync_HidesReview_UpdatesStatusCorrectly()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123",
            Rating = 1,
            ReviewText = "Inappropriate content",
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        _mockProductReviewRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ProductReview>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new ModerateReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user",
            NewStatus = ReviewStatus.Hidden,
            ModerationReason = "Violates content guidelines"
        };

        // Act
        var result = await _service.ModerateReviewAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ReviewStatus.Hidden, review.Status);
    }

    [Fact]
    public async Task ModerateReviewAsync_CreatesAuditLogWithCorrectDetails()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123",
            Rating = 4,
            ReviewText = "Good product",
            Status = ReviewStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        AdminAuditLog? capturedAuditLog = null;

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        _mockProductReviewRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ProductReview>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .Callback<AdminAuditLog>(a => capturedAuditLog = a)
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new ModerateReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user-123",
            NewStatus = ReviewStatus.Published,
            ModerationReason = "Approved after review"
        };

        // Act
        await _service.ModerateReviewAsync(command);

        // Assert
        Assert.NotNull(capturedAuditLog);
        Assert.Equal("admin-user-123", capturedAuditLog.AdminUserId);
        Assert.Equal("ModerateReview", capturedAuditLog.Action);
        Assert.Equal("ProductReview", capturedAuditLog.EntityType);
        Assert.Equal(reviewId.ToString(), capturedAuditLog.EntityId);
        Assert.Contains("Pending", capturedAuditLog.Details);
        Assert.Contains("Published", capturedAuditLog.Details);
        Assert.Contains("Approved after review", capturedAuditLog.Details);
    }

    #endregion

    #region FlagReviewAsync Tests

    [Fact]
    public async Task FlagReviewAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123",
            Rating = 2,
            ReviewText = "Suspicious review",
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        _mockProductReviewRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ProductReview>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new FlagReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user",
            FlagReason = "Suspicious activity detected"
        };

        // Act
        var result = await _service.FlagReviewAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        _mockAdminAuditRepository.Verify(r => r.AddAsync(It.IsAny<AdminAuditLog>()), Times.Once);
    }

    [Fact]
    public async Task FlagReviewAsync_WithEmptyReviewId_ReturnsValidationError()
    {
        // Arrange
        var command = new FlagReviewCommand
        {
            ReviewId = Guid.Empty,
            AdminUserId = "admin-user",
            FlagReason = "Test reason"
        };

        // Act
        var result = await _service.FlagReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review ID is required.", result.Errors);
    }

    [Fact]
    public async Task FlagReviewAsync_WithEmptyAdminUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new FlagReviewCommand
        {
            ReviewId = Guid.NewGuid(),
            AdminUserId = "",
            FlagReason = "Test reason"
        };

        // Act
        var result = await _service.FlagReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Admin user ID is required.", result.Errors);
    }

    [Fact]
    public async Task FlagReviewAsync_WithEmptyFlagReason_ReturnsValidationError()
    {
        // Arrange
        var command = new FlagReviewCommand
        {
            ReviewId = Guid.NewGuid(),
            AdminUserId = "admin-user",
            FlagReason = ""
        };

        // Act
        var result = await _service.FlagReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Flag reason is required.", result.Errors);
    }

    [Fact]
    public async Task FlagReviewAsync_ReviewNotFound_ReturnsFailure()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync((ProductReview?)null);

        var command = new FlagReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user",
            FlagReason = "Test reason"
        };

        // Act
        var result = await _service.FlagReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review not found.", result.Errors);
    }

    [Fact]
    public async Task FlagReviewAsync_AlreadyPending_ReturnsError()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123",
            Rating = 3,
            ReviewText = "Already flagged",
            Status = ReviewStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        var command = new FlagReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user",
            FlagReason = "Test reason"
        };

        // Act
        var result = await _service.FlagReviewAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Review is already flagged for moderation.", result.Errors);
    }

    [Fact]
    public async Task FlagReviewAsync_CreatesAuditLogWithCorrectDetails()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = reviewId,
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            BuyerId = "buyer-123",
            Rating = 1,
            ReviewText = "Needs review",
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        AdminAuditLog? capturedAuditLog = null;

        _mockProductReviewRepository
            .Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        _mockProductReviewRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ProductReview>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .Callback<AdminAuditLog>(a => capturedAuditLog = a)
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new FlagReviewCommand
        {
            ReviewId = reviewId,
            AdminUserId = "admin-user-456",
            FlagReason = "Reported by user for spam"
        };

        // Act
        await _service.FlagReviewAsync(command);

        // Assert
        Assert.NotNull(capturedAuditLog);
        Assert.Equal("admin-user-456", capturedAuditLog.AdminUserId);
        Assert.Equal("FlagReview", capturedAuditLog.Action);
        Assert.Equal("ProductReview", capturedAuditLog.EntityType);
        Assert.Equal(reviewId.ToString(), capturedAuditLog.EntityId);
        Assert.Contains("Published", capturedAuditLog.Details);
        Assert.Contains("Reported by user for spam", capturedAuditLog.Details);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetReviewsAsync_EmptyBuyerId_ReturnsUnknownAlias()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                BuyerId = "",
                Rating = 3,
                ReviewText = "Test",
                Status = ReviewStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReviewStatus>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((reviews, 1));

        var query = new AdminReviewFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Unknown", result.Reviews[0].BuyerAlias);
    }

    [Fact]
    public async Task GetReviewsAsync_ShortBuyerId_GeneratesPartialAlias()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                BuyerId = "abc",
                Rating = 3,
                ReviewText = "Test",
                Status = ReviewStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReviewStatus>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((reviews, 1));

        var query = new AdminReviewFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.StartsWith("Buyer-", result.Reviews[0].BuyerAlias);
    }

    [Fact]
    public async Task GetReviewsAsync_EmptyReviewText_ReturnsEmptyPreview()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                BuyerId = "buyer-123",
                Rating = 5,
                ReviewText = "",
                Status = ReviewStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductReviewRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReviewStatus>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((reviews, 1));

        var query = new AdminReviewFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetReviewsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Reviews[0].ReviewTextPreview);
    }

    #endregion
}
