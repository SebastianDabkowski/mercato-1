using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Web.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class SecurityDashboardModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsStatisticsAndEvents()
    {
        // Arrange
        var statistics = new AuthenticationStatistics
        {
            TotalSuccessfulLogins = 10,
            TotalFailedLogins = 5,
            TotalLockouts = 2,
            TotalPasswordResets = 1
        };

        var suspiciousActivities = new List<SuspiciousActivityInfo>();
        var recentEvents = new List<AuthenticationEvent>
        {
            new() { Email = "test@example.com", EventType = AuthenticationEventType.Login, IsSuccessful = true }
        };

        var mockService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);
        mockService.Setup(x => x.GetStatisticsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);
        mockService.Setup(x => x.GetSuspiciousActivityAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspiciousActivities);
        mockService.Setup(x => x.GetEventsAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<AuthenticationEventType?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentEvents);

        var mockLogger = new Mock<ILogger<SecurityDashboardModel>>();

        var model = CreateModel(mockService.Object, mockLogger.Object);
        model.TimePeriod = "24h";

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.NotNull(model.Statistics);
        Assert.Equal(10, model.Statistics.TotalSuccessfulLogins);
        Assert.Equal(5, model.Statistics.TotalFailedLogins);
        Assert.NotNull(model.SuspiciousActivities);
        Assert.Single(model.RecentEvents);
    }

    [Fact]
    public async Task OnGetAsync_WithDifferentTimePeriods_CalculatesCorrectDateRange()
    {
        // Arrange
        var mockService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);
        mockService.Setup(x => x.GetStatisticsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationStatistics());
        mockService.Setup(x => x.GetSuspiciousActivityAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SuspiciousActivityInfo>());
        mockService.Setup(x => x.GetEventsAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<AuthenticationEventType?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationEvent>());

        var mockLogger = new Mock<ILogger<SecurityDashboardModel>>();

        // Test 7 days period
        var model = CreateModel(mockService.Object, mockLogger.Object);
        model.TimePeriod = "7d";

        // Act
        await model.OnGetAsync();

        // Assert - verify the service was called with appropriate date range
        mockService.Verify(x => x.GetStatisticsAsync(
            It.Is<DateTimeOffset>(d => d < DateTimeOffset.UtcNow.AddDays(-6)),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var mockService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);
        mockService.Setup(x => x.GetStatisticsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationStatistics());
        mockService.Setup(x => x.GetSuspiciousActivityAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SuspiciousActivityInfo>());
        mockService.Setup(x => x.GetEventsAsync(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                AuthenticationEventType.Login,
                "Buyer",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationEvent>());

        var mockLogger = new Mock<ILogger<SecurityDashboardModel>>();

        var model = CreateModel(mockService.Object, mockLogger.Object);
        model.EventTypeFilter = AuthenticationEventType.Login;
        model.UserRoleFilter = "Buyer";
        model.SuccessFilter = true;

        // Act
        await model.OnGetAsync();

        // Assert
        mockService.Verify(x => x.GetEventsAsync(
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<DateTimeOffset?>(),
            AuthenticationEventType.Login,
            "Buyer",
            true,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(AlertSeverity.Critical, "bg-danger")]
    [InlineData(AlertSeverity.High, "bg-warning text-dark")]
    [InlineData(AlertSeverity.Medium, "bg-info text-dark")]
    [InlineData(AlertSeverity.Low, "bg-secondary")]
    public void GetSeverityBadgeClass_ReturnsCorrectClass(AlertSeverity severity, string expectedClass)
    {
        // Act
        var result = SecurityDashboardModel.GetSeverityBadgeClass(severity);

        // Assert
        Assert.Equal(expectedClass, result);
    }

    [Theory]
    [InlineData(AuthenticationEventType.Login, "bg-primary")]
    [InlineData(AuthenticationEventType.Logout, "bg-secondary")]
    [InlineData(AuthenticationEventType.Lockout, "bg-danger")]
    [InlineData(AuthenticationEventType.PasswordReset, "bg-warning text-dark")]
    [InlineData(AuthenticationEventType.PasswordChange, "bg-info text-dark")]
    [InlineData(AuthenticationEventType.TwoFactorAuthentication, "bg-success")]
    public void GetEventTypeBadgeClass_ReturnsCorrectClass(AuthenticationEventType eventType, string expectedClass)
    {
        // Act
        var result = SecurityDashboardModel.GetEventTypeBadgeClass(eventType);

        // Assert
        Assert.Equal(expectedClass, result);
    }

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SecurityDashboardModel>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityDashboardModel(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockService = new Mock<IAuthenticationEventService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityDashboardModel(mockService.Object, null!));
    }

    private static SecurityDashboardModel CreateModel(
        IAuthenticationEventService service,
        ILogger<SecurityDashboardModel> logger)
    {
        var httpContext = new DefaultHttpContext();
        var pageContext = new PageContext
        {
            HttpContext = httpContext
        };

        var model = new SecurityDashboardModel(service, logger)
        {
            PageContext = pageContext
        };

        return model;
    }
}
