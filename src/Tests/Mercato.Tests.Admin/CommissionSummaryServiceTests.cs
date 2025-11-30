using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

/// <summary>
/// Unit tests for the CommissionSummaryService.
/// </summary>
public class CommissionSummaryServiceTests
{
    private readonly Mock<ICommissionSummaryRepository> _mockRepository;
    private readonly Mock<ILogger<CommissionSummaryService>> _mockLogger;
    private readonly CommissionSummaryService _service;

    public CommissionSummaryServiceTests()
    {
        _mockRepository = new Mock<ICommissionSummaryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CommissionSummaryService>>();

        _service = new CommissionSummaryService(
            _mockRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CommissionSummaryService(
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CommissionSummaryService(
            _mockRepository.Object,
            null!));
    }

    #endregion

    #region GetSummaryAsync Tests

    [Fact]
    public async Task GetSummaryAsync_WithValidQuery_ReturnsSuccess()
    {
        // Arrange
        var rows = new List<SellerCommissionSummaryRow>
        {
            new()
            {
                SellerId = Guid.NewGuid(),
                SellerName = "Test Store",
                TotalGMV = 1000.00m,
                TotalCommission = 100.00m,
                TotalNetPayout = 900.00m,
                OrderCount = 5
            }
        };

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 1000.00m, 100.00m, 900.00m, 5));

        var query = new CommissionSummaryFilterQuery();

