using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Web.Pages.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class StoreIndexModelTests
{
    private const string TestSlug = "test-store";
    private static readonly Guid TestStoreId = Guid.NewGuid();

    [Fact]
    public async Task OnGetAsync_WithValidSlug_LoadsStoreAndRating()
    {
        // Arrange
        var store = CreateTestStore();
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        mockStoreProfileService.Setup(s => s.GetPublicStoreBySlugAsync(TestSlug))
            .ReturnsAsync(store);
        mockSellerRatingService.Setup(s => s.GetAverageRatingForStoreAsync(TestStoreId))
            .ReturnsAsync(GetAverageRatingResult.Success(4.5, 10));

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(TestSlug);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Store);
        Assert.Equal(4.5, model.AverageRating);
        Assert.Equal(10, model.RatingCount);
        Assert.False(model.IsNotFound);
        Assert.False(model.IsUnavailable);
    }

    [Fact]
    public async Task OnGetAsync_WithValidSlugAndNoRatings_ShowsNullRating()
    {
        // Arrange
        var store = CreateTestStore();
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        mockStoreProfileService.Setup(s => s.GetPublicStoreBySlugAsync(TestSlug))
            .ReturnsAsync(store);
        mockSellerRatingService.Setup(s => s.GetAverageRatingForStoreAsync(TestStoreId))
            .ReturnsAsync(GetAverageRatingResult.Success(null, 0));

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(TestSlug);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Store);
        Assert.Null(model.AverageRating);
        Assert.Equal(0, model.RatingCount);
    }

    [Fact]
    public async Task OnGetAsync_WhenRatingServiceFails_StillShowsStore()
    {
        // Arrange
        var store = CreateTestStore();
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        mockStoreProfileService.Setup(s => s.GetPublicStoreBySlugAsync(TestSlug))
            .ReturnsAsync(store);
        mockSellerRatingService.Setup(s => s.GetAverageRatingForStoreAsync(TestStoreId))
            .ReturnsAsync(GetAverageRatingResult.Failure("Database error"));

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(TestSlug);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Store);
        Assert.Null(model.AverageRating);
        Assert.Equal(0, model.RatingCount);
        Assert.False(model.IsNotFound);
    }

    [Fact]
    public async Task OnGetAsync_WithEmptySlug_SetsIsNotFoundToTrue()
    {
        // Arrange
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync("");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.IsNotFound);
        Assert.Null(model.Store);
    }

    [Fact]
    public async Task OnGetAsync_WhenStoreNotFound_SetsIsNotFoundToTrue()
    {
        // Arrange
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        mockStoreProfileService.Setup(s => s.GetPublicStoreBySlugAsync(TestSlug))
            .ReturnsAsync((Store?)null);
        mockStoreProfileService.Setup(s => s.StoreExistsBySlugAsync(TestSlug))
            .ReturnsAsync(false);

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(TestSlug);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.IsNotFound);
        Assert.Null(model.Store);
    }

    [Fact]
    public async Task OnGetAsync_WhenStoreExistsButUnavailable_SetsIsUnavailableToTrue()
    {
        // Arrange
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        mockStoreProfileService.Setup(s => s.GetPublicStoreBySlugAsync(TestSlug))
            .ReturnsAsync((Store?)null);
        mockStoreProfileService.Setup(s => s.StoreExistsBySlugAsync(TestSlug))
            .ReturnsAsync(true);

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(TestSlug);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.IsUnavailable);
        Assert.False(model.IsNotFound);
        Assert.Null(model.Store);
    }

    [Theory]
    [InlineData(1.0, 1)]
    [InlineData(2.5, 5)]
    [InlineData(4.8, 100)]
    [InlineData(5.0, 50)]
    public async Task OnGetAsync_WithDifferentRatings_SetsCorrectValues(double averageRating, int ratingCount)
    {
        // Arrange
        var store = CreateTestStore();
        var mockStoreProfileService = new Mock<IStoreProfileService>(MockBehavior.Strict);
        var mockSellerRatingService = new Mock<ISellerRatingService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<IndexModel>>();

        mockStoreProfileService.Setup(s => s.GetPublicStoreBySlugAsync(TestSlug))
            .ReturnsAsync(store);
        mockSellerRatingService.Setup(s => s.GetAverageRatingForStoreAsync(TestStoreId))
            .ReturnsAsync(GetAverageRatingResult.Success(averageRating, ratingCount));

        var model = CreateModel(mockStoreProfileService.Object, mockSellerRatingService.Object, mockLogger.Object);

        // Act
        await model.OnGetAsync(TestSlug);

        // Assert
        Assert.Equal(averageRating, model.AverageRating);
        Assert.Equal(ratingCount, model.RatingCount);
    }

    private static IndexModel CreateModel(
        IStoreProfileService storeProfileService,
        ISellerRatingService sellerRatingService,
        ILogger<IndexModel> logger)
    {
        var model = new IndexModel(storeProfileService, sellerRatingService, logger);

        var httpContext = new DefaultHttpContext();
        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private static Store CreateTestStore()
    {
        return new Store
        {
            Id = TestStoreId,
            SellerId = "test-seller-id",
            Name = "Test Store",
            Slug = TestSlug,
            Status = StoreStatus.Active,
            Description = "A test store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
