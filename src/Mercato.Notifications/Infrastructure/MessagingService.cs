using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Notifications.Infrastructure;

/// <summary>
/// Service implementation for messaging operations.
/// </summary>
public class MessagingService : IMessagingService
{
    private readonly IMessageThreadRepository _threadRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MessagingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingService"/> class.
    /// </summary>
    /// <param name="threadRepository">The message thread repository.</param>
    /// <param name="messageRepository">The message repository.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger.</param>
    public MessagingService(
        IMessageThreadRepository threadRepository,
        IMessageRepository messageRepository,
        INotificationService notificationService,
        ILogger<MessagingService> logger)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateMessageThreadResult> CreateThreadAsync(CreateMessageThreadCommand command)
    {
        var validationErrors = ValidateCreateThreadCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateMessageThreadResult.Failure(validationErrors);
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var threadId = Guid.NewGuid();
            var messageId = Guid.NewGuid();

            var thread = new MessageThread
            {
                Id = threadId,
                ProductId = command.ProductId,
                OrderId = command.OrderId,
                BuyerId = command.BuyerId,
                SellerId = command.SellerId,
                StoreId = command.StoreId,
                Subject = command.Subject,
                ThreadType = command.ThreadType,
                CreatedAt = now,
                LastMessageAt = now,
                IsClosed = false
            };

            var initialMessage = new Message
            {
                Id = messageId,
                ThreadId = threadId,
                SenderId = command.BuyerId,
                Content = command.InitialMessage,
                CreatedAt = now,
                IsRead = false
            };

            await _threadRepository.AddAsync(thread);
            await _messageRepository.AddAsync(initialMessage);

            // Send notification to seller about new message
            await SendNewMessageNotificationAsync(
                command.SellerId,
                command.BuyerId,
                command.Subject,
                threadId);

            _logger.LogInformation(
                "Created message thread {ThreadId} for {ThreadType} from buyer {BuyerId} to seller {SellerId}",
                threadId, command.ThreadType, command.BuyerId, command.SellerId);

            return CreateMessageThreadResult.Success(threadId, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating message thread from buyer {BuyerId}", command.BuyerId);
            return CreateMessageThreadResult.Failure("An error occurred while creating the message thread.");
        }
    }

    /// <inheritdoc />
    public async Task<SendMessageResult> SendMessageAsync(SendMessageCommand command)
    {
        var validationErrors = ValidateSendMessageCommand(command);
        if (validationErrors.Count > 0)
        {
            return SendMessageResult.Failure(validationErrors);
        }

        try
        {
            var thread = await _threadRepository.GetByIdAsync(command.ThreadId);
            if (thread == null)
            {
                return SendMessageResult.Failure("Thread not found.");
            }

            if (thread.IsClosed)
            {
                return SendMessageResult.Failure("Cannot send messages to a closed thread.");
            }

            // Check if sender is authorized (buyer or seller of the thread)
            if (thread.BuyerId != command.SenderId && thread.SellerId != command.SenderId)
            {
                return SendMessageResult.NotAuthorized();
            }

            var now = DateTimeOffset.UtcNow;
            var messageId = Guid.NewGuid();

            var message = new Message
            {
                Id = messageId,
                ThreadId = command.ThreadId,
                SenderId = command.SenderId,
                Content = command.Content,
                CreatedAt = now,
                IsRead = false
            };

            await _messageRepository.AddAsync(message);

            // Update thread's last message timestamp
            thread.LastMessageAt = now;
            await _threadRepository.UpdateAsync(thread);

            // Send notification to the recipient
            var recipientId = command.SenderId == thread.BuyerId ? thread.SellerId : thread.BuyerId;
            await SendNewMessageNotificationAsync(
                recipientId,
                command.SenderId,
                thread.Subject,
                thread.Id);

            _logger.LogInformation(
                "Sent message {MessageId} in thread {ThreadId} from {SenderId}",
                messageId, command.ThreadId, command.SenderId);

            return SendMessageResult.Success(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in thread {ThreadId}", command.ThreadId);
            return SendMessageResult.Failure("An error occurred while sending the message.");
        }
    }

    /// <inheritdoc />
    public async Task<GetThreadResult> GetThreadAsync(Guid threadId, string userId, bool isAdmin)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return GetThreadResult.Failure("User ID is required.");
        }

        if (threadId == Guid.Empty)
        {
            return GetThreadResult.Failure("Thread ID is required.");
        }

        try
        {
            var canAccess = await _threadRepository.CanAccessAsync(threadId, userId, isAdmin);
            if (!canAccess)
            {
                return GetThreadResult.NotAuthorized();
            }

            var thread = await _threadRepository.GetByIdAsync(threadId);
            if (thread == null)
            {
                return GetThreadResult.Failure("Thread not found.");
            }

            return GetThreadResult.Success(thread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thread {ThreadId}", threadId);
            return GetThreadResult.Failure("An error occurred while getting the thread.");
        }
    }

