using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Notifications;

public class NotificationServiceTests
{
    private static readonly string TestUserId = "test-user-id";
    private static readonly Guid TestNotificationId = Guid.NewGuid();

    private readonly Mock<INotificationRepository> _mockNotificationRepository;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _mockNotificationRepository = new Mock<INotificationRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _service = new NotificationService(
            _mockNotificationRepository.Object,
            _mockLogger.Object);
    }

    #region GetUserNotificationsAsync Tests

    [Fact]
    public async Task GetUserNotificationsAsync_ValidQuery_ReturnsNotifications()
    {
        // Arrange
        var notifications = new List<Notification> { CreateTestNotification() };

        _mockNotificationRepository.Setup(r => r.GetByUserIdAsync(TestUserId, null, 1, 10))
            .ReturnsAsync((notifications, 1));

        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, null, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Notifications);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        _mockNotificationRepository.VerifyAll();
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithUnreadFilter_ReturnsUnreadOnly()
    {
        // Arrange
        var notifications = new List<Notification> { CreateTestNotification() };

        _mockNotificationRepository.Setup(r => r.GetByUserIdAsync(TestUserId, false, 1, 10))
            .ReturnsAsync((notifications, 1));

        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, false, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Notifications);
        _mockNotificationRepository.Verify(r => r.GetByUserIdAsync(TestUserId, false, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithReadFilter_ReturnsReadOnly()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.IsRead = true;
        var notifications = new List<Notification> { notification };

        _mockNotificationRepository.Setup(r => r.GetByUserIdAsync(TestUserId, true, 1, 10))
            .ReturnsAsync((notifications, 1));

        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, true, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Notifications);
        _mockNotificationRepository.Verify(r => r.GetByUserIdAsync(TestUserId, true, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetUserNotificationsAsync(string.Empty, null, 1, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_InvalidPage_ReturnsFailure()
    {
        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, null, 0, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page number must be at least 1.", result.Errors);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_InvalidPageSize_ReturnsFailure()
    {
        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, null, 1, 0);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_PageSizeTooLarge_ReturnsFailure()
    {
        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, null, 1, 101);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_NoNotifications_ReturnsEmptyList()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.GetByUserIdAsync(TestUserId, null, 1, 10))
            .ReturnsAsync((new List<Notification>(), 0));

        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, null, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Notifications);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_Pagination_ReturnsCorrectTotalPages()
    {
        // Arrange
        var notifications = new List<Notification> { CreateTestNotification() };

        _mockNotificationRepository.Setup(r => r.GetByUserIdAsync(TestUserId, null, 1, 10))
            .ReturnsAsync((notifications, 25));

        // Act
        var result = await _service.GetUserNotificationsAsync(TestUserId, null, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_ValidUser_ReturnsCount()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.GetUnreadCountAsync(TestUserId))
            .ReturnsAsync(5);

        // Act
        var result = await _service.GetUnreadCountAsync(TestUserId);

        // Assert
        Assert.Equal(5, result);
        _mockNotificationRepository.VerifyAll();
    }

    [Fact]
    public async Task GetUnreadCountAsync_EmptyUserId_ReturnsZero()
    {
        // Act
        var result = await _service.GetUnreadCountAsync(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.GetUnreadCountAsync(TestUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetUnreadCountAsync(TestUserId);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_ValidNotification_MarksAsRead()
    {
        // Arrange
        var notification = CreateTestNotification();

        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync(notification);

        _mockNotificationRepository.Setup(r => r.MarkAsReadAsync(TestNotificationId, TestUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.MarkAsReadAsync(TestNotificationId, TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        _mockNotificationRepository.VerifyAll();
    }

    [Fact]
    public async Task MarkAsReadAsync_NotificationNotFound_ReturnsFailure()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _service.MarkAsReadAsync(TestNotificationId, TestUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Notification not found.", result.Errors);
    }

    [Fact]
    public async Task MarkAsReadAsync_DifferentUser_ReturnsNotAuthorized()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.UserId = "other-user-id";

        _mockNotificationRepository.Setup(r => r.GetByIdAsync(TestNotificationId))
            .ReturnsAsync(notification);

        // Act
        var result = await _service.MarkAsReadAsync(TestNotificationId, TestUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task MarkAsReadAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.MarkAsReadAsync(TestNotificationId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task MarkAsReadAsync_EmptyNotificationId_ReturnsFailure()
    {
        // Act
        var result = await _service.MarkAsReadAsync(Guid.Empty, TestUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Notification ID is required.", result.Errors);
    }

    #endregion

    #region MarkAllAsReadAsync Tests

    [Fact]
    public async Task MarkAllAsReadAsync_ValidUser_MarksAllAsRead()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.MarkAllAsReadAsync(TestUserId))
            .ReturnsAsync(5);

        // Act
        var result = await _service.MarkAllAsReadAsync(TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(5, result.MarkedCount);
        _mockNotificationRepository.VerifyAll();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.MarkAllAsReadAsync(string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_NoUnreadNotifications_ReturnsZeroCount()
    {
        // Arrange
        _mockNotificationRepository.Setup(r => r.MarkAllAsReadAsync(TestUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _service.MarkAllAsReadAsync(TestUserId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.MarkedCount);
    }

    #endregion

    #region CreateNotificationAsync Tests

    [Fact]
    public async Task CreateNotificationAsync_ValidCommand_CreatesNotification()
    {
        // Arrange
        var command = CreateTestCommand();

        _mockNotificationRepository.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => n);

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.NotificationId);
        _mockNotificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n =>
            n.UserId == TestUserId &&
            n.Title == command.Title &&
            n.Message == command.Message &&
            n.Type == command.Type &&
            !n.IsRead)), Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithRelatedEntity_SetsRelatedEntityId()
    {
        // Arrange
        var relatedId = Guid.NewGuid();
        var command = CreateTestCommand();
        command.RelatedEntityId = relatedId;
        command.RelatedUrl = "/Orders/Details/123";

        _mockNotificationRepository.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => n);

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockNotificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n =>
            n.RelatedEntityId == relatedId &&
            n.RelatedUrl == "/Orders/Details/123")), Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        command.UserId = string.Empty;

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateNotificationAsync_EmptyTitle_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        command.Title = string.Empty;

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Title is required.", result.Errors);
    }

    [Fact]
    public async Task CreateNotificationAsync_TitleTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        command.Title = new string('a', 201);

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Title must not exceed 200 characters.", result.Errors);
    }

    [Fact]
    public async Task CreateNotificationAsync_EmptyMessage_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        command.Message = string.Empty;

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message is required.", result.Errors);
    }

    [Fact]
    public async Task CreateNotificationAsync_MessageTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        command.Message = new string('a', 2001);

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message must not exceed 2000 characters.", result.Errors);
    }

    [Fact]
    public async Task CreateNotificationAsync_RelatedUrlTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        command.RelatedUrl = new string('a', 501);

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Related URL must not exceed 500 characters.", result.Errors);
    }

    [Theory]
    [InlineData(NotificationType.OrderPlaced)]
    [InlineData(NotificationType.OrderShipped)]
    [InlineData(NotificationType.OrderDelivered)]
    [InlineData(NotificationType.ReturnRequested)]
    [InlineData(NotificationType.ReturnApproved)]
    [InlineData(NotificationType.ReturnRejected)]
    [InlineData(NotificationType.PayoutProcessed)]
    [InlineData(NotificationType.Message)]
    [InlineData(NotificationType.SystemUpdate)]
    public async Task CreateNotificationAsync_AllNotificationTypes_CreatesSuccessfully(NotificationType type)
    {
        // Arrange
        var command = CreateTestCommand();
        command.Type = type;

        _mockNotificationRepository.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => n);

        // Act
        var result = await _service.CreateNotificationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockNotificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n =>
            n.Type == type)), Times.Once);
    }

    #endregion

    #region Helper Methods

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

    private static CreateNotificationCommand CreateTestCommand()
    {
        return new CreateNotificationCommand
        {
            UserId = TestUserId,
            Title = "Test Notification",
            Message = "This is a test notification message.",
            Type = NotificationType.OrderPlaced
        };
    }

    #endregion
}
