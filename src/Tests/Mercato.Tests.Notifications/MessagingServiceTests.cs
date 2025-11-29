using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Notifications;

public class MessagingServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly string TestSellerId = "test-seller-id";
    private static readonly Guid TestThreadId = Guid.NewGuid();
    private static readonly Guid TestMessageId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();

    private readonly Mock<IMessageThreadRepository> _mockThreadRepository;
    private readonly Mock<IMessageRepository> _mockMessageRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<MessagingService>> _mockLogger;
    private readonly MessagingService _service;

    public MessagingServiceTests()
    {
        _mockThreadRepository = new Mock<IMessageThreadRepository>(MockBehavior.Strict);
        _mockMessageRepository = new Mock<IMessageRepository>(MockBehavior.Strict);
        _mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<MessagingService>>();

        _service = new MessagingService(
            _mockThreadRepository.Object,
            _mockMessageRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    #region CreateThreadAsync Tests

    [Fact]
    public async Task CreateThreadAsync_ValidProductQuestion_CreatesThreadAndMessage()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();

        _mockThreadRepository.Setup(r => r.AddAsync(It.IsAny<MessageThread>()))
            .ReturnsAsync((MessageThread t) => t);

        _mockMessageRepository.Setup(r => r.AddAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        _mockNotificationService.Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(CreateNotificationResult.Success(Guid.NewGuid()));

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ThreadId);
        Assert.NotNull(result.MessageId);
        _mockThreadRepository.Verify(r => r.AddAsync(It.Is<MessageThread>(t =>
            t.BuyerId == TestBuyerId &&
            t.SellerId == TestSellerId &&
            t.ProductId == TestProductId &&
            t.ThreadType == MessageThreadType.ProductQuestion)), Times.Once);
        _mockMessageRepository.Verify(r => r.AddAsync(It.Is<Message>(m =>
            m.SenderId == TestBuyerId &&
            m.Content == command.InitialMessage)), Times.Once);
    }

    [Fact]
    public async Task CreateThreadAsync_ValidOrderMessage_CreatesThreadAndMessage()
    {
        // Arrange
        var command = CreateValidOrderMessageCommand();

        _mockThreadRepository.Setup(r => r.AddAsync(It.IsAny<MessageThread>()))
            .ReturnsAsync((MessageThread t) => t);

        _mockMessageRepository.Setup(r => r.AddAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        _mockNotificationService.Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(CreateNotificationResult.Success(Guid.NewGuid()));

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ThreadId);
        _mockThreadRepository.Verify(r => r.AddAsync(It.Is<MessageThread>(t =>
            t.OrderId == TestOrderId &&
            t.ThreadType == MessageThreadType.OrderMessage)), Times.Once);
    }

    [Fact]
    public async Task CreateThreadAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.BuyerId = string.Empty;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.SellerId = string.Empty;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.StoreId = Guid.Empty;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_EmptySubject_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.Subject = string.Empty;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Subject is required.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_SubjectTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.Subject = new string('a', 201);

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Subject must not exceed 200 characters.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_EmptyInitialMessage_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.InitialMessage = string.Empty;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Initial message is required.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_InitialMessageTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.InitialMessage = new string('a', 4001);

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Initial message must not exceed 4000 characters.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_ProductQuestionWithoutProductId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidProductQuestionCommand();
        command.ProductId = null;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required for product questions.", result.Errors);
    }

    [Fact]
    public async Task CreateThreadAsync_OrderMessageWithoutOrderId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidOrderMessageCommand();
        command.OrderId = null;

        // Act
        var result = await _service.CreateThreadAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order ID is required for order messages.", result.Errors);
    }

    #endregion

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_ValidMessage_SendsMessage()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        var thread = CreateTestThread();

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        _mockMessageRepository.Setup(r => r.AddAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        _mockThreadRepository.Setup(r => r.UpdateAsync(It.IsAny<MessageThread>()))
            .Returns(Task.CompletedTask);

        _mockNotificationService.Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(CreateNotificationResult.Success(Guid.NewGuid()));

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.MessageId);
        _mockMessageRepository.Verify(r => r.AddAsync(It.Is<Message>(m =>
            m.ThreadId == TestThreadId &&
            m.SenderId == TestBuyerId &&
            m.Content == command.Content)), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ThreadNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync((MessageThread?)null);

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Thread not found.", result.Errors);
    }

    [Fact]
    public async Task SendMessageAsync_ThreadClosed_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        var thread = CreateTestThread();
        thread.IsClosed = true;

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot send messages to a closed thread.", result.Errors);
    }

    [Fact]
    public async Task SendMessageAsync_UnauthorizedSender_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        command.SenderId = "unauthorized-user-id";
        var thread = CreateTestThread();

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyThreadId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        command.ThreadId = Guid.Empty;

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Thread ID is required.", result.Errors);
    }

    [Fact]
    public async Task SendMessageAsync_EmptySenderId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        command.SenderId = string.Empty;

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sender ID is required.", result.Errors);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyContent_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        command.Content = string.Empty;

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message content is required.", result.Errors);
    }

    [Fact]
    public async Task SendMessageAsync_ContentTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSendMessageCommand();
        command.Content = new string('a', 4001);

        // Act
        var result = await _service.SendMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message content must not exceed 4000 characters.", result.Errors);
    }

    #endregion

    #region GetThreadAsync Tests

    [Fact]
    public async Task GetThreadAsync_ValidRequest_ReturnsThread()
    {
        // Arrange
        var thread = CreateTestThread();

        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, TestBuyerId, false))
            .ReturnsAsync(true);

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        // Act
        var result = await _service.GetThreadAsync(TestThreadId, TestBuyerId, false);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Thread);
        Assert.Equal(TestThreadId, result.Thread.Id);
    }

    [Fact]
    public async Task GetThreadAsync_UnauthorizedAccess_ReturnsNotAuthorized()
    {
        // Arrange
        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, "other-user", false))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetThreadAsync(TestThreadId, "other-user", false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetThreadAsync_AdminAccess_ReturnsThread()
    {
        // Arrange
        var thread = CreateTestThread();

        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, "admin-user", true))
            .ReturnsAsync(true);

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        // Act
        var result = await _service.GetThreadAsync(TestThreadId, "admin-user", true);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Thread);
    }

    [Fact]
    public async Task GetThreadAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetThreadAsync(TestThreadId, string.Empty, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetThreadAsync_EmptyThreadId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetThreadAsync(Guid.Empty, TestBuyerId, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Thread ID is required.", result.Errors);
    }

    #endregion

    #region GetThreadMessagesAsync Tests

    [Fact]
    public async Task GetThreadMessagesAsync_ValidRequest_ReturnsMessages()
    {
        // Arrange
        var messages = new List<Message> { CreateTestMessage() };

        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, TestBuyerId, false))
            .ReturnsAsync(true);

        _mockMessageRepository.Setup(r => r.GetByThreadIdAsync(TestThreadId, 1, 10))
            .ReturnsAsync((messages, 1));

        // Act
        var result = await _service.GetThreadMessagesAsync(TestThreadId, TestBuyerId, false, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Messages);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetThreadMessagesAsync_UnauthorizedAccess_ReturnsNotAuthorized()
    {
        // Arrange
        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, "other-user", false))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetThreadMessagesAsync(TestThreadId, "other-user", false, 1, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetThreadMessagesAsync_InvalidPage_ReturnsFailure()
    {
        // Act
        var result = await _service.GetThreadMessagesAsync(TestThreadId, TestBuyerId, false, 0, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page number must be at least 1.", result.Errors);
    }

    [Fact]
    public async Task GetThreadMessagesAsync_InvalidPageSize_ReturnsFailure()
    {
        // Act
        var result = await _service.GetThreadMessagesAsync(TestThreadId, TestBuyerId, false, 1, 0);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    #endregion

    #region GetBuyerThreadsAsync Tests

    [Fact]
    public async Task GetBuyerThreadsAsync_ValidRequest_ReturnsThreads()
    {
        // Arrange
        var threads = new List<MessageThread> { CreateTestThread() };

        _mockThreadRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId, 1, 10))
            .ReturnsAsync((threads, 1));

        // Act
        var result = await _service.GetBuyerThreadsAsync(TestBuyerId, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Threads);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetBuyerThreadsAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetBuyerThreadsAsync(string.Empty, 1, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    #endregion

    #region GetSellerThreadsAsync Tests

    [Fact]
    public async Task GetSellerThreadsAsync_ValidRequest_ReturnsThreads()
    {
        // Arrange
        var threads = new List<MessageThread> { CreateTestThread() };

        _mockThreadRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId, 1, 10))
            .ReturnsAsync((threads, 1));

        // Act
        var result = await _service.GetSellerThreadsAsync(TestSellerId, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Threads);
    }

    #endregion

    #region GetAllThreadsAsync Tests

    [Fact]
    public async Task GetAllThreadsAsync_ValidRequest_ReturnsAllThreads()
    {
        // Arrange
        var threads = new List<MessageThread> { CreateTestThread() };

        _mockThreadRepository.Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync((threads, 1));

        // Act
        var result = await _service.GetAllThreadsAsync(1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Threads);
    }

    [Fact]
    public async Task GetAllThreadsAsync_InvalidPagination_ReturnsFailure()
    {
        // Act
        var result = await _service.GetAllThreadsAsync(0, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page number must be at least 1.", result.Errors);
    }

    #endregion

    #region GetProductQuestionsAsync Tests

    [Fact]
    public async Task GetProductQuestionsAsync_ValidRequest_ReturnsProductQuestions()
    {
        // Arrange
        var threads = new List<MessageThread> { CreateTestThread() };

        _mockThreadRepository.Setup(r => r.GetByProductIdAsync(TestProductId, 1, 10))
            .ReturnsAsync((threads, 1));

        // Act
        var result = await _service.GetProductQuestionsAsync(TestProductId, 1, 10);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Threads);
    }

    [Fact]
    public async Task GetProductQuestionsAsync_EmptyProductId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetProductQuestionsAsync(Guid.Empty, 1, 10);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    #endregion

    #region CloseThreadAsync Tests

    [Fact]
    public async Task CloseThreadAsync_ValidRequest_ClosesThread()
    {
        // Arrange
        var thread = CreateTestThread();

        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, TestBuyerId, false))
            .ReturnsAsync(true);

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        _mockThreadRepository.Setup(r => r.UpdateAsync(It.IsAny<MessageThread>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CloseThreadAsync(TestThreadId, TestBuyerId, false);

        // Assert
        Assert.True(result.Succeeded);
        _mockThreadRepository.Verify(r => r.UpdateAsync(It.Is<MessageThread>(t =>
            t.IsClosed == true && t.ClosedAt != null)), Times.Once);
    }

    [Fact]
    public async Task CloseThreadAsync_UnauthorizedAccess_ReturnsNotAuthorized()
    {
        // Arrange
        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, "other-user", false))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CloseThreadAsync(TestThreadId, "other-user", false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CloseThreadAsync_AlreadyClosed_ReturnsFailure()
    {
        // Arrange
        var thread = CreateTestThread();
        thread.IsClosed = true;

        _mockThreadRepository.Setup(r => r.CanAccessAsync(TestThreadId, TestBuyerId, false))
            .ReturnsAsync(true);

        _mockThreadRepository.Setup(r => r.GetByIdAsync(TestThreadId))
            .ReturnsAsync(thread);

        // Act
        var result = await _service.CloseThreadAsync(TestThreadId, TestBuyerId, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Thread is already closed.", result.Errors);
    }

    [Fact]
    public async Task CloseThreadAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.CloseThreadAsync(TestThreadId, string.Empty, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task CloseThreadAsync_EmptyThreadId_ReturnsFailure()
    {
        // Act
        var result = await _service.CloseThreadAsync(Guid.Empty, TestBuyerId, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Thread ID is required.", result.Errors);
    }

    #endregion

    #region MarkMessageAsReadAsync Tests

    [Fact]
    public async Task MarkMessageAsReadAsync_ValidRequest_MarksAsRead()
    {
        // Arrange
        var thread = CreateTestThread();
        var message = CreateTestMessage();
        message.SenderId = TestSellerId; // Sent by seller, buyer can mark as read
        message.Thread = thread;

        _mockMessageRepository.Setup(r => r.GetByIdAsync(TestMessageId))
            .ReturnsAsync(message);

        _mockMessageRepository.Setup(r => r.MarkAsReadAsync(TestMessageId, TestBuyerId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.MarkMessageAsReadAsync(TestMessageId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_SenderMarksOwn_ReturnsSuccessNoOp()
    {
        // Arrange
        var thread = CreateTestThread();
        var message = CreateTestMessage();
        message.SenderId = TestBuyerId; // Sent by buyer, buyer tries to mark as read
        message.Thread = thread;

        _mockMessageRepository.Setup(r => r.GetByIdAsync(TestMessageId))
            .ReturnsAsync(message);

        // Act - sender marking their own message as read should succeed without action
        var result = await _service.MarkMessageAsReadAsync(TestMessageId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_UnauthorizedUser_ReturnsNotAuthorized()
    {
        // Arrange
        var thread = CreateTestThread();
        var message = CreateTestMessage();
        message.Thread = thread;

        _mockMessageRepository.Setup(r => r.GetByIdAsync(TestMessageId))
            .ReturnsAsync(message);

        // Act
        var result = await _service.MarkMessageAsReadAsync(TestMessageId, "other-user");

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_MessageNotFound_ReturnsFailure()
    {
        // Arrange
        _mockMessageRepository.Setup(r => r.GetByIdAsync(TestMessageId))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _service.MarkMessageAsReadAsync(TestMessageId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message not found.", result.Errors);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.MarkMessageAsReadAsync(TestMessageId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_EmptyMessageId_ReturnsFailure()
    {
        // Act
        var result = await _service.MarkMessageAsReadAsync(Guid.Empty, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message ID is required.", result.Errors);
    }

    #endregion

    #region Helper Methods

    private static CreateMessageThreadCommand CreateValidProductQuestionCommand()
    {
        return new CreateMessageThreadCommand
        {
            ProductId = TestProductId,
            BuyerId = TestBuyerId,
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            Subject = "Question about product",
            InitialMessage = "Is this product available in blue?",
            ThreadType = MessageThreadType.ProductQuestion
        };
    }

    private static CreateMessageThreadCommand CreateValidOrderMessageCommand()
    {
        return new CreateMessageThreadCommand
        {
            OrderId = TestOrderId,
            BuyerId = TestBuyerId,
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            Subject = "Question about order",
            InitialMessage = "When will my order ship?",
            ThreadType = MessageThreadType.OrderMessage
        };
    }

    private static SendMessageCommand CreateValidSendMessageCommand()
    {
        return new SendMessageCommand
        {
            ThreadId = TestThreadId,
            SenderId = TestBuyerId,
            Content = "This is a test message."
        };
    }

    private static MessageThread CreateTestThread()
    {
        return new MessageThread
        {
            Id = TestThreadId,
            ProductId = TestProductId,
            BuyerId = TestBuyerId,
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            Subject = "Test Thread",
            ThreadType = MessageThreadType.ProductQuestion,
            CreatedAt = DateTimeOffset.UtcNow,
            LastMessageAt = DateTimeOffset.UtcNow,
            IsClosed = false
        };
    }

    private static Message CreateTestMessage()
    {
        return new Message
        {
            Id = TestMessageId,
            ThreadId = TestThreadId,
            SenderId = TestBuyerId,
            Content = "Test message content.",
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false
        };
    }

    #endregion
}
