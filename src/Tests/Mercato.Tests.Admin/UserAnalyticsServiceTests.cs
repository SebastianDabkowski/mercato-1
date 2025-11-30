using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class UserAnalyticsServiceTests
{
    [Fact]
    public async Task GetAnalyticsAsync_WithValidQuery_ReturnsSuccessfulResult()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IUserAnalyticsRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetNewBuyerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        mockRepository.Setup(x => x.GetNewSellerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        mockRepository.Setup(x => x.GetUsersLoggedInCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        mockRepository.Setup(x => x.GetUsersLoggedInByRoleAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { { "Buyer", 80 }, { "Seller", 20 } });
        mockRepository.Setup(x => x.GetUsersWhoPlacedOrdersCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await service.GetAnalyticsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(10, result.NewBuyerAccounts);
        Assert.Equal(5, result.NewSellerAccounts);
        Assert.Equal(100, result.TotalActiveUsers);
        Assert.Equal(100, result.UsersWhoLoggedIn);
        Assert.Equal(50, result.UsersWhoPlacedOrders);
        Assert.Equal(2, result.ActiveUsersByRole.Count);
        Assert.False(result.HasInsufficientData);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IUserAnalyticsRepository>();
        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetAnalyticsAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAnalyticsService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IUserAnalyticsRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAnalyticsService(mockRepository.Object, null!));
    }

    [Fact]
    public async Task GetAnalyticsAsync_WhenAllMetricsFail_SetsHasInsufficientData()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IUserAnalyticsRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetNewBuyerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        mockRepository.Setup(x => x.GetNewSellerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        mockRepository.Setup(x => x.GetUsersLoggedInCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        mockRepository.Setup(x => x.GetUsersLoggedInByRoleAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        mockRepository.Setup(x => x.GetUsersWhoPlacedOrdersCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await service.GetAnalyticsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasInsufficientData);
        Assert.NotNull(result.InsufficientDataMessage);
        Assert.Equal(0, result.NewBuyerAccounts);
        Assert.Equal(0, result.NewSellerAccounts);
        Assert.Equal(0, result.TotalActiveUsers);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WhenPartialMetricsFail_ShowsPartialData()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IUserAnalyticsRepository>(MockBehavior.Strict);
        // Login metrics fail
        mockRepository.Setup(x => x.GetNewBuyerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        mockRepository.Setup(x => x.GetUsersLoggedInCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        mockRepository.Setup(x => x.GetUsersLoggedInByRoleAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        // Other metrics succeed
        mockRepository.Setup(x => x.GetNewSellerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        mockRepository.Setup(x => x.GetUsersWhoPlacedOrdersCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await service.GetAnalyticsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasInsufficientData); // Some data is available
        Assert.False(result.HasBuyerRegistrationData);
        Assert.True(result.HasSellerRegistrationData);
        Assert.False(result.HasLoginActivityData);
        Assert.True(result.HasOrderActivityData);
        Assert.Equal(5, result.NewSellerAccounts);
        Assert.Equal(50, result.UsersWhoPlacedOrders);
    }

    [Fact]
    public async Task GetAnalyticsAsync_SetsRetrievedAtToCurrentTime()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var beforeTest = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IUserAnalyticsRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetNewBuyerRegistrationsCountAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetNewSellerRegistrationsCountAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetUsersLoggedInCountAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetUsersLoggedInByRoleAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        mockRepository.Setup(x => x.GetUsersWhoPlacedOrdersCountAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await service.GetAnalyticsAsync(query);
        var afterTest = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.RetrievedAt >= beforeTest);
        Assert.True(result.RetrievedAt <= afterTest);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WhenZeroMetrics_SetsDataAvailabilityCorrectly()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IUserAnalyticsRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetNewBuyerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetNewSellerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetUsersLoggedInCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetUsersLoggedInByRoleAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        mockRepository.Setup(x => x.GetUsersWhoPlacedOrdersCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await service.GetAnalyticsAsync(query);

        // Assert
        Assert.NotNull(result);
        // Even with zero values, data is "available" (successful queries)
        Assert.True(result.HasBuyerRegistrationData);
        Assert.True(result.HasSellerRegistrationData);
        Assert.True(result.HasLoginActivityData);
        Assert.True(result.HasOrderActivityData);
        Assert.False(result.HasInsufficientData);
    }

    [Fact]
    public async Task GetAnalyticsAsync_TotalActiveUsersEqualsUsersLoggedIn()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IUserAnalyticsRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetNewBuyerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetNewSellerRegistrationsCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockRepository.Setup(x => x.GetUsersLoggedInCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);
        mockRepository.Setup(x => x.GetUsersLoggedInByRoleAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        mockRepository.Setup(x => x.GetUsersWhoPlacedOrdersCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockLogger = new Mock<ILogger<UserAnalyticsService>>();

        var service = new UserAnalyticsService(mockRepository.Object, mockLogger.Object);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await service.GetAnalyticsAsync(query);

        // Assert
        // Active users is defined as users who logged in
        Assert.Equal(75, result.TotalActiveUsers);
        Assert.Equal(75, result.UsersWhoLoggedIn);
    }
}
