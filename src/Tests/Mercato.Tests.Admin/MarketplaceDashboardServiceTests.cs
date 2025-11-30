using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Moq;

namespace Mercato.Tests.Admin;

public class MarketplaceDashboardServiceTests
{
    private readonly Mock<IMarketplaceDashboardRepository> _mockRepository;
    private readonly MarketplaceDashboardService _service;

    public MarketplaceDashboardServiceTests()
    {
        _mockRepository = new Mock<IMarketplaceDashboardRepository>(MockBehavior.Strict);
        _service = new MarketplaceDashboardService(_mockRepository.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MarketplaceDashboardService(null!));
    }

    [Fact]
    public async Task GetDashboardAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetDashboardAsync(null!));
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCorrectData()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var query = new MarketplaceDashboardQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        _mockRepository.Setup(r => r.GetOrderMetricsAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((150, 25000.50m));

        _mockRepository.Setup(r => r.GetActiveSellerCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        _mockRepository.Setup(r => r.GetActiveProductCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1250);

        _mockRepository.Setup(r => r.GetNewUserCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(85);

        // Act
        var result = await _service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(150, result.TotalOrders);
        Assert.Equal(25000.50m, result.TotalGmv);
        Assert.Equal(42, result.ActiveSellers);
        Assert.Equal(1250, result.ActiveProducts);
        Assert.Equal(85, result.NewUsers);
        Assert.True(result.RetrievedAt > DateTimeOffset.MinValue);

        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetDashboardAsync_WithNoData_ReturnsZeros()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var query = new MarketplaceDashboardQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        _mockRepository.Setup(r => r.GetOrderMetricsAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0m));

        _mockRepository.Setup(r => r.GetActiveSellerCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository.Setup(r => r.GetActiveProductCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository.Setup(r => r.GetNewUserCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalOrders);
        Assert.Equal(0m, result.TotalGmv);
        Assert.Equal(0, result.ActiveSellers);
        Assert.Equal(0, result.ActiveProducts);
        Assert.Equal(0, result.NewUsers);

        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetDashboardAsync_PassesDateRangeCorrectly()
    {
        // Arrange
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var query = new MarketplaceDashboardQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        _mockRepository.Setup(r => r.GetOrderMetricsAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((10, 1000m));

        _mockRepository.Setup(r => r.GetActiveSellerCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _mockRepository.Setup(r => r.GetActiveProductCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _mockRepository.Setup(r => r.GetNewUserCountAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        // Act
        var result = await _service.GetDashboardAsync(query);

        // Assert
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);

        _mockRepository.Verify(r => r.GetOrderMetricsAsync(startDate, endDate, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetNewUserCountAsync(startDate, endDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_SetsRetrievedAtToCurrentTime()
    {
        // Arrange
        var query = new MarketplaceDashboardQuery
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            EndDate = DateTimeOffset.UtcNow
        };
        var beforeCall = DateTimeOffset.UtcNow;

        _mockRepository.Setup(r => r.GetOrderMetricsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 100m));

        _mockRepository.Setup(r => r.GetActiveSellerCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockRepository.Setup(r => r.GetActiveProductCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockRepository.Setup(r => r.GetNewUserCountAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GetDashboardAsync(query);
        var afterCall = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.RetrievedAt >= beforeCall);
        Assert.True(result.RetrievedAt <= afterCall);
    }

    [Fact]
    public async Task GetDashboardAsync_WithLargeValues_ReturnsCorrectly()
    {
        // Arrange
        var query = new MarketplaceDashboardQuery
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-365),
            EndDate = DateTimeOffset.UtcNow
        };

        _mockRepository.Setup(r => r.GetOrderMetricsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1_000_000, 999_999_999.99m));

        _mockRepository.Setup(r => r.GetActiveSellerCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50_000);

        _mockRepository.Setup(r => r.GetActiveProductCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5_000_000);

        _mockRepository.Setup(r => r.GetNewUserCountAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1_000_000);

        // Act
        var result = await _service.GetDashboardAsync(query);

        // Assert
        Assert.Equal(1_000_000, result.TotalOrders);
        Assert.Equal(999_999_999.99m, result.TotalGmv);
        Assert.Equal(50_000, result.ActiveSellers);
        Assert.Equal(5_000_000, result.ActiveProducts);
        Assert.Equal(1_000_000, result.NewUsers);
    }
}