        // Act
        var result = await _service.GetSummaryAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Rows);
        Assert.Equal("Test Store", result.Rows[0].SellerName);
        Assert.Equal(1000.00m, result.TotalGMV);
        Assert.Equal(100.00m, result.TotalCommission);
        Assert.Equal(900.00m, result.TotalNetPayout);
        Assert.Equal(5, result.TotalOrderCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithDateFilters_PassesFiltersToRepository()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(fromDate, toDate))
            .ReturnsAsync((new List<SellerCommissionSummaryRow>(), 0m, 0m, 0m, 0));

        var query = new CommissionSummaryFilterQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        // Act
        var result = await _service.GetSummaryAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.GetSummaryDataAsync(fromDate, toDate), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_WithFromDateAfterToDate_ReturnsValidationError()
    {
        // Arrange
        var query = new CommissionSummaryFilterQuery
        {
            FromDate = DateTimeOffset.UtcNow.AddDays(1),
            ToDate = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _service.GetSummaryAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    [Fact]
    public async Task GetSummaryAsync_WithMultipleSellers_ReturnsSortedByGMV()
    {
        // Arrange
        var sellerId1 = Guid.NewGuid();
        var sellerId2 = Guid.NewGuid();
        var rows = new List<SellerCommissionSummaryRow>
        {
            new()
            {
                SellerId = sellerId1,
                SellerName = "Store A",
                TotalGMV = 2000.00m,
                TotalCommission = 200.00m,
                TotalNetPayout = 1800.00m,
                OrderCount = 10
            },
            new()
            {
                SellerId = sellerId2,
                SellerName = "Store B",
                TotalGMV = 1000.00m,
                TotalCommission = 100.00m,
                TotalNetPayout = 900.00m,
                OrderCount = 5
            }
        };

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 3000.00m, 300.00m, 2700.00m, 15));

        var query = new CommissionSummaryFilterQuery();

        // Act
        var result = await _service.GetSummaryAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(3000.00m, result.TotalGMV);
        Assert.Equal(300.00m, result.TotalCommission);
        Assert.Equal(2700.00m, result.TotalNetPayout);
        Assert.Equal(15, result.TotalOrderCount);
    }

    #endregion

    #region GetSellerOrdersAsync Tests

    [Fact]
    public async Task GetSellerOrdersAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var rows = new List<OrderCommissionRow>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                OrderDate = DateTimeOffset.UtcNow.AddDays(-1),
                OrderAmount = 100.00m,
                CommissionRate = 0.10m,
                CommissionAmount = 10.00m,
                NetPayout = 90.00m,
                CalculatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockRepository
            .Setup(r => r.GetSellerOrdersAsync(
                sellerId,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                1,
                20))
            .ReturnsAsync((rows, 1, sellerId, "Test Store"));

        // Act
        var result = await _service.GetSellerOrdersAsync(sellerId, null, null, 1, 20);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Rows);
        Assert.Equal("Test Store", result.SellerName);
        Assert.Equal(sellerId, result.SellerId);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetSellerOrdersAsync_WithEmptySellerId_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetSellerOrdersAsync(Guid.Empty, null, null, 1, 20);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetSellerOrdersAsync_WithInvalidPage_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetSellerOrdersAsync(Guid.NewGuid(), null, null, 0, 20);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page must be greater than or equal to 1.", result.Errors);
    }

    [Fact]
    public async Task GetSellerOrdersAsync_WithInvalidPageSize_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetSellerOrdersAsync(Guid.NewGuid(), null, null, 1, 0);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors, e => e.Contains("Page size must be between"));
    }

    [Fact]
    public async Task GetSellerOrdersAsync_WithPageSizeOver100_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetSellerOrdersAsync(Guid.NewGuid(), null, null, 1, 101);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors, e => e.Contains("Page size must be between"));
    }

    [Fact]
    public async Task GetSellerOrdersAsync_WithFromDateAfterToDate_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetSellerOrdersAsync(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow,
            1,
            20);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    [Fact]
    public async Task GetSellerOrdersAsync_WithDateFilters_PassesFiltersToRepository()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        _mockRepository
            .Setup(r => r.GetSellerOrdersAsync(sellerId, fromDate, toDate, 2, 10))
            .ReturnsAsync((new List<OrderCommissionRow>(), 0, sellerId, "Test Store"));

        // Act
        var result = await _service.GetSellerOrdersAsync(sellerId, fromDate, toDate, 2, 10);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.GetSellerOrdersAsync(sellerId, fromDate, toDate, 2, 10), Times.Once);
    }

    #endregion

    #region ExportToCsvAsync Tests

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_ReturnsSuccessWithCsvContent()
    {
        // Arrange
        var rows = new List<SellerCommissionSummaryRow>
        {
            new()
            {
                SellerId = Guid.NewGuid(),
                SellerName = "Test Store",
                TotalGMV = 1000.00m,
                TotalCommission = 100.00m,
                TotalNetPayout = 900.00m,
                OrderCount = 5
            }
        };

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 1000.00m, 100.00m, 900.00m, 5));

        var query = new CommissionSummaryFilterQuery();

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.CsvContent);
        Assert.StartsWith("CommissionSummary_", result.FileName);
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
        var rows = new List<SellerCommissionSummaryRow>
        {
            new()
            {
                SellerId = Guid.NewGuid(),
                SellerName = "Store, Inc.",
                TotalGMV = 1000.00m,
                TotalCommission = 100.00m,
                TotalNetPayout = 900.00m,
                OrderCount = 5
            }
        };

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 1000.00m, 100.00m, 900.00m, 5));

        var query = new CommissionSummaryFilterQuery();

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
        var rows = new List<SellerCommissionSummaryRow>
        {
            new()
            {
                SellerId = Guid.NewGuid(),
                SellerName = "Store \"Special\" Name",
                TotalGMV = 1000.00m,
                TotalCommission = 100.00m,
                TotalNetPayout = 900.00m,
                OrderCount = 5
            }
        };

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 1000.00m, 100.00m, 900.00m, 5));

        var query = new CommissionSummaryFilterQuery();

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.CsvContent);
        Assert.Contains("\"Store \"\"Special\"\" Name\"", csvContent);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithDateRange_IncludesDatesInFilename()
    {
        // Arrange
        var rows = new List<SellerCommissionSummaryRow>();

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 0m, 0m, 0m, 0));

        var query = new CommissionSummaryFilterQuery
        {
            FromDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
            ToDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero)
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
        var rows = new List<SellerCommissionSummaryRow>();

        _mockRepository
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((rows, 0m, 0m, 0m, 0));

        var query = new CommissionSummaryFilterQuery();

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.CsvContent);
        Assert.Contains("Seller ID", csvContent);
        Assert.Contains("Seller Name", csvContent);
        Assert.Contains("Total GMV", csvContent);
        Assert.Contains("Total Commission", csvContent);
        Assert.Contains("Total Net Payout", csvContent);
        Assert.Contains("Order Count", csvContent);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithFromDateAfterToDate_ReturnsValidationError()
    {
        // Arrange
        var query = new CommissionSummaryFilterQuery
        {
            FromDate = DateTimeOffset.UtcNow.AddDays(1),
            ToDate = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _service.ExportToCsvAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    #endregion
}
