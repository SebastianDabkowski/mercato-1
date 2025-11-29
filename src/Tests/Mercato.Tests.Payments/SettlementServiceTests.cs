using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Payments;

public class SettlementServiceTests
{
    private readonly Mock<ISettlementRepository> _mockSettlementRepository;
    private readonly Mock<ICommissionRecordRepository> _mockCommissionRecordRepository;
    private readonly Mock<IEscrowRepository> _mockEscrowRepository;
    private readonly Mock<ILogger<SettlementService>> _mockLogger;

    public SettlementServiceTests()
    {
        _mockSettlementRepository = new Mock<ISettlementRepository>(MockBehavior.Strict);
        _mockCommissionRecordRepository = new Mock<ICommissionRecordRepository>(MockBehavior.Strict);
        _mockEscrowRepository = new Mock<IEscrowRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<SettlementService>>();
    }

    private SettlementService CreateService(SettlementSettings? settings = null)
    {
        var settlementSettings = settings ?? new SettlementSettings();
        var options = Options.Create(settlementSettings);
        return new SettlementService(
            _mockSettlementRepository.Object,
            _mockCommissionRecordRepository.Object,
            _mockEscrowRepository.Object,
            _mockLogger.Object,
            options);
    }

    #region GenerateSettlementAsync Tests

    [Fact]
    public async Task GenerateSettlementAsync_ValidData_CreatesSettlement()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var year = 2024;
        var month = 1;

