using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Notifications;

public class PushNotificationServiceTests
{
    private static readonly string TestUserId = "test-user-id";
    private static readonly Guid TestNotificationId = Guid.NewGuid();
    private static readonly Guid TestSubscriptionId = Guid.NewGuid();

    private readonly Mock<IPushSubscriptionRepository> _mockPushSubscriptionRepository;
    private readonly Mock<INotificationRepository> _mockNotificationRepository;
    private readonly Mock<IWebPushClient> _mockWebPushClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<PushNotificationService>> _mockLogger;
    private readonly PushNotificationService _service;

    public PushNotificationServiceTests()
    {
        _mockPushSubscriptionRepository = new Mock<IPushSubscriptionRepository>(MockBehavior.Strict);
        _mockNotificationRepository = new Mock<INotificationRepository>(MockBehavior.Strict);
        _mockWebPushClient = new Mock<IWebPushClient>(MockBehavior.Strict);
        _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Loose);
        _mockLogger = new Mock<ILogger<PushNotificationService>>();

        _service = new PushNotificationService(
            _mockPushSubscriptionRepository.Object,
            _mockNotificationRepository.Object,
            _mockWebPushClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    #region SubscribeAsync Tests

    [Fact]
    public async Task SubscribeAsync_ValidCommand_CreatesSubscription()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();

        _mockPushSubscriptionRepository.Setup(r => r.AddAsync(It.IsAny<PushSubscription>()))
            .ReturnsAsync((PushSubscription s) => s);

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.SubscriptionId);
        Assert.NotEqual(Guid.Empty, result.SubscriptionId.Value);
        _mockPushSubscriptionRepository.Verify(r => r.AddAsync(It.Is<PushSubscription>(s =>
            s.UserId == TestUserId &&
            s.Endpoint == command.Endpoint &&
            s.P256DH == command.P256DH &&
            s.Auth == command.Auth)), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.UserId = string.Empty;

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_EmptyEndpoint_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.Endpoint = string.Empty;

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Endpoint is required.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_EmptyP256DH_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.P256DH = string.Empty;

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("P256DH key is required.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_EmptyAuth_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.Auth = string.Empty;

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Auth secret is required.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_EndpointTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.Endpoint = new string('a', 2001);

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Endpoint must not exceed 2000 characters.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_P256DHKeyTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.P256DH = new string('a', 501);

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("P256DH key must not exceed 500 characters.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_AuthSecretTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestSubscribeCommand();
        command.Auth = new string('a', 501);

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Auth secret must not exceed 500 characters.", result.Errors);
    }

    [Fact]
    public async Task SubscribeAsync_WithExpiresAt_SetsExpirationDate()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var command = CreateTestSubscribeCommand();
        command.ExpiresAt = expiresAt;

        _mockPushSubscriptionRepository.Setup(r => r.AddAsync(It.IsAny<PushSubscription>()))
            .ReturnsAsync((PushSubscription s) => s);

        // Act
        var result = await _service.SubscribeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockPushSubscriptionRepository.Verify(r => r.AddAsync(It.Is<PushSubscription>(s =>
            s.ExpiresAt == expiresAt)), Times.Once);
    }

    #endregion

    #region UnsubscribeAsync Tests

    [Fact]
    public async Task UnsubscribeAsync_ValidUserId_RemovesSubscriptions()
    {
        // Arrange
        _mockPushSubscriptionRepository.Setup(r => r.DeleteByUserIdAsync(TestUserId))
            .ReturnsAsync(2);

        // Act
        var result = await _service.UnsubscribeAsync(TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.RemovedCount);
        _mockPushSubscriptionRepository.VerifyAll();
    }

    [Fact]
    public async Task UnsubscribeAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.UnsubscribeAsync(string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task UnsubscribeAsync_NoSubscriptions_ReturnsZeroCount()
    {
        // Arrange
        _mockPushSubscriptionRepository.Setup(r => r.DeleteByUserIdAsync(TestUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _service.UnsubscribeAsync(TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.RemovedCount);
    }

    #endregion

    #region SendPushNotificationAsync Tests

    [Fact]
    public async Task SendPushNotificationAsync_EmptyNotificationId_ReturnsFailure()
    {
        // Act
        var result = await _service.SendPushNotificationAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Notification ID is required.", result.Errors);
    }

    [Fact]
    public async Task SendPushNotificationAsync_NotificationNotFound_ReturnsFailure()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _service.SendPushNotificationAsync(TestNotificationId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Notification not found.", result.Errors);
    }

    [Fact]
    public async Task SendPushNotificationAsync_NoSubscriptions_ReturnsSuccessWithZeroCount()
    {
        // Arrange
        var notification = CreateTestNotification();

        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync(notification);

        _mockPushSubscriptionRepository.Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync(new List<PushSubscription>());

        // Act
        var result = await _service.SendPushNotificationAsync(TestNotificationId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.SentCount);
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithSubscriptions_SendsPushNotifications()
    {
        // Arrange
        var notification = CreateTestNotification();
        var subscription = CreateTestPushSubscription();
        var subscriptions = new List<PushSubscription> { subscription };

        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync(notification);

        _mockPushSubscriptionRepository.Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync(subscriptions);

        _mockWebPushClient.Setup(c => c.SendAsync(
            subscription.Endpoint,
            subscription.P256DH,
            subscription.Auth,
            It.IsAny<string>()))
            .ReturnsAsync(WebPushSendResult.Succeeded());

        // Act
        var result = await _service.SendPushNotificationAsync(TestNotificationId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.SentCount);
        Assert.Equal(0, result.FailedCount);
        _mockWebPushClient.VerifyAll();
    }

    [Fact]
    public async Task SendPushNotificationAsync_ExpiredSubscription_RemovesSubscription()
    {
        // Arrange
        var notification = CreateTestNotification();
        var subscription = CreateTestPushSubscription();
        var subscriptions = new List<PushSubscription> { subscription };

        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync(notification);

        _mockPushSubscriptionRepository.Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync(subscriptions);

        _mockWebPushClient.Setup(c => c.SendAsync(
            subscription.Endpoint,
            subscription.P256DH,
            subscription.Auth,
            It.IsAny<string>()))
            .ReturnsAsync(WebPushSendResult.SubscriptionGone());

        _mockPushSubscriptionRepository.Setup(r => r.DeleteAsync(subscription.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendPushNotificationAsync(TestNotificationId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.SentCount);
        Assert.Equal(1, result.FailedCount);
        _mockPushSubscriptionRepository.Verify(r => r.DeleteAsync(subscription.Id), Times.Once);
    }

    #endregion

    #region GetSubscriptionStatusAsync Tests

    [Fact]
    public async Task GetSubscriptionStatusAsync_ValidUser_ReturnsStatus()
    {
        // Arrange
        var subscriptions = new List<PushSubscription>
        {
            CreateTestPushSubscription(),
            CreateTestPushSubscription()
        };

        _mockPushSubscriptionRepository.Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetSubscriptionStatusAsync(TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsSubscribed);
        Assert.Equal(2, result.SubscriptionCount);
        _mockPushSubscriptionRepository.VerifyAll();
    }

    [Fact]
    public async Task GetSubscriptionStatusAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetSubscriptionStatusAsync(string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetSubscriptionStatusAsync_NoSubscriptions_ReturnsNotSubscribed()
    {
        // Arrange
        _mockPushSubscriptionRepository.Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync(new List<PushSubscription>());

        // Act
        var result = await _service.GetSubscriptionStatusAsync(TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsSubscribed);
        Assert.Equal(0, result.SubscriptionCount);
    }

    #endregion

    #region Helper Methods

    private static SubscribePushCommand CreateTestSubscribeCommand()
    {
        return new SubscribePushCommand
        {
            UserId = TestUserId,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test-subscription-id",
            P256DH = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM",
            Auth = "tBHItJI5svbpez7KI4CCXg"
        };
    }

    private static Notification CreateTestNotification()
    {
        return new Notification
        {
            Id = TestNotificationId,
            UserId = TestUserId,
            Title = "Test Notification",
            Message = "This is a test notification message.",
            Type = NotificationType.OrderPlaced,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
            RelatedEntityId = Guid.NewGuid(),
            RelatedUrl = "/Orders/Details/123"
        };
    }

    private static PushSubscription CreateTestPushSubscription()
    {
        return new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test-subscription-id",
            P256DH = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM",
            Auth = "tBHItJI5svbpez7KI4CCXg",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
