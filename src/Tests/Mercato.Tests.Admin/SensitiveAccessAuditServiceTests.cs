using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class SensitiveAccessAuditServiceTests
{
    [Fact]
    public async Task LogCustomerProfileAccessAsync_WithValidParameters_CreatesAuditLog()
    {
        // Arrange
        var adminUserId = "admin-123";
        var customerId = "customer-456";
        var ipAddress = "192.168.1.1";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.Is<AdminAuditLog>(log =>
            log.AdminUserId == adminUserId &&
            log.Action == "ViewCustomerProfile" &&
            log.EntityType == "Customer" &&
            log.EntityId == customerId &&
            log.IpAddress == ipAddress &&
            log.Details!.Contains(customerId))))
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogCustomerProfileAccessAsync(adminUserId, customerId, ipAddress);

        // Assert
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogCustomerProfileAccessAsync_WithoutIpAddress_CreatesAuditLogWithNullIpAddress()
    {
        // Arrange
        var adminUserId = "admin-123";
        var customerId = "customer-456";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.Is<AdminAuditLog>(log =>
            log.AdminUserId == adminUserId &&
            log.Action == "ViewCustomerProfile" &&
            log.EntityType == "Customer" &&
            log.EntityId == customerId &&
            log.IpAddress == null)))
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogCustomerProfileAccessAsync(adminUserId, customerId);

        // Assert
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogPayoutDetailsAccessAsync_WithValidParameters_CreatesAuditLog()
    {
        // Arrange
        var adminUserId = "admin-123";
        var sellerId = "seller-789";
        var ipAddress = "10.0.0.1";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.Is<AdminAuditLog>(log =>
            log.AdminUserId == adminUserId &&
            log.Action == "ViewPayoutDetails" &&
            log.EntityType == "Seller" &&
            log.EntityId == sellerId &&
            log.IpAddress == ipAddress &&
            log.Details!.Contains(sellerId))))
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogPayoutDetailsAccessAsync(adminUserId, sellerId, ipAddress);

        // Assert
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogKycDocumentAccessAsync_WithValidParameters_CreatesAuditLog()
    {
        // Arrange
        var adminUserId = "admin-123";
        var submissionId = Guid.NewGuid();
        var ipAddress = "172.16.0.1";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.Is<AdminAuditLog>(log =>
            log.AdminUserId == adminUserId &&
            log.Action == "ViewKycDocument" &&
            log.EntityType == "KycSubmission" &&
            log.EntityId == submissionId.ToString() &&
            log.IpAddress == ipAddress &&
            log.Details!.Contains(submissionId.ToString()))))
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogKycDocumentAccessAsync(adminUserId, submissionId, ipAddress);

        // Assert
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogStoreDetailsAccessAsync_WithValidParameters_CreatesAuditLog()
    {
        // Arrange
        var adminUserId = "admin-123";
        var storeId = Guid.NewGuid();
        var ipAddress = "192.168.0.100";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.Is<AdminAuditLog>(log =>
            log.AdminUserId == adminUserId &&
            log.Action == "ViewStoreDetails" &&
            log.EntityType == "Store" &&
            log.EntityId == storeId.ToString() &&
            log.IpAddress == ipAddress &&
            log.Details!.Contains(storeId.ToString()))))
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogStoreDetailsAccessAsync(adminUserId, storeId, ipAddress);

        // Assert
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogCustomerProfileAccessAsync_WhenRepositoryThrows_RethrowsException()
    {
        // Arrange
        var adminUserId = "admin-123";
        var customerId = "customer-456";

        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<AdminAuditLog>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LogCustomerProfileAccessAsync(adminUserId, customerId));

        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogPayoutDetailsAccessAsync_CreatesAuditLogWithCorrectTimestamp()
    {
        // Arrange
        var adminUserId = "admin-123";
        var sellerId = "seller-789";
        var beforeCall = DateTimeOffset.UtcNow;

        var capturedLog = default(AdminAuditLog);
        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<AdminAuditLog>()))
            .Callback<AdminAuditLog>(log => capturedLog = log)
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogPayoutDetailsAccessAsync(adminUserId, sellerId);
        var afterCall = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(capturedLog);
        Assert.True(capturedLog.Timestamp >= beforeCall && capturedLog.Timestamp <= afterCall);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task LogKycDocumentAccessAsync_CreatesAuditLogWithNewGuid()
    {
        // Arrange
        var adminUserId = "admin-123";
        var submissionId = Guid.NewGuid();

        var capturedLog = default(AdminAuditLog);
        var mockRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<AdminAuditLog>()))
            .Callback<AdminAuditLog>(log => capturedLog = log)
            .ReturnsAsync((AdminAuditLog log) => log);

        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        var service = new SensitiveAccessAuditService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogKycDocumentAccessAsync(adminUserId, submissionId);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.NotEqual(Guid.Empty, capturedLog.Id);
        mockRepository.VerifyAll();
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SensitiveAccessAuditService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SensitiveAccessAuditService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IAdminAuditRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SensitiveAccessAuditService(mockRepository.Object, null!));
    }
}
