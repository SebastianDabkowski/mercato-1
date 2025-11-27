using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class AuthenticationEventServiceTests
{
    [Fact]
    public async Task LogEventAsync_WithValidInput_AddsEventToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<AuthenticationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act
        await service.LogEventAsync(
            AuthenticationEventType.Login,
            "test@example.com",
            isSuccessful: true,
            userId: "user-123",
            userRole: "Buyer",
            ipAddress: "192.168.1.1",
            userAgent: "Mozilla/5.0");

        // Assert
        mockRepository.Verify(
            x => x.AddAsync(
                It.Is<AuthenticationEvent>(e =>
                    e.EventType == AuthenticationEventType.Login &&
                    e.Email == "test@example.com" &&
                    e.IsSuccessful == true &&
                    e.UserId == "user-123" &&
                    e.UserRole == "Buyer" &&
                    e.IpAddressHash != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogEventAsync_WithRepositoryException_DoesNotThrow()
    {
        // Arrange
        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<AuthenticationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act - should not throw
        await service.LogEventAsync(
            AuthenticationEventType.Login,
            "test@example.com",
            isSuccessful: false);

        // Assert - exception was caught and logged
        mockRepository.Verify(
            x => x.AddAsync(It.IsAny<AuthenticationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-1);
        var endDate = DateTimeOffset.UtcNow;

        var events = new List<AuthenticationEvent>
        {
            new() { EventType = AuthenticationEventType.Login, IsSuccessful = true },
            new() { EventType = AuthenticationEventType.Login, IsSuccessful = true },
            new() { EventType = AuthenticationEventType.Login, IsSuccessful = false },
            new() { EventType = AuthenticationEventType.Lockout, IsSuccessful = false },
            new() { EventType = AuthenticationEventType.PasswordReset, IsSuccessful = true }
        };

        var eventCounts = new Dictionary<AuthenticationEventType, int>
        {
            { AuthenticationEventType.Login, 3 },
            { AuthenticationEventType.Lockout, 1 },
            { AuthenticationEventType.PasswordReset, 1 }
        };

        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
        mockRepository.Setup(x => x.GetEventCountsByTypeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventCounts);

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetStatisticsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalSuccessfulLogins);
        Assert.Equal(1, result.TotalFailedLogins);
        Assert.Equal(1, result.TotalLockouts);
        Assert.Equal(1, result.TotalPasswordResets);
    }

    [Fact]
    public async Task GetSuspiciousActivityAsync_DetectsBruteForce()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-1);
        var endDate = DateTimeOffset.UtcNow;

        var failedByIp = new Dictionary<string, int>
        {
            { "hash123", 15 } // Multiple failed attempts from same IP
        };

        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFailedAttemptsByIpAsync(startDate, endDate, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedByIp);
        mockRepository.Setup(x => x.GetRapidLoginAttemptsAsync(startDate, endDate, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetSuspiciousActivityAsync(startDate, endDate);

        // Assert
        Assert.Single(result);
        Assert.Equal(SuspiciousActivityType.BruteForce, result[0].ActivityType);
        Assert.Equal(15, result[0].Count);
        Assert.Equal(AlertSeverity.Medium, result[0].Severity);
    }

    [Fact]
    public async Task GetSuspiciousActivityAsync_DetectsCredentialStuffing()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-1);
        var endDate = DateTimeOffset.UtcNow;

        var rapidAttempts = new Dictionary<string, int>
        {
            { "user@example.com", 50 } // Rapid attempts on same account
        };

        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFailedAttemptsByIpAsync(startDate, endDate, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        mockRepository.Setup(x => x.GetRapidLoginAttemptsAsync(startDate, endDate, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rapidAttempts);

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetSuspiciousActivityAsync(startDate, endDate);

        // Assert
        Assert.Single(result);
        Assert.Equal(SuspiciousActivityType.RapidAttempts, result[0].ActivityType);
        Assert.Equal(50, result[0].Count);
        Assert.Equal(AlertSeverity.High, result[0].Severity);
    }

    [Fact]
    public async Task GetEventsAsync_PassesFiltersToRepository()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-1);
        var endDate = DateTimeOffset.UtcNow;

        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                startDate,
                endDate,
                AuthenticationEventType.Login,
                "Buyer",
                null,
                true,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationEvent>());

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetEventsAsync(
            startDate,
            endDate,
            AuthenticationEventType.Login,
            "Buyer",
            isSuccessful: true);

        // Assert
        mockRepository.Verify(
            x => x.GetFilteredAsync(
                startDate,
                endDate,
                AuthenticationEventType.Login,
                "Buyer",
                null,
                true,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AuthenticationEventService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IAuthenticationEventRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AuthenticationEventService(mockRepository.Object, null!));
    }

    [Fact]
    public async Task GetSuspiciousActivityAsync_SortsResultsBySeverityAndCount()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-1);
        var endDate = DateTimeOffset.UtcNow;

        var failedByIp = new Dictionary<string, int>
        {
            { "hash1", 50 }, // Critical
            { "hash2", 10 }, // Medium
            { "hash3", 5 }   // Low
        };

        var mockRepository = new Mock<IAuthenticationEventRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFailedAttemptsByIpAsync(startDate, endDate, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedByIp);
        mockRepository.Setup(x => x.GetRapidLoginAttemptsAsync(startDate, endDate, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        var mockLogger = new Mock<ILogger<AuthenticationEventService>>();

        var service = new AuthenticationEventService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetSuspiciousActivityAsync(startDate, endDate);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(AlertSeverity.Critical, result[0].Severity);
        Assert.Equal(AlertSeverity.Medium, result[1].Severity);
        Assert.Equal(AlertSeverity.Low, result[2].Severity);
    }
}
