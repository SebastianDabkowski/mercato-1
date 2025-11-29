using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Queries;

/// <summary>
/// Data transfer object for a case message.
/// </summary>
public class CaseMessageDto
{
    /// <summary>
    /// Gets or sets the message ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the sender.
    /// </summary>
    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the sender (Buyer, Seller, Admin).
    /// </summary>
    public string SenderRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Result of getting case messages.
/// </summary>
public class GetCaseMessagesResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the list of messages.
    /// </summary>
    public IReadOnlyList<CaseMessageDto> Messages { get; private init; } = [];

    /// <summary>
    /// Gets the return request for context.
    /// </summary>
    public ReturnRequest? ReturnRequest { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="messages">The messages.</param>
    /// <param name="returnRequest">The return request.</param>
    /// <returns>A successful result.</returns>
    public static GetCaseMessagesResult Success(IReadOnlyList<CaseMessageDto> messages, ReturnRequest returnRequest) => new()
    {
        Succeeded = true,
        Errors = [],
        Messages = messages,
        ReturnRequest = returnRequest
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCaseMessagesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCaseMessagesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCaseMessagesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this case's messages."]
    };
}
