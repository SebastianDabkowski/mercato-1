using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class AuditLogServiceTests
{
    [Fact]
    public async Task GetAuditLogsAsync_WithNoFilters_ReturnsAllLogs()
    {
        // Arrange
        var auditLogs = new List<AdminAuditLog>
        {
            new() { Id = Guid.NewGuid(), AdminUserId = "admin1", Action = "StatusChange", EntityType = "User", EntityId = "user1", Timestamp = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), AdminUserId = "admin2", Action = "Approve", EntityType = "Product", EntityId = "prod1", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5) }
        };

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithDateRangeFilter_PassesDatesToRepository()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                startDate,
                endDate,
                null,
                null,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminAuditLog>());

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync(startDate, endDate);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                startDate,
                endDate,
                null,
                null,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithAdminUserFilter_PassesAdminUserToRepository()
    {
        // Arrange
        var adminUserId = "admin@example.com";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                null,
                null,
                adminUserId,
                null,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminAuditLog>());

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync(adminUserId: adminUserId);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                null,
                null,
                adminUserId,
                null,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithEntityTypeFilter_PassesEntityTypeToRepository()
    {
        // Arrange
        var entityType = "User";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                null,
                null,
                null,
                entityType,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminAuditLog>());

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync(entityType: entityType);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                null,
                null,
                null,
                entityType,
                null,
                null,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithActionFilter_PassesActionToRepository()
    {
        // Arrange
        var action = "StatusChange";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                null,
                null,
                null,
                null,
                action,
                null,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminAuditLog>());

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync(action: action);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                null,
                null,
                null,
                null,
                action,
                null,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithEntityIdFilter_PassesEntityIdToRepository()
    {
        // Arrange
        var entityId = "user-123";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                null,
                null,
                null,
                null,
                null,
                entityId,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminAuditLog>());

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync(entityId: entityId);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                null,
                null,
                null,
                null,
                null,
                entityId,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithAllFilters_PassesAllFiltersToRepository()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var adminUserId = "admin@example.com";
        var entityType = "User";
        var action = "StatusChange";
        var entityId = "user-123";
        var maxResults = 50;

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                startDate,
                endDate,
                adminUserId,
                entityType,
                action,
                entityId,
                maxResults,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminAuditLog>());

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsAsync(
            startDate,
            endDate,
            adminUserId,
            entityType,
            action,
            entityId,
            maxResults);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                startDate,
                endDate,
                adminUserId,
                entityType,
                action,
                entityId,
                maxResults,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsByResourceAsync_ReturnsLogsForResource()
    {
        // Arrange
        var entityType = "User";
        var entityId = "user-123";
        var auditLogs = new List<AdminAuditLog>
        {
            new() { Id = Guid.NewGuid(), AdminUserId = "admin1", Action = "Create", EntityType = entityType, EntityId = entityId, Timestamp = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), AdminUserId = "admin2", Action = "StatusChange", EntityType = entityType, EntityId = entityId, Timestamp = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), AdminUserId = "admin1", Action = "Suspend", EntityType = entityType, EntityId = entityId, Timestamp = DateTimeOffset.UtcNow }
        };

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByEntityAsync(entityType, entityId))
            .ReturnsAsync(auditLogs);

        var mockLogger = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetAuditLogsByResourceAsync(entityType, entityId);

        // Assert
        Assert.Equal(3, result.Count);
        mockRepository.Verify(x => x.GetByEntityAsync(entityType, entityId), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AuditLogService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AuditLogService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IAdminAuditRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AuditLogService(mockRepository.Object, null!));
    }
}