    /// <inheritdoc />
    public async Task<GetThreadMessagesResult> GetThreadMessagesAsync(
        Guid threadId,
        string userId,
        bool isAdmin,
        int page,
        int pageSize)
    {
        var validationErrors = ValidateGetMessagesQuery(userId, page, pageSize);
        if (validationErrors.Count > 0)
        {
            return GetThreadMessagesResult.Failure(validationErrors);
        }

        if (threadId == Guid.Empty)
        {
            return GetThreadMessagesResult.Failure("Thread ID is required.");
        }

        try
        {
            var canAccess = await _threadRepository.CanAccessAsync(threadId, userId, isAdmin);
            if (!canAccess)
            {
                return GetThreadMessagesResult.NotAuthorized();
            }

            var (messages, totalCount) = await _messageRepository.GetByThreadIdAsync(threadId, page, pageSize);

            return GetThreadMessagesResult.Success(messages, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for thread {ThreadId}", threadId);
            return GetThreadMessagesResult.Failure("An error occurred while getting the messages.");
        }
    }

    /// <inheritdoc />
    public async Task<GetThreadsResult> GetBuyerThreadsAsync(string buyerId, int page, int pageSize)
    {
        var validationErrors = ValidateGetThreadsQuery(buyerId, page, pageSize);
        if (validationErrors.Count > 0)
        {
            return GetThreadsResult.Failure(validationErrors);
        }

        try
        {
            var (threads, totalCount) = await _threadRepository.GetByBuyerIdAsync(buyerId, page, pageSize);

            return GetThreadsResult.Success(threads, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting threads for buyer {BuyerId}", buyerId);
            return GetThreadsResult.Failure("An error occurred while getting the threads.");
        }
    }

    /// <inheritdoc />
    public async Task<GetThreadsResult> GetSellerThreadsAsync(string sellerId, int page, int pageSize)
    {
        var validationErrors = ValidateGetThreadsQuery(sellerId, page, pageSize);
        if (validationErrors.Count > 0)
        {
            return GetThreadsResult.Failure(validationErrors);
        }

        try
        {
            var (threads, totalCount) = await _threadRepository.GetBySellerIdAsync(sellerId, page, pageSize);

            return GetThreadsResult.Success(threads, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting threads for seller {SellerId}", sellerId);
            return GetThreadsResult.Failure("An error occurred while getting the threads.");
        }
    }

    /// <inheritdoc />
    public async Task<GetThreadsResult> GetAllThreadsAsync(int page, int pageSize)
    {
        var validationErrors = ValidatePagination(page, pageSize);
        if (validationErrors.Count > 0)
        {
            return GetThreadsResult.Failure(validationErrors);
        }

        try
        {
            var (threads, totalCount) = await _threadRepository.GetAllAsync(page, pageSize);

            return GetThreadsResult.Success(threads, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all threads");
            return GetThreadsResult.Failure("An error occurred while getting the threads.");
        }
    }

    /// <inheritdoc />
    public async Task<GetThreadsResult> GetProductQuestionsAsync(Guid productId, int page, int pageSize)
    {
        var validationErrors = ValidatePagination(page, pageSize);
        if (validationErrors.Count > 0)
        {
            return GetThreadsResult.Failure(validationErrors);
        }

        if (productId == Guid.Empty)
        {
            return GetThreadsResult.Failure("Product ID is required.");
        }

        try
        {
            var (threads, totalCount) = await _threadRepository.GetByProductIdAsync(productId, page, pageSize);

            return GetThreadsResult.Success(threads, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product questions for product {ProductId}", productId);
            return GetThreadsResult.Failure("An error occurred while getting the product questions.");
        }
    }

    /// <inheritdoc />
    public async Task<CloseThreadResult> CloseThreadAsync(Guid threadId, string userId, bool isAdmin)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return CloseThreadResult.Failure("User ID is required.");
        }

        if (threadId == Guid.Empty)
        {
            return CloseThreadResult.Failure("Thread ID is required.");
        }

        try
        {
            var canAccess = await _threadRepository.CanAccessAsync(threadId, userId, isAdmin);
            if (!canAccess)
            {
                return CloseThreadResult.NotAuthorized();
            }

            var thread = await _threadRepository.GetByIdAsync(threadId);
            if (thread == null)
            {
                return CloseThreadResult.Failure("Thread not found.");
            }

            if (thread.IsClosed)
            {
                return CloseThreadResult.Failure("Thread is already closed.");
            }

            thread.IsClosed = true;
            thread.ClosedAt = DateTimeOffset.UtcNow;
            await _threadRepository.UpdateAsync(thread);

            _logger.LogInformation("Closed thread {ThreadId} by user {UserId}", threadId, userId);

            return CloseThreadResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing thread {ThreadId}", threadId);
            return CloseThreadResult.Failure("An error occurred while closing the thread.");
        }
    }

    /// <inheritdoc />
    public async Task<MarkMessageAsReadResult> MarkMessageAsReadAsync(Guid messageId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return MarkMessageAsReadResult.Failure("User ID is required.");
        }

        if (messageId == Guid.Empty)
        {
            return MarkMessageAsReadResult.Failure("Message ID is required.");
        }

        try
        {
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null)
            {
                return MarkMessageAsReadResult.Failure("Message not found.");
            }

            // Check if user is the recipient (not the sender)
            var thread = message.Thread;
            if (thread == null)
            {
                return MarkMessageAsReadResult.Failure("Thread not found.");
            }

            var isParticipant = thread.BuyerId == userId || thread.SellerId == userId;
            if (!isParticipant)
            {
                return MarkMessageAsReadResult.NotAuthorized();
            }

            // If the user is the sender, marking as read is a no-op (success without action)
            if (message.SenderId == userId)
            {
                return MarkMessageAsReadResult.Success();
            }

            var success = await _messageRepository.MarkAsReadAsync(messageId, userId);
            if (!success)
            {
                return MarkMessageAsReadResult.Failure("Failed to mark message as read.");
            }

            _logger.LogDebug("Marked message {MessageId} as read for user {UserId}", messageId, userId);
            return MarkMessageAsReadResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
            return MarkMessageAsReadResult.Failure("An error occurred while marking the message as read.");
        }
    }

    private async Task SendNewMessageNotificationAsync(
        string recipientId,
        string senderId,
        string subject,
        Guid threadId)
    {
        try
        {
            var command = new CreateNotificationCommand
            {
                UserId = recipientId,
                Title = "New Message",
                Message = $"You have a new message: {subject}",
                Type = NotificationType.Message,
                RelatedEntityId = threadId,
                RelatedUrl = $"/Messaging/Thread/{threadId}"
            };

            await _notificationService.CreateNotificationAsync(command);
        }
        catch (Exception ex)
        {
            // Log but don't fail the operation if notification fails
            _logger.LogWarning(ex, "Failed to send notification for new message in thread {ThreadId}", threadId);
        }
    }

    private static List<string> ValidateCreateThreadCommand(CreateMessageThreadCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (string.IsNullOrEmpty(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrEmpty(command.Subject))
        {
            errors.Add("Subject is required.");
        }
        else if (command.Subject.Length > 200)
        {
            errors.Add("Subject must not exceed 200 characters.");
        }

        if (string.IsNullOrEmpty(command.InitialMessage))
        {
            errors.Add("Initial message is required.");
        }
        else if (command.InitialMessage.Length > 4000)
        {
            errors.Add("Initial message must not exceed 4000 characters.");
        }

        // For product questions, ProductId is required
        if (command.ThreadType == MessageThreadType.ProductQuestion && command.ProductId == null)
        {
            errors.Add("Product ID is required for product questions.");
        }

        // For order messages, OrderId is required
        if (command.ThreadType == MessageThreadType.OrderMessage && command.OrderId == null)
        {
            errors.Add("Order ID is required for order messages.");
        }

        return errors;
    }

    private static List<string> ValidateSendMessageCommand(SendMessageCommand command)
    {
        var errors = new List<string>();

        if (command.ThreadId == Guid.Empty)
        {
            errors.Add("Thread ID is required.");
        }

        if (string.IsNullOrEmpty(command.SenderId))
        {
            errors.Add("Sender ID is required.");
        }

        if (string.IsNullOrEmpty(command.Content))
        {
            errors.Add("Message content is required.");
        }
        else if (command.Content.Length > 4000)
        {
            errors.Add("Message content must not exceed 4000 characters.");
        }

        return errors;
    }

    private static List<string> ValidateGetMessagesQuery(string userId, int page, int pageSize)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(userId))
        {
            errors.Add("User ID is required.");
        }

        errors.AddRange(ValidatePagination(page, pageSize));

        return errors;
    }

    private static List<string> ValidateGetThreadsQuery(string userId, int page, int pageSize)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(userId))
        {
            errors.Add("User ID is required.");
        }

        errors.AddRange(ValidatePagination(page, pageSize));

        return errors;
    }

    private static List<string> ValidatePagination(int page, int pageSize)
    {
        var errors = new List<string>();

        if (page < 1)
        {
            errors.Add("Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100.");
        }

        return errors;
    }
}
