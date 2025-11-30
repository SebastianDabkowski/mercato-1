using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class SecurityIncidentServiceTests
{
    [Fact]
    public async Task CreateIncidentAsync_WithValidInput_CreatesIncident()
    {
        // Arrange
        var source = "192.168.1.1";
        var detectionRule = "MultipleFailedLogins";
        var severity = SecurityIncidentSeverity.Medium;
        var description = "5 failed login attempts in 5 minutes";

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync(source, detectionRule, severity, description);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Incident);
        Assert.Equal(source, result.Incident.Source);
        Assert.Equal(detectionRule, result.Incident.DetectionRule);
        Assert.Equal(severity, result.Incident.Severity);
        Assert.Equal(SecurityIncidentStatus.Open, result.Incident.Status);
        Assert.Equal(description, result.Incident.Description);
        Assert.False(result.AlertsTriggered);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateIncidentAsync_WithHighSeverity_TriggersAlert()
    {
        // Arrange
        var source = "192.168.1.1";
        var detectionRule = "DataAccessAnomaly";
        var severity = SecurityIncidentSeverity.High;
        var description = "Unusual data access pattern detected";

        SecurityIncident? capturedIncident = null;
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityIncident, CancellationToken>((i, ct) => capturedIncident = i)
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        mockNotificationService.Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(CreateNotificationResult.Success(Guid.NewGuid()));

        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync(source, detectionRule, severity, description);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.AlertsTriggered);
        Assert.NotNull(capturedIncident);
        Assert.True(capturedIncident.AlertsSent);
        mockNotificationService.Verify(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncidentAsync_WithCriticalSeverity_TriggersAlert()
    {
        // Arrange
        var source = "admin-panel";
        var detectionRule = "SuspiciousAPIUsage";
        var severity = SecurityIncidentSeverity.Critical;
        var description = "Unauthorized API access attempt";

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        mockNotificationService.Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(CreateNotificationResult.Success(Guid.NewGuid()));

        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync(source, detectionRule, severity, description);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.AlertsTriggered);
        mockNotificationService.Verify(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncidentAsync_WithEmptySource_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync("", "Rule", SecurityIncidentSeverity.Low, "Description");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Source is required.", result.Errors);
    }

    [Fact]
    public async Task CreateIncidentAsync_WithEmptyDetectionRule_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync("Source", "", SecurityIncidentSeverity.Low, "Description");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Detection rule is required.", result.Errors);
    }

    [Fact]
    public async Task CreateIncidentAsync_WithEmptyDescription_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync("Source", "Rule", SecurityIncidentSeverity.Low, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Description is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidStatusChange_UpdatesStatus()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var userId = "admin@example.com";
        var incident = new SecurityIncident
        {
            Id = incidentId,
            Source = "192.168.1.1",
            DetectionRule = "MultipleFailedLogins",
            Severity = SecurityIncidentSeverity.Medium,
            Status = SecurityIncidentStatus.Open,
            Description = "Test incident",
            DetectedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
        mockRepository.Setup(x => x.AddStatusChangeAsync(It.IsAny<SecurityIncidentStatusChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncidentStatusChange sc, CancellationToken ct) => sc);
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateStatusAsync(incidentId, SecurityIncidentStatus.Triaged, userId, "Initial triage complete");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Incident);
        Assert.Equal(SecurityIncidentStatus.Triaged, result.Incident.Status);
        mockRepository.Verify(x => x.AddStatusChangeAsync(It.Is<SecurityIncidentStatusChange>(sc =>
            sc.PreviousStatus == SecurityIncidentStatus.Open &&
            sc.NewStatus == SecurityIncidentStatus.Triaged &&
            sc.ChangedByUserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithResolvedStatus_SetsResolutionFields()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var userId = "admin@example.com";
        var resolutionNotes = "Issue was a false alarm from monitoring system.";
        var incident = new SecurityIncident
        {
            Id = incidentId,
            Source = "192.168.1.1",
            DetectionRule = "MultipleFailedLogins",
            Severity = SecurityIncidentSeverity.Medium,
            Status = SecurityIncidentStatus.InInvestigation,
            Description = "Test incident",
            DetectedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        SecurityIncident? capturedIncident = null;
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
        mockRepository.Setup(x => x.AddStatusChangeAsync(It.IsAny<SecurityIncidentStatusChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncidentStatusChange sc, CancellationToken ct) => sc);
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityIncident, CancellationToken>((i, ct) => capturedIncident = i)
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateStatusAsync(incidentId, SecurityIncidentStatus.Resolved, userId, null, resolutionNotes);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedIncident);
        Assert.Equal(SecurityIncidentStatus.Resolved, capturedIncident.Status);
        Assert.NotNull(capturedIncident.ResolvedAt);
        Assert.Equal(userId, capturedIncident.ResolvedByUserId);
        Assert.Equal(resolutionNotes, capturedIncident.ResolutionNotes);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNonExistentIncident_ReturnsError()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident?)null);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateStatusAsync(incidentId, SecurityIncidentStatus.Triaged, "user@example.com");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Incident not found.", result.Errors);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithSameStatus_ReturnsError()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new SecurityIncident
        {
            Id = incidentId,
            Status = SecurityIncidentStatus.Triaged
        };

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateStatusAsync(incidentId, SecurityIncidentStatus.Triaged, "user@example.com");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Status is already set to the requested value.", result.Errors);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithEmptyUserId_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateStatusAsync(Guid.NewGuid(), SecurityIncidentStatus.Triaged, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetIncidentAsync_WithExistingIncident_ReturnsIncident()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new SecurityIncident
        {
            Id = incidentId,
            Source = "192.168.1.1",
            DetectionRule = "MultipleFailedLogins",
            Severity = SecurityIncidentSeverity.Medium,
            Status = SecurityIncidentStatus.Open,
            Description = "Test incident"
        };

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetIncidentAsync(incidentId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Incident);
        Assert.Equal(incidentId, result.Incident.Id);
    }

    [Fact]
    public async Task GetIncidentAsync_WithNonExistentIncident_ReturnsError()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident?)null);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetIncidentAsync(incidentId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Incident not found.", result.Errors);
    }

    [Fact]
    public async Task GetIncidentsAsync_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var severity = SecurityIncidentSeverity.High;
        var status = SecurityIncidentStatus.Open;
        var detectionRule = "MultipleFailedLogins";

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                startDate,
                endDate,
                severity,
                status,
                detectionRule,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SecurityIncident>());

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetIncidentsAsync(startDate, endDate, severity, status, detectionRule);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(x => x.GetFilteredAsync(
            startDate,
            endDate,
            severity,
            status,
            detectionRule,
            100,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_WithExistingIncident_ReturnsStatusChanges()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new SecurityIncident { Id = incidentId };
        var statusChanges = new List<SecurityIncidentStatusChange>
        {
            new() { Id = Guid.NewGuid(), SecurityIncidentId = incidentId, PreviousStatus = SecurityIncidentStatus.Open, NewStatus = SecurityIncidentStatus.Triaged },
            new() { Id = Guid.NewGuid(), SecurityIncidentId = incidentId, PreviousStatus = SecurityIncidentStatus.Triaged, NewStatus = SecurityIncidentStatus.InInvestigation }
        };

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
        mockRepository.Setup(x => x.GetStatusChangesAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusChanges);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetStatusHistoryAsync(incidentId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.StatusChanges.Count);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_WithNonExistentIncident_ReturnsError()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident?)null);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetStatusHistoryAsync(incidentId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Incident not found.", result.Errors);
    }

    [Fact]
    public async Task GetComplianceReportAsync_WithValidDateRange_ReturnsReport()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;
        var incidents = new List<SecurityIncident>
        {
            new() { Id = Guid.NewGuid(), Severity = SecurityIncidentSeverity.High, Status = SecurityIncidentStatus.Resolved },
            new() { Id = Guid.NewGuid(), Severity = SecurityIncidentSeverity.Medium, Status = SecurityIncidentStatus.Open }
        };
        var incidentsByStatus = new Dictionary<SecurityIncidentStatus, int>
        {
            { SecurityIncidentStatus.Open, 1 },
            { SecurityIncidentStatus.Resolved, 1 }
        };

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetFilteredAsync(
                startDate,
                endDate,
                null,
                null,
                null,
                10000,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidents);
        mockRepository.Setup(x => x.GetIncidentCountsByStatusAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidentsByStatus);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetComplianceReportAsync(startDate, endDate);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.TotalIncidents);
        Assert.Equal(2, result.IncidentsByStatus.Count);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
    }

    [Fact]
    public async Task GetComplianceReportAsync_WithInvalidDateRange_ReturnsError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow;
        var endDate = DateTimeOffset.UtcNow.AddDays(-30); // End before start

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetComplianceReportAsync(startDate, endDate);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("End date must be after start date.", result.Errors);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityIncidentService(null!, mockNotificationService.Object, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<ISecurityIncidentRepository>();
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityIncidentService(mockRepository.Object, null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<ISecurityIncidentRepository>();
        var mockNotificationService = new Mock<INotificationService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityIncidentService(mockRepository.Object, mockNotificationService.Object, null!));
    }

    [Fact]
    public async Task CreateIncidentAsync_WithLowSeverity_DoesNotTriggerAlert()
    {
        // Arrange
        var source = "192.168.1.1";
        var detectionRule = "MinorAnomaly";
        var severity = SecurityIncidentSeverity.Low;
        var description = "Minor activity detected";

        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        // No notification setup - should not be called

        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateIncidentAsync(source, detectionRule, severity, description);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.AlertsTriggered);
        mockNotificationService.Verify(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithFalsePositiveStatus_SetsResolutionFields()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var userId = "admin@example.com";
        var incident = new SecurityIncident
        {
            Id = incidentId,
            Status = SecurityIncidentStatus.InInvestigation
        };

        SecurityIncident? capturedIncident = null;
        var mockRepository = new Mock<ISecurityIncidentRepository>(MockBehavior.Strict);
        mockRepository.Setup(x => x.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
        mockRepository.Setup(x => x.AddStatusChangeAsync(It.IsAny<SecurityIncidentStatusChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityIncidentStatusChange sc, CancellationToken ct) => sc);
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SecurityIncident>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityIncident, CancellationToken>((i, ct) => capturedIncident = i)
            .ReturnsAsync((SecurityIncident i, CancellationToken ct) => i);

        var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<SecurityIncidentService>>();

        var service = new SecurityIncidentService(
            mockRepository.Object,
            mockNotificationService.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateStatusAsync(incidentId, SecurityIncidentStatus.FalsePositive, userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedIncident);
        Assert.Equal(SecurityIncidentStatus.FalsePositive, capturedIncident.Status);
        Assert.NotNull(capturedIncident.ResolvedAt);
        Assert.Equal(userId, capturedIncident.ResolvedByUserId);
    }
}
