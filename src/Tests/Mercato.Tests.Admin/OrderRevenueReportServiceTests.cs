using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

/// <summary>
/// Unit tests for the OrderRevenueReportService.
/// </summary>
public class OrderRevenueReportServiceTests
{
    private readonly Mock<IOrderRevenueReportRepository> _mockRepository;
    private readonly Mock<ILogger<OrderRevenueReportService>> _mockLogger;
    private readonly OrderRevenueReportService _service;

    public OrderRevenueReportServiceTests()
    {
        _mockRepository = new Mock<IOrderRevenueReportRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<OrderRevenueReportService>>();

        _service = new OrderRevenueReportService(
            _mockRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderRevenueReportService(
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderRevenueReportService(
            _mockRepository.Object,
            null!));
    }

    #endregion

    #region GetReportAsync Tests

    [Fact]
    public async Task GetReportAsync_WithValidQuery_ReturnsSuccess()
    {
        // Arrange
        var rows = new List<OrderRevenueReportRow>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                OrderDate = DateTimeOffset.UtcNow.AddDays(-1),
                BuyerEmail = "buyer@example.com",
                SellerName = "Test Store",
                SellerId = Guid.NewGuid(),
                OrderStatus = OrderStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                OrderValue = 100.00m,
                Commission = 10.00m,
                PayoutAmount = 90.00m
            }
        };

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((rows, 1, 100.00m, 10.00m, 90.00m));

        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Rows);
        Assert.Equal("ORD-001", result.Rows[0].OrderNumber);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(100.00m, result.TotalOrderValue);
        Assert.Equal(10.00m, result.TotalCommission);
        Assert.Equal(90.00m, result.TotalPayoutAmount);
    }

    [Fact]
    public async Task GetReportAsync_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var sellerId = Guid.NewGuid();
        var orderStatuses = new List<OrderStatus> { OrderStatus.Paid, OrderStatus.Delivered };
        var paymentStatuses = new List<PaymentStatus> { PaymentStatus.Paid };

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                fromDate,
                toDate,
                sellerId,
                orderStatuses,
                paymentStatuses,
                2,
                10))
            .ReturnsAsync((new List<OrderRevenueReportRow>(), 0, 0m, 0m, 0m));

        var query = new OrderRevenueReportFilterQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            SellerId = sellerId,
            OrderStatuses = orderStatuses,
            PaymentStatuses = paymentStatuses,
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.GetReportDataAsync(
            fromDate, toDate, sellerId, orderStatuses, paymentStatuses, 2, 10), Times.Once);
    }

    [Fact]
    public async Task GetReportAsync_WithInvalidPage_ReturnsValidationError()
    {
        // Arrange
        var query = new OrderRevenueReportFilterQuery
        {
            Page = 0,
            PageSize = 20
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page must be greater than or equal to 1.", result.Errors);
    }

    [Fact]
    public async Task GetReportAsync_WithInvalidPageSize_ReturnsValidationError()
    {
        // Arrange
        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 0
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetReportAsync_WithPageSizeOver100_ReturnsValidationError()
    {
        // Arrange
        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 101
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetReportAsync_WithFromDateAfterToDate_ReturnsValidationError()
    {
        // Arrange
        var query = new OrderRevenueReportFilterQuery
        {
            FromDate = DateTimeOffset.UtcNow.AddDays(1),
            ToDate = DateTimeOffset.UtcNow,
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    [Fact]
    public async Task GetReportAsync_EmptyStatusFilters_PassesNullToRepository()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                null,
                null,
                null,
                null,
                null,
                1,
                20))
            .ReturnsAsync((new List<OrderRevenueReportRow>(), 0, 0m, 0m, 0m));

        var query = new OrderRevenueReportFilterQuery
        {
            OrderStatuses = [],
            PaymentStatuses = [],
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.GetReportDataAsync(
            null, null, null, null, null, 1, 20), Times.Once);
    }

    #endregion

    #region ExportToCsvAsync Tests

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_ReturnsSuccessWithCsvContent()
    {
        // Arrange
        var rows = new List<OrderRevenueReportRow>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                OrderDate = DateTimeOffset.Parse("2024-01-15T10:30:00+00:00"),
                BuyerEmail = "buyer@example.com",
                SellerName = "Test Store",
                SellerId = Guid.NewGuid(),
                OrderStatus = OrderStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                OrderValue = 100.00m,
                Commission = 10.00m,
                PayoutAmount = 90.00m
            }
        };

        _mockRepository
            .Setup(r => r.GetCountAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>()))
            .ReturnsAsync(1);

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>(),
                1,
                10000))
            .ReturnsAsync((rows, 1, 100.00m, 10.00m, 90.00m));

        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.CsvContent);
        Assert.StartsWith("OrderRevenueReport_", result.FileName);
        Assert.EndsWith(".csv", result.FileName);

        // Verify UTF-8 BOM
        Assert.Equal(0xEF, result.CsvContent[0]);
        Assert.Equal(0xBB, result.CsvContent[1]);
        Assert.Equal(0xBF, result.CsvContent[2]);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithFieldsContainingCommas_EscapesCorrectly()
    {
        // Arrange
        var rows = new List<OrderRevenueReportRow>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                OrderDate = DateTimeOffset.UtcNow,
                BuyerEmail = "buyer@example.com",
                SellerName = "Store, Inc.",
                SellerId = Guid.NewGuid(),
                OrderStatus = OrderStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                OrderValue = 100.00m,
                Commission = 10.00m,
                PayoutAmount = 90.00m
            }
        };

        _mockRepository
            .Setup(r => r.GetCountAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>()))
            .ReturnsAsync(1);

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>(),
                1,
                10000))
            .ReturnsAsync((rows, 1, 100.00m, 10.00m, 90.00m));

        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.CsvContent);
        Assert.Contains("\"Store, Inc.\"", csvContent);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithFieldsContainingQuotes_EscapesCorrectly()
    {
        // Arrange
        var rows = new List<OrderRevenueReportRow>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                OrderDate = DateTimeOffset.UtcNow,
                BuyerEmail = "buyer@example.com",
                SellerName = "Store \"Special\" Name",
                SellerId = Guid.NewGuid(),
                OrderStatus = OrderStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                OrderValue = 100.00m,
                Commission = 10.00m,
                PayoutAmount = 90.00m
            }
        };

        _mockRepository
            .Setup(r => r.GetCountAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>()))
            .ReturnsAsync(1);

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>(),
                1,
                10000))
            .ReturnsAsync((rows, 1, 100.00m, 10.00m, 90.00m));

        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.CsvContent);
        Assert.Contains("\"Store \"\"Special\"\" Name\"", csvContent);
    }

    [Fact]
    public async Task ExportToCsvAsync_ExceedsMaxRows_ReturnsError()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetCountAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>()))
            .ReturnsAsync(10001);

        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Export exceeds maximum allowed rows", result.Errors[0]);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithDateRange_IncludesDatesInFilename()
    {
        // Arrange
        var rows = new List<OrderRevenueReportRow>();

        _mockRepository
            .Setup(r => r.GetCountAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((rows, 0, 0m, 0m, 0m));

        var query = new OrderRevenueReportFilterQuery
        {
            FromDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
            ToDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero),
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("from_20240115", result.FileName);
        Assert.Contains("to_20240131", result.FileName);
    }

    [Fact]
    public async Task ExportToCsvAsync_IncludesHeaderRow()
    {
        // Arrange
        var rows = new List<OrderRevenueReportRow>();

        _mockRepository
            .Setup(r => r.GetCountAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.GetReportDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<OrderStatus>?>(),
                It.IsAny<IReadOnlyList<PaymentStatus>?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((rows, 0, 0m, 0m, 0m));

        var query = new OrderRevenueReportFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.CsvContent);
        Assert.Contains("Order ID", csvContent);
        Assert.Contains("Order Number", csvContent);
        Assert.Contains("Order Date", csvContent);
        Assert.Contains("Buyer Email", csvContent);
        Assert.Contains("Seller Name", csvContent);
        Assert.Contains("Seller ID", csvContent);
        Assert.Contains("Order Status", csvContent);
        Assert.Contains("Payment Status", csvContent);
        Assert.Contains("Order Value", csvContent);
        Assert.Contains("Commission", csvContent);
        Assert.Contains("Payout Amount", csvContent);
    }

    #endregion

    #region GetDistinctSellersAsync Tests

    [Fact]
    public async Task GetDistinctSellersAsync_ReturnsSellersFromRepository()
    {
        // Arrange
        var sellers = new List<(Guid SellerId, string SellerName)>
        {
            (Guid.NewGuid(), "Store A"),
            (Guid.NewGuid(), "Store B"),
            (Guid.NewGuid(), "Store C")
        };

        _mockRepository
            .Setup(r => r.GetDistinctSellersAsync())
            .ReturnsAsync(sellers);

        // Act
        var result = await _service.GetDistinctSellersAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Store A", result[0].SellerName);
        Assert.Equal("Store B", result[1].SellerName);
        Assert.Equal("Store C", result[2].SellerName);
    }

    [Fact]
    public async Task GetDistinctSellersAsync_WhenRepositoryThrows_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetDistinctSellersAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetDistinctSellersAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion
}
