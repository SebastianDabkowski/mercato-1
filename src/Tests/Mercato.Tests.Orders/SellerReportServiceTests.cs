using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Orders;

public class SellerReportServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();

    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ISellerSubOrderRepository> _mockSellerSubOrderRepository;
    private readonly Mock<IReturnRequestRepository> _mockReturnRequestRepository;
    private readonly Mock<IShippingStatusHistoryRepository> _mockShippingStatusHistoryRepository;
    private readonly Mock<ICaseMessageRepository> _mockCaseMessageRepository;
    private readonly Mock<IOrderConfirmationEmailService> _mockEmailService;
    private readonly Mock<IShippingNotificationService> _mockShippingNotificationService;
    private readonly Mock<ISellerNotificationEmailService> _mockSellerNotificationEmailService;
    private readonly Mock<IStoreEmailProvider> _mockStoreEmailProvider;
    private readonly Mock<IRefundService> _mockRefundService;
    private readonly Mock<ICommissionService> _mockCommissionService;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly OrderService _service;

    public SellerReportServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        _mockSellerSubOrderRepository = new Mock<ISellerSubOrderRepository>(MockBehavior.Strict);
        _mockReturnRequestRepository = new Mock<IReturnRequestRepository>(MockBehavior.Strict);
        _mockShippingStatusHistoryRepository = new Mock<IShippingStatusHistoryRepository>(MockBehavior.Strict);
        _mockCaseMessageRepository = new Mock<ICaseMessageRepository>(MockBehavior.Strict);
        _mockEmailService = new Mock<IOrderConfirmationEmailService>(MockBehavior.Strict);
        _mockShippingNotificationService = new Mock<IShippingNotificationService>(MockBehavior.Strict);
        _mockSellerNotificationEmailService = new Mock<ISellerNotificationEmailService>(MockBehavior.Strict);
        _mockStoreEmailProvider = new Mock<IStoreEmailProvider>(MockBehavior.Strict);
        _mockRefundService = new Mock<IRefundService>(MockBehavior.Strict);
        _mockCommissionService = new Mock<ICommissionService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<OrderService>>();
        var returnSettings = Options.Create(new ReturnSettings { ReturnWindowDays = 30 });

        // Setup default store email provider behavior
        _mockStoreEmailProvider.Setup(p => p.GetStoreEmailsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _mockStoreEmailProvider.Setup(p => p.GetStoreEmailAsync(It.IsAny<Guid>()))
            .ReturnsAsync((string?)null);

        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockSellerSubOrderRepository.Object,
            _mockReturnRequestRepository.Object,
            _mockShippingStatusHistoryRepository.Object,
            _mockCaseMessageRepository.Object,
            _mockEmailService.Object,
            _mockShippingNotificationService.Object,
            _mockSellerNotificationEmailService.Object,
            _mockStoreEmailProvider.Object,
            _mockRefundService.Object,
            _mockCommissionService.Object,
            returnSettings,
            _mockLogger.Object);
    }

    #region GetSellerReportAsync Tests

    [Fact]
    public async Task GetSellerReportAsync_ValidQuery_ReturnsReportWithFinancialData()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder>
        {
            CreateTestSellerSubOrder(100m),
            CreateTestSellerSubOrder(200m)
        };
        var commissionRecords = new List<CommissionRecord>
        {
            CreateTestCommissionRecord(subOrders[0].OrderId, 10m),
            CreateTestCommissionRecord(subOrders[1].OrderId, 20m)
        };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 2));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 2));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(commissionRecords));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(300m, result.TotalOrderValue);
        Assert.Equal(30m, result.TotalCommissionAmount);
        Assert.Equal(270m, result.TotalNetAmount);
    }

    [Fact]
    public async Task GetSellerReportAsync_WithStatusFilter_ReturnsMatchingOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder(100m, SellerSubOrderStatus.Delivered) };
        var statuses = new List<SellerSubOrderStatus> { SellerSubOrderStatus.Delivered };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Statuses = statuses,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, statuses, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 1));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, statuses, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(new List<CommissionRecord>()));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Items);
        Assert.Equal(SellerSubOrderStatus.Delivered, result.Items[0].Status);
    }

    [Fact]
    public async Task GetSellerReportAsync_WithDateRangeFilter_ReturnsMatchingOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder(100m) };
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, fromDate, toDate, null, 1, 10))
            .ReturnsAsync((subOrders, 1));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, fromDate, toDate, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(new List<CommissionRecord>()));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.GetFilteredByStoreIdAsync(
            TestStoreId, null, fromDate, toDate, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetSellerReportAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = Guid.Empty,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetSellerReportAsync_InvalidPage_ReturnsFailure()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 0,
            PageSize = 10
        };

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page number must be at least 1.", result.Errors);
    }

    [Fact]
    public async Task GetSellerReportAsync_InvalidPageSize_ReturnsFailure()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 0
        };

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Page size must be between 1 and"));
    }

    [Fact]
    public async Task GetSellerReportAsync_FromDateAfterToDate_ReturnsFailure()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            FromDate = DateTimeOffset.UtcNow,
            ToDate = DateTimeOffset.UtcNow.AddDays(-7),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    [Fact]
    public async Task GetSellerReportAsync_NoOrders_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((new List<SellerSubOrder>(), 0));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((new List<SellerSubOrder>(), 0));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(new List<CommissionRecord>()));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalOrderValue);
        Assert.Equal(0, result.TotalCommissionAmount);
        Assert.Equal(0, result.TotalNetAmount);
    }

    [Fact]
    public async Task GetSellerReportAsync_NoCommissionRecords_ReturnsZeroCommission()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder>
        {
            CreateTestSellerSubOrder(100m),
            CreateTestSellerSubOrder(200m)
        };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 2));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 2));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(new List<CommissionRecord>()));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(300m, result.TotalOrderValue);
        Assert.Equal(0m, result.TotalCommissionAmount);
        Assert.Equal(300m, result.TotalNetAmount);
        Assert.All(result.Items, item => Assert.Equal(0m, item.CommissionAmount));
    }

    [Fact]
    public async Task GetSellerReportAsync_Pagination_ReturnsCorrectTotalPages()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder(100m) };
        var allSubOrders = Enumerable.Range(0, 25).Select(_ => CreateTestSellerSubOrder(100m)).ToList();
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 25));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((allSubOrders, 25));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(new List<CommissionRecord>()));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetSellerReportAsync_CommissionServiceFails_ReturnsZeroCommission()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder(100m) };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 1));

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Failure("Service unavailable"));

        // Act
        var result = await _service.GetSellerReportAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Items);
        Assert.Equal(0m, result.Items[0].CommissionAmount);
        Assert.Equal(100m, result.Items[0].NetAmount);
    }

    #endregion

    #region ExportSellerReportToCsvAsync Tests

    [Fact]
    public async Task ExportSellerReportToCsvAsync_ValidQuery_ReturnsCsvWithFinancialData()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder(100m);
        subOrder.SubOrderNumber = "ORD-12345678-S1";
        var subOrders = new List<SellerSubOrder> { subOrder };
        var commissionRecords = new List<CommissionRecord>
        {
            CreateTestCommissionRecord(subOrder.OrderId, 10m)
        };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(commissionRecords));

        // Act
        var result = await _service.ExportSellerReportToCsvAsync(TestStoreId, query);

        // Assert
        Assert.NotEmpty(result);
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Sub-Order Number", csvContent);
        Assert.Contains("Order Date", csvContent);
        Assert.Contains("Status", csvContent);
        Assert.Contains("Order Value", csvContent);
        Assert.Contains("Commission Amount", csvContent);
        Assert.Contains("Net Amount", csvContent);
        Assert.Contains("ORD-12345678-S1", csvContent);
        Assert.Contains("100.00", csvContent);
        Assert.Contains("10.00", csvContent);
        Assert.Contains("90.00", csvContent);
        Assert.Contains("TOTALS", csvContent);
    }

    [Fact]
    public async Task ExportSellerReportToCsvAsync_EmptyStoreId_ReturnsEmptyArray()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = Guid.Empty
        };

        // Act
        var result = await _service.ExportSellerReportToCsvAsync(Guid.Empty, query);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportSellerReportToCsvAsync_NoOrders_ReturnsEmptyArray()
    {
        // Arrange
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((new List<SellerSubOrder>(), 0));

        // Act
        var result = await _service.ExportSellerReportToCsvAsync(TestStoreId, query);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportSellerReportToCsvAsync_WithFilters_AppliesFilters()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder(100m, SellerSubOrderStatus.Delivered);
        var subOrders = new List<SellerSubOrder> { subOrder };
        var statuses = new List<SellerSubOrderStatus> { SellerSubOrderStatus.Delivered };
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId,
            Statuses = statuses,
            FromDate = fromDate,
            ToDate = toDate
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, statuses, fromDate, toDate, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(new List<CommissionRecord>()));

        // Act
        var result = await _service.ExportSellerReportToCsvAsync(TestStoreId, query);

        // Assert
        Assert.NotEmpty(result);
        _mockSellerSubOrderRepository.Verify(r => r.GetFilteredByStoreIdAsync(
            TestStoreId, statuses, fromDate, toDate, null, 1, 10000), Times.Once);
    }

    [Fact]
    public async Task ExportSellerReportToCsvAsync_IncludesSummaryTotals()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder>
        {
            CreateTestSellerSubOrder(100m),
            CreateTestSellerSubOrder(200m)
        };
        var commissionRecords = new List<CommissionRecord>
        {
            CreateTestCommissionRecord(subOrders[0].OrderId, 10m),
            CreateTestCommissionRecord(subOrders[1].OrderId, 20m)
        };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 2));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(commissionRecords));

        // Act
        var result = await _service.ExportSellerReportToCsvAsync(TestStoreId, query);

        // Assert
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("TOTALS", csvContent);
        Assert.Contains("300.00", csvContent); // Total order value
        Assert.Contains("30.00", csvContent);  // Total commission
        Assert.Contains("270.00", csvContent); // Total net amount
    }

    [Fact]
    public async Task ExportSellerReportToCsvAsync_MultipleOrders_CalculatesCorrectTotals()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder>
        {
            CreateTestSellerSubOrder(150m),
            CreateTestSellerSubOrder(250m),
            CreateTestSellerSubOrder(100m)
        };
        var commissionRecords = new List<CommissionRecord>
        {
            CreateTestCommissionRecord(subOrders[0].OrderId, 15m),
            CreateTestCommissionRecord(subOrders[1].OrderId, 25m),
            CreateTestCommissionRecord(subOrders[2].OrderId, 10m)
        };
        var query = new SellerReportFilterQuery
        {
            StoreId = TestStoreId
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 3));

        _mockCommissionService.Setup(c => c.GetCommissionRecordsBySellerIdAsync(TestStoreId))
            .ReturnsAsync(GetCommissionRecordsResult.Success(commissionRecords));

        // Act
        var result = await _service.ExportSellerReportToCsvAsync(TestStoreId, query);

        // Assert
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("500.00", csvContent); // Total: 150 + 250 + 100
        Assert.Contains("50.00", csvContent);  // Commission: 15 + 25 + 10
        Assert.Contains("450.00", csvContent); // Net: 500 - 50
    }

    #endregion

    #region Helper Methods

    private SellerSubOrder CreateTestSellerSubOrder(decimal totalAmount, SellerSubOrderStatus status = SellerSubOrderStatus.Paid)
    {
        var orderId = Guid.NewGuid();
        return new SellerSubOrder
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            StoreId = TestStoreId,
            StoreName = "Test Store",
            SubOrderNumber = $"ORD-{orderId.ToString("N")[..8].ToUpperInvariant()}-S1",
            Status = status,
            ItemsSubtotal = totalAmount - 5m,
            ShippingCost = 5m,
            TotalAmount = totalAmount,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<SellerSubOrderItem>()
        };
    }

    private static CommissionRecord CreateTestCommissionRecord(Guid orderId, decimal commissionAmount)
    {
        return new CommissionRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = TestStoreId,
            OrderAmount = 100m,
            CommissionRate = 0.10m,
            CommissionAmount = commissionAmount,
            NetCommissionAmount = commissionAmount,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            CalculatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
