using Mercato.Analytics.Application.Services;
using Mercato.Analytics.Domain.Entities;
using Mercato.Analytics.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Analytics;

public class AnalyticsServiceTests
{
    private readonly Mock<IAnalyticsEventRepository> _mockRepository;
    private readonly Mock<ILogger<AnalyticsService>> _mockLogger;

    public AnalyticsServiceTests()
    {
        _mockRepository = new Mock<IAnalyticsEventRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<AnalyticsService>>();
    }

    private AnalyticsService CreateService(AnalyticsOptions options)
    {
        var optionsWrapper = Options.Create(options);
        return new AnalyticsService(_mockRepository.Object, optionsWrapper, _mockLogger.Object);
    }

    [Fact]
    public async Task RecordSearchAsync_WhenEnabled_RecordsEvent()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordSearchAsync("session-123", "user-456", "laptop");

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == AnalyticsEventType.Search &&
                e.SessionId == "session-123" &&
                e.UserId == "user-456" &&
                e.SearchQuery == "laptop"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordSearchAsync_WhenDisabled_DoesNotRecordEvent()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = false };
        var service = CreateService(options);

        // Act
        await service.RecordSearchAsync("session-123", "user-456", "laptop");

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordProductViewAsync_WhenEnabled_RecordsEvent()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordProductViewAsync("session-123", "user-456", 42, 7);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == AnalyticsEventType.ProductView &&
                e.SessionId == "session-123" &&
                e.UserId == "user-456" &&
                e.ProductId == 42 &&
                e.SellerId == 7),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordProductViewAsync_WithGuestUser_RecordsEventWithNullUserId()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordProductViewAsync("session-123", null, 42, 7);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == AnalyticsEventType.ProductView &&
                e.SessionId == "session-123" &&
                e.UserId == null &&
                e.ProductId == 42),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordAddToCartAsync_WhenEnabled_RecordsEvent()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordAddToCartAsync("session-123", "user-456", 42, 7);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == AnalyticsEventType.AddToCart &&
                e.SessionId == "session-123" &&
                e.UserId == "user-456" &&
                e.ProductId == 42 &&
                e.SellerId == 7),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordCheckoutStartAsync_WhenEnabled_RecordsEvent()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordCheckoutStartAsync("session-123", "user-456");

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == AnalyticsEventType.CheckoutStart &&
                e.SessionId == "session-123" &&
                e.UserId == "user-456"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordOrderCompletionAsync_WhenEnabled_RecordsEvent()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordOrderCompletionAsync("session-123", "user-456", 999);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == AnalyticsEventType.OrderCompletion &&
                e.SessionId == "session-123" &&
                e.UserId == "user-456" &&
                e.OrderId == 999),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordSearchAsync_WhenRepositoryThrows_LogsWarningButDoesNotThrow()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act - should not throw
        await service.RecordSearchAsync("session-123", "user-456", "laptop");

        // Assert - verify the repository was called (even though it threw)
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventsAsync_WithEventTypeFilter_CallsRepositoryWithFilter()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        _mockRepository.Setup(r => r.GetByEventTypeAsync(
            AnalyticsEventType.Search, fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AnalyticsEvent>());

        // Act
        await service.GetEventsAsync(fromDate, toDate, AnalyticsEventType.Search);

        // Assert
        _mockRepository.Verify(r => r.GetByEventTypeAsync(
            AnalyticsEventType.Search, fromDate, toDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventsAsync_WithoutEventTypeFilter_CallsRepositoryWithTimeRange()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        _mockRepository.Setup(r => r.GetByTimeRangeAsync(fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AnalyticsEvent>());

        // Act
        await service.GetEventsAsync(fromDate, toDate);

        // Assert
        _mockRepository.Verify(r => r.GetByTimeRangeAsync(fromDate, toDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventCountsAsync_ReturnsEventCounts()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var expectedCounts = new Dictionary<AnalyticsEventType, int>
        {
            { AnalyticsEventType.Search, 100 },
            { AnalyticsEventType.ProductView, 50 },
            { AnalyticsEventType.AddToCart, 25 }
        };

        _mockRepository.Setup(r => r.GetEventCountsByTypeAsync(fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCounts);

        // Act
        var result = await service.GetEventCountsAsync(fromDate, toDate);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(100, result[AnalyticsEventType.Search]);
        Assert.Equal(50, result[AnalyticsEventType.ProductView]);
        Assert.Equal(25, result[AnalyticsEventType.AddToCart]);
    }

    [Fact]
    public void IsEnabled_ReturnsCorrectValue()
    {
        // Arrange & Act
        var enabledService = CreateService(new AnalyticsOptions { Enabled = true });
        var disabledService = CreateService(new AnalyticsOptions { Enabled = false });

        // Assert
        Assert.True(enabledService.IsEnabled);
        Assert.False(disabledService.IsEnabled);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AnalyticsOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AnalyticsService(null!, options, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AnalyticsService(_mockRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AnalyticsOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AnalyticsService(_mockRepository.Object, options, null!));
    }

    [Fact]
    public async Task RecordSearchAsync_SetsTimestamp()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);
        var beforeTest = DateTimeOffset.UtcNow;

        AnalyticsEvent? capturedEvent = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AnalyticsEvent, CancellationToken>((e, _) => capturedEvent = e)
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordSearchAsync("session-123", "user-456", "laptop");
        var afterTest = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.True(capturedEvent.Timestamp >= beforeTest);
        Assert.True(capturedEvent.Timestamp <= afterTest);
    }

    [Fact]
    public async Task RecordSearchAsync_GeneratesUniqueId()
    {
        // Arrange
        var options = new AnalyticsOptions { Enabled = true };
        var service = CreateService(options);
        var capturedEvents = new List<AnalyticsEvent>();

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AnalyticsEvent, CancellationToken>((e, _) => capturedEvents.Add(e))
            .ReturnsAsync((AnalyticsEvent e, CancellationToken _) => e);

        // Act
        await service.RecordSearchAsync("session-1", null, "query1");
        await service.RecordSearchAsync("session-2", null, "query2");

        // Assert
        Assert.Equal(2, capturedEvents.Count);
        Assert.NotEqual(capturedEvents[0].Id, capturedEvents[1].Id);
        Assert.NotEqual(Guid.Empty, capturedEvents[0].Id);
        Assert.NotEqual(Guid.Empty, capturedEvents[1].Id);
    }
}