        var commissionRecords = new List<CommissionRecord>
        {
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                OrderId = Guid.NewGuid(),
                OrderAmount = 100.00m,
                CommissionRate = 0.10m,
                CommissionAmount = 10.00m,
                RefundedAmount = 0m,
                RefundedCommissionAmount = 0m,
                NetCommissionAmount = 10.00m,
                CalculatedAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero)
            },
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                OrderId = Guid.NewGuid(),
                OrderAmount = 200.00m,
                CommissionRate = 0.10m,
                CommissionAmount = 20.00m,
                RefundedAmount = 0m,
                RefundedCommissionAmount = 0m,
                NetCommissionAmount = 20.00m,
                CalculatedAt = new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero)
            }
        };

        var escrowEntries = new List<EscrowEntry>();

        _mockSettlementRepository
            .Setup(r => r.GetBySellerAndPeriodAsync(sellerId, year, month))
            .ReturnsAsync((Settlement?)null);

        _mockCommissionRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(commissionRecords);

        _mockEscrowRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(escrowEntries);

        _mockSettlementRepository
            .Setup(r => r.AddAsync(It.IsAny<Settlement>()))
            .ReturnsAsync((Settlement s) => s);

        _mockSettlementRepository
            .Setup(r => r.AddLineItemAsync(It.IsAny<SettlementLineItem>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateSettlementCommand
        {
            SellerId = sellerId,
            Year = year,
            Month = month,
            AuditNote = "Test generation"
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Settlement);
        Assert.Equal(sellerId, result.Settlement.SellerId);
        Assert.Equal(year, result.Settlement.Year);
        Assert.Equal(month, result.Settlement.Month);
        Assert.Equal(300.00m, result.Settlement.GrossSales);
        Assert.Equal(0m, result.Settlement.TotalRefunds);
        Assert.Equal(300.00m, result.Settlement.NetSales);
        Assert.Equal(30.00m, result.Settlement.TotalCommission);
        Assert.Equal(2, result.Settlement.OrderCount);
        Assert.Equal(SettlementStatus.Draft, result.Settlement.Status);
    }

    [Fact]
    public async Task GenerateSettlementAsync_NoOrders_ReturnsEmptySettlement()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var year = 2024;
        var month = 1;

        _mockSettlementRepository
            .Setup(r => r.GetBySellerAndPeriodAsync(sellerId, year, month))
            .ReturnsAsync((Settlement?)null);

        _mockCommissionRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(new List<CommissionRecord>());

        _mockEscrowRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(new List<EscrowEntry>());

        _mockSettlementRepository
            .Setup(r => r.AddAsync(It.IsAny<Settlement>()))
            .ReturnsAsync((Settlement s) => s);

        var command = new GenerateSettlementCommand
        {
            SellerId = sellerId,
            Year = year,
            Month = month
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Settlement);
        Assert.Equal(0m, result.Settlement.GrossSales);
        Assert.Equal(0m, result.Settlement.NetPayable);
        Assert.Equal(0, result.Settlement.OrderCount);
    }

    [Fact]
    public async Task GenerateSettlementAsync_SettlementAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var year = 2024;
        var month = 1;

        var existingSettlement = new Settlement
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Year = year,
            Month = month
        };

        _mockSettlementRepository
            .Setup(r => r.GetBySellerAndPeriodAsync(sellerId, year, month))
            .ReturnsAsync(existingSettlement);

        var command = new GenerateSettlementCommand
        {
            SellerId = sellerId,
            Year = year,
            Month = month
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task GenerateSettlementAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new GenerateSettlementCommand
        {
            SellerId = Guid.Empty,
            Year = 2024,
            Month = 1
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task GenerateSettlementAsync_InvalidMonth_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new GenerateSettlementCommand
        {
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 13
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Month must be between 1 and 12"));
    }

    [Fact]
    public async Task GenerateSettlementAsync_InvalidYear_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new GenerateSettlementCommand
        {
            SellerId = Guid.NewGuid(),
            Year = 1900,
            Month = 1
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Year must be between 2000 and 2100"));
    }

    #endregion

    #region RegenerateSettlementAsync Tests

    [Fact]
    public async Task RegenerateSettlementAsync_DraftSettlement_IncrementsVersionAndAddsAuditNotes()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingSettlement = new Settlement
        {
            Id = settlementId,
            SellerId = sellerId,
            Year = 2024,
            Month = 1,
            Status = SettlementStatus.Draft,
            Version = 1,
            AuditNotes = "Initial generation",
            LineItems = new List<SettlementLineItem>()
        };

        var commissionRecords = new List<CommissionRecord>
        {
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                OrderId = Guid.NewGuid(),
                OrderAmount = 150.00m,
                CommissionRate = 0.10m,
                CommissionAmount = 15.00m,
                RefundedAmount = 0m,
                RefundedCommissionAmount = 0m,
                NetCommissionAmount = 15.00m,
                CalculatedAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero)
            }
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync(existingSettlement);

        _mockSettlementRepository
            .Setup(r => r.DeleteLineItemsAsync(settlementId))
            .Returns(Task.CompletedTask);

        _mockCommissionRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(commissionRecords);

        _mockSettlementRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Settlement>()))
            .Returns(Task.CompletedTask);

        _mockSettlementRepository
            .Setup(r => r.AddLineItemAsync(It.IsAny<SettlementLineItem>()))
            .Returns(Task.CompletedTask);

        var command = new RegenerateSettlementCommand
        {
            SettlementId = settlementId,
            Reason = "Updated commission records"
        };

        // Act
        var result = await service.RegenerateSettlementAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Settlement);
        Assert.Equal(2, result.Settlement.Version);
        Assert.Equal(1, result.PreviousVersion);
        Assert.Contains("Updated commission records", result.Settlement.AuditNotes ?? string.Empty);
        Assert.NotNull(result.Settlement.RegeneratedAt);
    }

    [Fact]
    public async Task RegenerateSettlementAsync_FinalizedSettlement_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        var existingSettlement = new Settlement
        {
            Id = settlementId,
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 1,
            Status = SettlementStatus.Finalized,
            Version = 1,
            LineItems = new List<SettlementLineItem>()
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync(existingSettlement);

        var command = new RegenerateSettlementCommand
        {
            SettlementId = settlementId
        };

        // Act
        var result = await service.RegenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Cannot regenerate"));
    }

    [Fact]
    public async Task RegenerateSettlementAsync_SettlementNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync((Settlement?)null);

        var command = new RegenerateSettlementCommand
        {
            SettlementId = settlementId
        };

        // Act
        var result = await service.RegenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement not found"));
    }

    [Fact]
    public async Task RegenerateSettlementAsync_EmptySettlementId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new RegenerateSettlementCommand
        {
            SettlementId = Guid.Empty
        };

        // Act
        var result = await service.RegenerateSettlementAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement ID is required"));
    }

    #endregion

    #region FinalizeSettlementAsync Tests

    [Fact]
    public async Task FinalizeSettlementAsync_DraftSettlement_ChangesStatusCorrectly()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        var settlement = new Settlement
        {
            Id = settlementId,
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 1,
            Status = SettlementStatus.Draft
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdAsync(settlementId))
            .ReturnsAsync(settlement);

        _mockSettlementRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Settlement>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.FinalizeSettlementAsync(settlementId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Settlement);
        Assert.Equal(SettlementStatus.Finalized, result.Settlement.Status);
        Assert.NotNull(result.Settlement.FinalizedAt);
    }

    [Fact]
    public async Task FinalizeSettlementAsync_AlreadyFinalized_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        var settlement = new Settlement
        {
            Id = settlementId,
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 1,
            Status = SettlementStatus.Finalized
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdAsync(settlementId))
            .ReturnsAsync(settlement);

        // Act
        var result = await service.FinalizeSettlementAsync(settlementId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already finalized"));
    }

    [Fact]
    public async Task FinalizeSettlementAsync_SettlementNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        _mockSettlementRepository
            .Setup(r => r.GetByIdAsync(settlementId))
            .ReturnsAsync((Settlement?)null);

        // Act
        var result = await service.FinalizeSettlementAsync(settlementId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement not found"));
    }

    [Fact]
    public async Task FinalizeSettlementAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.FinalizeSettlementAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement ID is required"));
    }

    #endregion

    #region ExportSettlementAsync Tests

    [Fact]
    public async Task ExportSettlementAsync_ValidSettlement_GeneratesValidCsvData()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var settlement = new Settlement
        {
            Id = settlementId,
            SellerId = sellerId,
            Year = 2024,
            Month = 1,
            Currency = "USD",
            GrossSales = 300.00m,
            TotalRefunds = 50.00m,
            NetSales = 250.00m,
            TotalCommission = 25.00m,
            PreviousMonthAdjustments = 0m,
            NetPayable = 225.00m,
            OrderCount = 2,
            Status = SettlementStatus.Draft,
            GeneratedAt = DateTimeOffset.UtcNow,
            Version = 1,
            LineItems = new List<SettlementLineItem>
            {
                new SettlementLineItem
                {
                    Id = Guid.NewGuid(),
                    SettlementId = settlementId,
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-12345678",
                    OrderDate = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
                    GrossAmount = 100.00m,
                    RefundAmount = 0m,
                    NetAmount = 100.00m,
                    CommissionAmount = 10.00m,
                    IsAdjustment = false
                },
                new SettlementLineItem
                {
                    Id = Guid.NewGuid(),
                    SettlementId = settlementId,
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-87654321",
                    OrderDate = new DateTimeOffset(2024, 1, 20, 14, 0, 0, TimeSpan.Zero),
                    GrossAmount = 200.00m,
                    RefundAmount = 50.00m,
                    NetAmount = 150.00m,
                    CommissionAmount = 15.00m,
                    IsAdjustment = false
                }
            }
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync(settlement);

        _mockSettlementRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Settlement>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ExportSettlementAsync(settlementId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.CsvData);
        Assert.NotNull(result.FileName);
        Assert.Contains("Settlement Report", result.CsvData);
        Assert.Contains("300.00", result.CsvData); // Gross Sales
        Assert.Contains("225.00", result.CsvData); // Net Payable
        Assert.Contains("ORD-12345678", result.CsvData);
        Assert.Contains("ORD-87654321", result.CsvData);
        Assert.Contains(".csv", result.FileName);
    }

    [Fact]
    public async Task ExportSettlementAsync_SettlementNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync((Settlement?)null);

        // Act
        var result = await service.ExportSettlementAsync(settlementId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement not found"));
    }

    [Fact]
    public async Task ExportSettlementAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ExportSettlementAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement ID is required"));
    }

    [Fact]
    public async Task ExportSettlementAsync_UpdatesStatusToExported()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        var settlement = new Settlement
        {
            Id = settlementId,
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 1,
            Status = SettlementStatus.Finalized,
            GeneratedAt = DateTimeOffset.UtcNow,
            Version = 1,
            LineItems = new List<SettlementLineItem>()
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync(settlement);

        _mockSettlementRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Settlement>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ExportSettlementAsync(settlementId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(SettlementStatus.Exported, result.Settlement!.Status);
        Assert.NotNull(result.Settlement.ExportedAt);
    }

    #endregion

    #region GetSettlementAsync Tests

    [Fact]
    public async Task GetSettlementAsync_ValidId_ReturnsSettlementWithLineItems()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        var settlement = new Settlement
        {
            Id = settlementId,
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 1,
            Status = SettlementStatus.Draft,
            LineItems = new List<SettlementLineItem>
            {
                new SettlementLineItem
                {
                    Id = Guid.NewGuid(),
                    SettlementId = settlementId,
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-12345678"
                }
            }
        };

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync(settlement);

        // Act
        var result = await service.GetSettlementAsync(settlementId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Settlement);
        Assert.Equal(settlementId, result.Settlement.Id);
        Assert.Single(result.Settlement.LineItems);
    }

    [Fact]
    public async Task GetSettlementAsync_SettlementNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var settlementId = Guid.NewGuid();

        _mockSettlementRepository
            .Setup(r => r.GetByIdWithLineItemsAsync(settlementId))
            .ReturnsAsync((Settlement?)null);

        // Act
        var result = await service.GetSettlementAsync(settlementId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement not found"));
    }

    [Fact]
    public async Task GetSettlementAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetSettlementAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Settlement ID is required"));
    }

    #endregion

    #region GetSettlementsAsync Tests

    [Fact]
    public async Task GetSettlementsAsync_NoFilters_ReturnsAllSettlements()
    {
        // Arrange
        var service = CreateService();

        var settlements = new List<Settlement>
        {
            new Settlement { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Year = 2024, Month = 1 },
            new Settlement { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Year = 2024, Month = 2 }
        };

        _mockSettlementRepository
            .Setup(r => r.GetFilteredAsync(null, null, null, null))
            .ReturnsAsync(settlements);

        var query = new GetSettlementsQuery();

        // Act
        var result = await service.GetSettlementsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Settlements.Count);
    }

    [Fact]
    public async Task GetSettlementsAsync_FiltersBySeller_ReturnsFilteredSettlements()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var settlements = new List<Settlement>
        {
            new Settlement { Id = Guid.NewGuid(), SellerId = sellerId, Year = 2024, Month = 1 }
        };

        _mockSettlementRepository
            .Setup(r => r.GetFilteredAsync(sellerId, null, null, null))
            .ReturnsAsync(settlements);

        var query = new GetSettlementsQuery { SellerId = sellerId };

        // Act
        var result = await service.GetSettlementsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Settlements);
        Assert.Equal(sellerId, result.Settlements[0].SellerId);
    }

    [Fact]
    public async Task GetSettlementsAsync_FiltersByYearAndMonth_ReturnsFilteredSettlements()
    {
        // Arrange
        var service = CreateService();
        var year = 2024;
        var month = 1;

        var settlements = new List<Settlement>
        {
            new Settlement { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Year = year, Month = month }
        };

        _mockSettlementRepository
            .Setup(r => r.GetFilteredAsync(null, year, month, null))
            .ReturnsAsync(settlements);

        var query = new GetSettlementsQuery { Year = year, Month = month };

        // Act
        var result = await service.GetSettlementsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Settlements);
        Assert.Equal(year, result.Settlements[0].Year);
        Assert.Equal(month, result.Settlements[0].Month);
    }

    [Fact]
    public async Task GetSettlementsAsync_FiltersByStatus_ReturnsFilteredSettlements()
    {
        // Arrange
        var service = CreateService();
        var status = SettlementStatus.Finalized;

        var settlements = new List<Settlement>
        {
            new Settlement { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Year = 2024, Month = 1, Status = status }
        };

        _mockSettlementRepository
            .Setup(r => r.GetFilteredAsync(null, null, null, status))
            .ReturnsAsync(settlements);

        var query = new GetSettlementsQuery { Status = status };

        // Act
        var result = await service.GetSettlementsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Settlements);
        Assert.Equal(status, result.Settlements[0].Status);
    }

    [Fact]
    public async Task GetSettlementsAsync_InvalidMonth_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var query = new GetSettlementsQuery { Month = 15 };

        // Act
        var result = await service.GetSettlementsAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Month must be between 1 and 12"));
    }

    [Fact]
    public async Task GetSettlementsAsync_InvalidYear_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var query = new GetSettlementsQuery { Year = 1800 };

        // Act
        var result = await service.GetSettlementsAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Year must be between 2000 and 2100"));
    }

    #endregion

    #region Adjustments Tests

    [Fact]
    public async Task GenerateSettlementAsync_WithPreviousMonthAdjustments_MarksAdjustmentsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var year = 2024;
        var month = 2; // February

        var currentMonthRecord = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = Guid.NewGuid(),
            OrderAmount = 100.00m,
            CommissionRate = 0.10m,
            CommissionAmount = 10.00m,
            RefundedAmount = 0m,
            RefundedCommissionAmount = 0m,
            NetCommissionAmount = 10.00m,
            CalculatedAt = new DateTimeOffset(2024, 2, 15, 0, 0, 0, TimeSpan.Zero)
        };

        // A refund from January that was processed in February
        var previousMonthRefund = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = Guid.NewGuid(),
            OrderAmount = 200.00m,
            CommissionRate = 0.10m,
            CommissionAmount = 20.00m,
            RefundedAmount = 50.00m,
            RefundedCommissionAmount = 5.00m,
            NetCommissionAmount = 15.00m,
            CalculatedAt = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero), // Order from January
            LastRefundRecalculatedAt = new DateTimeOffset(2024, 2, 5, 0, 0, 0, TimeSpan.Zero) // Refund processed in February
        };

        var commissionRecords = new List<CommissionRecord> { currentMonthRecord, previousMonthRefund };

        _mockSettlementRepository
            .Setup(r => r.GetBySellerAndPeriodAsync(sellerId, year, month))
            .ReturnsAsync((Settlement?)null);

        _mockCommissionRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(commissionRecords);

        _mockEscrowRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(new List<EscrowEntry>());

        _mockSettlementRepository
            .Setup(r => r.AddAsync(It.IsAny<Settlement>()))
            .ReturnsAsync((Settlement s) => s);

        _mockSettlementRepository
            .Setup(r => r.AddLineItemAsync(It.IsAny<SettlementLineItem>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateSettlementCommand
        {
            SellerId = sellerId,
            Year = year,
            Month = month
        };

        // Act
        var result = await service.GenerateSettlementAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Settlement);
        Assert.NotEqual(0m, result.Settlement.PreviousMonthAdjustments);
    }

    #endregion
}
