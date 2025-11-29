using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Application.Services;

/// <summary>
/// Service interface for messaging operations.
/// </summary>
public interface IMessagingService
{
    /// <summary>
    /// Creates a new message thread with an initial message.
    /// </summary>
    /// <param name="command">The create thread command.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<CreateMessageThreadResult> CreateThreadAsync(CreateMessageThreadCommand command);

    /// <summary>
    /// Sends a message in an existing thread.
    /// </summary>
    /// <param name="command">The send message command.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<SendMessageResult> SendMessageAsync(SendMessageCommand command);

    /// <summary>
    /// Gets a message thread with authorization check.
    /// </summary>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>A result containing the thread.</returns>
    Task<GetThreadResult> GetThreadAsync(Guid threadId, string userId, bool isAdmin);

    /// <summary>
    /// Gets messages for a thread with authorization check and pagination.
    /// </summary>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A result containing the messages.</returns>
    Task<GetThreadMessagesResult> GetThreadMessagesAsync(
        Guid threadId,
        string userId,
        bool isAdmin,
        int page,
        int pageSize);

    /// <summary>
    /// Gets message threads for a buyer with pagination.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A result containing the threads.</returns>
    Task<GetThreadsResult> GetBuyerThreadsAsync(string buyerId, int page, int pageSize);

    /// <summary>
    /// Gets message threads for a seller with pagination.
    /// </summary>
    /// <param name="sellerId">The seller ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A result containing the threads.</returns>
    Task<GetThreadsResult> GetSellerThreadsAsync(string sellerId, int page, int pageSize);

    /// <summary>
    /// Gets all message threads for admin moderation with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A result containing the threads.</returns>
    Task<GetThreadsResult> GetAllThreadsAsync(int page, int pageSize);

    /// <summary>
    /// Gets product questions (public Q&A) with pagination.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A result containing the threads.</returns>
    Task<GetThreadsResult> GetProductQuestionsAsync(Guid productId, int page, int pageSize);

    /// <summary>
    /// Closes a message thread.
    /// </summary>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<CloseThreadResult> CloseThreadAsync(Guid threadId, string userId, bool isAdmin);

    /// <summary>
    /// Marks a message as read.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<MarkMessageAsReadResult> MarkMessageAsReadAsync(Guid messageId, string userId);
}

/// <summary>
/// Result of the get thread operation.
/// </summary>
public class GetThreadResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the message thread.
    /// </summary>
    public MessageThread? Thread { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="thread">The thread.</param>
    /// <returns>A successful result.</returns>
    public static GetThreadResult Success(MessageThread thread) => new()
    {
        Succeeded = true,
        Thread = thread,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetThreadResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this thread."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetThreadResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}

/// <summary>
/// Result of the get thread messages operation.
/// </summary>
public class GetThreadMessagesResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of messages.
    /// </summary>
    public IReadOnlyList<Message> Messages { get; init; } = [];

    /// <summary>
    /// Gets the total count of messages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="messages">The messages.</param>
    /// <param name="totalCount">The total count.</param>
    /// <param name="page">The current page.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetThreadMessagesResult Success(
        IReadOnlyList<Message> messages,
        int totalCount,
        int page,
        int pageSize) => new()
    {
        Succeeded = true,
        Messages = messages,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetThreadMessagesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this thread."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetThreadMessagesResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static GetThreadMessagesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}

/// <summary>
/// Result of the get threads operation.
/// </summary>
public class GetThreadsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of threads.
    /// </summary>
    public IReadOnlyList<MessageThread> Threads { get; init; } = [];

    /// <summary>
    /// Gets the total count of threads.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="threads">The threads.</param>
    /// <param name="totalCount">The total count.</param>
    /// <param name="page">The current page.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetThreadsResult Success(
        IReadOnlyList<MessageThread> threads,
        int totalCount,
        int page,
        int pageSize) => new()
    {
        Succeeded = true,
        Threads = threads,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetThreadsResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static GetThreadsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}

/// <summary>
/// Result of the close thread operation.
/// </summary>
public class CloseThreadResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static CloseThreadResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CloseThreadResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to close this thread."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static CloseThreadResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}

/// <summary>
/// Result of the mark message as read operation.
/// </summary>
public class MarkMessageAsReadResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static MarkMessageAsReadResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static MarkMessageAsReadResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to mark this message as read."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static MarkMessageAsReadResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}
