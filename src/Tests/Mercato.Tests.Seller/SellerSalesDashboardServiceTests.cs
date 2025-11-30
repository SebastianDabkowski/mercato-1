using Mercato.Seller.Application.Queries;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Moq;

namespace Mercato.Tests.Seller;

public class SellerSalesDashboardServiceTests
{
    private readonly Guid _testStoreId = Guid.NewGuid();

    [Fact]
    public async Task GetDashboardAsync_WithValidQuery_ReturnsDashboardResult()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var expectedOrderCount = 10;
        var expectedGmv = 1500.00m;
        var expectedChartData = new List<SalesChartDataPoint>
        {
            new() { Date = startDate, Gmv = 500m, OrderCount = 3 },
            new() { Date = startDate.AddDays(3), Gmv = 1000m, OrderCount = 7 }
        };
        var expectedProducts = new List<ProductFilterItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Product 1" },
            new() { Id = Guid.NewGuid(), Title = "Product 2" }
        };
        var expectedCategories = new List<string> { "Category 1", "Category 2" };

        var mockRepository = new Mock<ISellerSalesDashboardRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetSalesMetricsAsync(
                _testStoreId, startDate, endDate, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderCount, expectedGmv));
        mockRepository.Setup(r => r.GetSalesChartDataAsync(
                _testStoreId, startDate, endDate, SalesGranularity.Day, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChartData);
        mockRepository.Setup(r => r.GetProductsForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProducts);
        mockRepository.Setup(r => r.GetCategoriesForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCategories);

        var service = new SellerSalesDashboardService(mockRepository.Object);

        var query = new SellerSalesDashboardQuery
        {
            StoreId = _testStoreId,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = SalesGranularity.Day
        };

        // Act
        var result = await service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(expectedGmv, result.TotalGmv);
        Assert.Equal(expectedOrderCount, result.TotalOrders);
        Assert.Equal(2, result.ChartDataPoints.Count);
        Assert.Equal(2, result.AvailableProducts.Count);
        Assert.Equal(2, result.AvailableCategories.Count);
        Assert.True(result.RetrievedAt <= DateTimeOffset.UtcNow);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetDashboardAsync_WithProductFilter_PassesFilterToRepository()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var productId = Guid.NewGuid();

        var mockRepository = new Mock<ISellerSalesDashboardRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetSalesMetricsAsync(
                _testStoreId, startDate, endDate, productId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 750m));
        mockRepository.Setup(r => r.GetSalesChartDataAsync(
                _testStoreId, startDate, endDate, SalesGranularity.Day, productId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SalesChartDataPoint>());
        mockRepository.Setup(r => r.GetProductsForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductFilterItem>());
        mockRepository.Setup(r => r.GetCategoriesForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var service = new SellerSalesDashboardService(mockRepository.Object);

        var query = new SellerSalesDashboardQuery
        {
            StoreId = _testStoreId,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = SalesGranularity.Day,
            ProductId = productId
        };

        // Act
        var result = await service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalOrders);
        Assert.Equal(750m, result.TotalGmv);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetDashboardAsync_WithCategoryFilter_PassesFilterToRepository()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var category = "Electronics";

        var mockRepository = new Mock<ISellerSalesDashboardRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetSalesMetricsAsync(
                _testStoreId, startDate, endDate, null, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync((8, 1200m));
        mockRepository.Setup(r => r.GetSalesChartDataAsync(
                _testStoreId, startDate, endDate, SalesGranularity.Week, null, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SalesChartDataPoint>());
        mockRepository.Setup(r => r.GetProductsForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductFilterItem>());
        mockRepository.Setup(r => r.GetCategoriesForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var service = new SellerSalesDashboardService(mockRepository.Object);

        var query = new SellerSalesDashboardQuery
        {
            StoreId = _testStoreId,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = SalesGranularity.Week,
            Category = category
        };

        // Act
        var result = await service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.TotalOrders);
        Assert.Equal(1200m, result.TotalGmv);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetDashboardAsync_WithNoSalesData_ReturnsEmptyResult()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<ISellerSalesDashboardRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetSalesMetricsAsync(
                _testStoreId, startDate, endDate, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0m));
        mockRepository.Setup(r => r.GetSalesChartDataAsync(
                _testStoreId, startDate, endDate, SalesGranularity.Day, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SalesChartDataPoint>());
        mockRepository.Setup(r => r.GetProductsForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductFilterItem>());
        mockRepository.Setup(r => r.GetCategoriesForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var service = new SellerSalesDashboardService(mockRepository.Object);

        var query = new SellerSalesDashboardQuery
        {
            StoreId = _testStoreId,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = SalesGranularity.Day
        };

        // Act
        var result = await service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalOrders);
        Assert.Equal(0m, result.TotalGmv);
        Assert.Empty(result.ChartDataPoints);
        Assert.Empty(result.AvailableProducts);
        Assert.Empty(result.AvailableCategories);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetDashboardAsync_WithMonthlyGranularity_UsesMonthGranularity()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-90);
        var endDate = DateTimeOffset.UtcNow;
        var expectedChartData = new List<SalesChartDataPoint>
        {
            new() { Date = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), Gmv = 5000m, OrderCount = 50 },
            new() { Date = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero), Gmv = 6000m, OrderCount = 60 },
            new() { Date = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero), Gmv = 7000m, OrderCount = 70 }
        };

        var mockRepository = new Mock<ISellerSalesDashboardRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetSalesMetricsAsync(
                _testStoreId, startDate, endDate, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((180, 18000m));
        mockRepository.Setup(r => r.GetSalesChartDataAsync(
                _testStoreId, startDate, endDate, SalesGranularity.Month, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChartData);
        mockRepository.Setup(r => r.GetProductsForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductFilterItem>());
        mockRepository.Setup(r => r.GetCategoriesForFilterAsync(_testStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var service = new SellerSalesDashboardService(mockRepository.Object);

        var query = new SellerSalesDashboardQuery
        {
            StoreId = _testStoreId,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = SalesGranularity.Month
        };

        // Act
        var result = await service.GetDashboardAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.ChartDataPoints.Count);
        Assert.Equal(18000m, result.TotalGmv);
        Assert.Equal(180, result.TotalOrders);
        mockRepository.VerifyAll();
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SellerSalesDashboardService(null!));
    }

    [Fact]
    public async Task GetDashboardAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<ISellerSalesDashboardRepository>(MockBehavior.Strict);
        var service = new SellerSalesDashboardService(mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetDashboardAsync(null!));
    }
}
