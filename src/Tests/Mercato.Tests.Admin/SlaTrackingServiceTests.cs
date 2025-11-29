using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class SlaTrackingServiceTests
{
    private readonly Mock<ISlaTrackingRepository> _mockTrackingRepository;
    private readonly Mock<ISlaConfigurationRepository> _mockConfigurationRepository;
    private readonly Mock<ILogger<SlaTrackingService>> _mockLogger;
    private readonly SlaTrackingService _service;

    public SlaTrackingServiceTests()
    {
        _mockTrackingRepository = new Mock<ISlaTrackingRepository>(MockBehavior.Strict);
        _mockConfigurationRepository = new Mock<ISlaConfigurationRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<SlaTrackingService>>();
        _service = new SlaTrackingService(
            _mockTrackingRepository.Object,
            _mockConfigurationRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullTrackingRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SlaTrackingService(null!, _mockConfigurationRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullConfigurationRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SlaTrackingService(_mockTrackingRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SlaTrackingService(_mockTrackingRepository.Object, _mockConfigurationRepository.Object, null!));
    }

    [Fact]
    public async Task CreateTrackingRecordAsync_WithConfiguration_UsesConfiguredDeadlines()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var config = new SlaConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            FirstResponseDeadlineHours = 12,
            ResolutionDeadlineHours = 48
        };

        _mockConfigurationRepository.Setup(x => x.GetApplicableConfigurationAsync("Return", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockTrackingRepository.Setup(x => x.AddAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaTrackingRecord r, CancellationToken _) => r);

        // Act
        var result = await _service.CreateTrackingRecordAsync(
            caseId,
            "CASE-12345",
            "Return",
            storeId,
            "Test Store",
            createdAt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.CaseId);
        Assert.Equal("CASE-12345", result.CaseNumber);
        Assert.Equal(storeId, result.StoreId);
        Assert.Equal("Test Store", result.StoreName);
        Assert.Equal(createdAt.AddHours(12), result.FirstResponseDeadline);
        Assert.Equal(createdAt.AddHours(48), result.ResolutionDeadline);
        Assert.Equal(SlaStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateTrackingRecordAsync_WithoutConfiguration_UsesDefaultDeadlines()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        _mockConfigurationRepository.Setup(x => x.GetApplicableConfigurationAsync("Return", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaConfiguration?)null);

        _mockTrackingRepository.Setup(x => x.AddAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaTrackingRecord r, CancellationToken _) => r);

        // Act
        var result = await _service.CreateTrackingRecordAsync(
            caseId,
            "CASE-12345",
            "Return",
            storeId,
            "Test Store",
            createdAt);

        // Assert
        Assert.Equal(createdAt.AddHours(24), result.FirstResponseDeadline);  // Default 24 hours
        Assert.Equal(createdAt.AddHours(72), result.ResolutionDeadline);     // Default 72 hours
    }

    [Fact]
    public async Task RecordFirstResponseAsync_BeforeDeadline_SetsRespondedStatus()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-12);
        var respondedAt = DateTimeOffset.UtcNow;
        var record = new SlaTrackingRecord
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            CaseNumber = "CASE-12345",
            CaseCreatedAt = createdAt,
            FirstResponseDeadline = createdAt.AddHours(24),
            Status = SlaStatus.Pending
        };

        _mockTrackingRepository.Setup(x => x.GetByCaseIdAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _mockTrackingRepository.Setup(x => x.UpdateAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordFirstResponseAsync(caseId, respondedAt);

        // Assert
        Assert.Equal(respondedAt, record.FirstResponseAt);
        Assert.False(record.IsFirstResponseBreached);
        Assert.Equal(SlaStatus.Responded, record.Status);
    }

    [Fact]
    public async Task RecordFirstResponseAsync_AfterDeadline_SetsBreachedStatus()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-48);
        var respondedAt = DateTimeOffset.UtcNow;
        var record = new SlaTrackingRecord
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            CaseNumber = "CASE-12345",
            CaseCreatedAt = createdAt,
            FirstResponseDeadline = createdAt.AddHours(24), // Deadline was 24 hours ago
            Status = SlaStatus.Pending
        };

        _mockTrackingRepository.Setup(x => x.GetByCaseIdAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _mockTrackingRepository.Setup(x => x.UpdateAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordFirstResponseAsync(caseId, respondedAt);

        // Assert
        Assert.True(record.IsFirstResponseBreached);
        Assert.Equal(SlaStatus.FirstResponseBreached, record.Status);
    }

    [Fact]
    public async Task RecordFirstResponseAsync_WhenAlreadyResponded_DoesNotUpdate()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var existingResponseTime = DateTimeOffset.UtcNow.AddHours(-6);
        var record = new SlaTrackingRecord
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            FirstResponseAt = existingResponseTime, // Already responded
            Status = SlaStatus.Responded
        };

        _mockTrackingRepository.Setup(x => x.GetByCaseIdAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act
        await _service.RecordFirstResponseAsync(caseId, DateTimeOffset.UtcNow);

        // Assert - UpdateAsync should not be called
        _mockTrackingRepository.Verify(x => x.UpdateAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(existingResponseTime, record.FirstResponseAt);
    }

    [Fact]
    public async Task RecordResolutionAsync_BeforeDeadline_SetsResolvedWithinSlaStatus()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-24);
        var resolvedAt = DateTimeOffset.UtcNow;
        var record = new SlaTrackingRecord
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            CaseNumber = "CASE-12345",
            CaseCreatedAt = createdAt,
            ResolutionDeadline = createdAt.AddHours(72), // 48 hours remaining
            FirstResponseAt = createdAt.AddHours(12),
            IsFirstResponseBreached = false,
            Status = SlaStatus.Responded
        };

        _mockTrackingRepository.Setup(x => x.GetByCaseIdAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _mockTrackingRepository.Setup(x => x.UpdateAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordResolutionAsync(caseId, resolvedAt);

        // Assert
        Assert.Equal(resolvedAt, record.ResolvedAt);
        Assert.False(record.IsResolutionBreached);
        Assert.Equal(SlaStatus.ResolvedWithinSla, record.Status);
    }

    [Fact]
    public async Task RecordResolutionAsync_AfterDeadline_SetsClosedStatus()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-100);
        var resolvedAt = DateTimeOffset.UtcNow;
        var record = new SlaTrackingRecord
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            CaseNumber = "CASE-12345",
            CaseCreatedAt = createdAt,
            ResolutionDeadline = createdAt.AddHours(72), // Deadline was 28 hours ago
            FirstResponseAt = createdAt.AddHours(12),
            IsFirstResponseBreached = false,
            Status = SlaStatus.Responded
        };

        _mockTrackingRepository.Setup(x => x.GetByCaseIdAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _mockTrackingRepository.Setup(x => x.UpdateAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordResolutionAsync(caseId, resolvedAt);

        // Assert
        Assert.True(record.IsResolutionBreached);
        Assert.Equal(SlaStatus.Closed, record.Status);
    }

    [Fact]
    public async Task CheckAndUpdateBreachesAsync_UpdatesPendingCasesPastDeadline()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pendingRecords = new List<SlaTrackingRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CaseNumber = "CASE-001",
                CaseCreatedAt = now.AddHours(-30),
                FirstResponseDeadline = now.AddHours(-6), // Breached
                ResolutionDeadline = now.AddHours(42),
                Status = SlaStatus.Pending,
                IsFirstResponseBreached = false
            }
        };

        _mockTrackingRepository.Setup(x => x.GetByDateRangeAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), null, SlaStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRecords);

        _mockTrackingRepository.Setup(x => x.GetByDateRangeAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), null, SlaStatus.Responded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaTrackingRecord>());

        _mockTrackingRepository.Setup(x => x.UpdateAsync(It.IsAny<SlaTrackingRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var breachCount = await _service.CheckAndUpdateBreachesAsync();

        // Assert
        Assert.Equal(1, breachCount);
        Assert.True(pendingRecords[0].IsFirstResponseBreached);
        Assert.Equal(SlaStatus.FirstResponseBreached, pendingRecords[0].Status);
    }

    [Fact]
    public async Task GetDashboardStatisticsAsync_ReturnsCorrectAggregates()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var records = new List<SlaTrackingRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CaseCreatedAt = startDate.AddDays(1),
                FirstResponseAt = startDate.AddDays(1).AddHours(12),
                ResolvedAt = startDate.AddDays(2),
                Status = SlaStatus.ResolvedWithinSla,
                IsFirstResponseBreached = false,
                IsResolutionBreached = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                CaseCreatedAt = startDate.AddDays(3),
                FirstResponseAt = startDate.AddDays(4),
                Status = SlaStatus.Responded,
                IsFirstResponseBreached = true,
                IsResolutionBreached = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                CaseCreatedAt = startDate.AddDays(5),
                Status = SlaStatus.Pending,
                IsFirstResponseBreached = false,
                IsResolutionBreached = false
            }
        };

        _mockTrackingRepository.Setup(x => x.GetByDateRangeAsync(startDate, endDate, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        _mockTrackingRepository.Setup(x => x.GetBreachedCasesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaTrackingRecord> { records[1] });

        // Act
        var result = await _service.GetDashboardStatisticsAsync(startDate, endDate);

        // Assert
        Assert.Equal(3, result.TotalCases);
        Assert.Equal(2, result.OpenCases); // Pending + Responded
        Assert.Equal(1, result.CasesResolvedWithinSla);
        Assert.Equal(1, result.CurrentlyBreachedCases);
        Assert.Equal(1, result.TotalFirstResponseBreaches);
    }

    [Fact]
    public async Task GetBreachedCasesAsync_ReturnsBreachedCases()
    {
        // Arrange
        var breachedCases = new List<SlaTrackingRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CaseNumber = "CASE-001",
                IsFirstResponseBreached = true,
                Status = SlaStatus.FirstResponseBreached
            }
        };

        _mockTrackingRepository.Setup(x => x.GetBreachedCasesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(breachedCases);

        // Act
        var result = await _service.GetBreachedCasesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("CASE-001", result[0].CaseNumber);
    }

    [Fact]
    public async Task SaveSlaConfigurationAsync_NewConfiguration_AddsWithCreatedInfo()
    {
        // Arrange
        var config = new SlaConfiguration
        {
            Id = Guid.Empty, // New configuration
            Name = "Test Config",
            FirstResponseDeadlineHours = 12,
            ResolutionDeadlineHours = 48
        };

        _mockConfigurationRepository.Setup(x => x.AddAsync(It.IsAny<SlaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaConfiguration c, CancellationToken _) => c);

        // Act
        var result = await _service.SaveSlaConfigurationAsync(config, "admin-user-id");

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("admin-user-id", result.CreatedByUserId);
        Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task SaveSlaConfigurationAsync_ExistingConfiguration_UpdatesWithUpdatedInfo()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var config = new SlaConfiguration
        {
            Id = existingId,
            Name = "Updated Config",
            FirstResponseDeadlineHours = 24,
            ResolutionDeadlineHours = 72,
            CreatedByUserId = "original-admin"
        };

        _mockConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<SlaConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SaveSlaConfigurationAsync(config, "updating-admin");

        // Assert
        Assert.Equal(existingId, result.Id);
        Assert.Equal("updating-admin", result.UpdatedByUserId);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task DeleteSlaConfigurationAsync_CallsRepository()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockConfigurationRepository.Setup(x => x.DeleteAsync(configId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteSlaConfigurationAsync(configId);

        // Assert
        _mockConfigurationRepository.Verify(x => x.DeleteAsync(configId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSellerStatisticsAsync_ReturnsStoreStatistics()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;
        var storeStats = new List<SlaStoreStatistics>
        {
            new()
            {
                StoreId = Guid.NewGuid(),
                StoreName = "Store A",
                TotalCases = 10,
                CasesResolvedWithinSla = 8
            },
            new()
            {
                StoreId = Guid.NewGuid(),
                StoreName = "Store B",
                TotalCases = 5,
                CasesResolvedWithinSla = 5
            }
        };

        _mockTrackingRepository.Setup(x => x.GetAllStoreStatisticsAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storeStats);

        // Act
        var result = await _service.GetSellerStatisticsAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Store A", result[0].StoreName);
        Assert.Equal(80, result[0].SlaCompliancePercentage);
    }
}
