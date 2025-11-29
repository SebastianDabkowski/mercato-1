namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Command for adding a new message to a case.
/// </summary>
public class AddCaseMessageCommand
{
    /// <summary>
    /// Gets or sets the return request ID to add the message to.
    /// </summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the message sender.
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
}

/// <summary>
/// Result of adding a message to a case.
/// </summary>
public class AddCaseMessageResult
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
    /// Gets the ID of the created message.
    /// </summary>
    public Guid? MessageId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="messageId">The ID of the created message.</param>
    /// <returns>A successful result.</returns>
    public static AddCaseMessageResult Success(Guid messageId) => new()
    {
        Succeeded = true,
        Errors = [],
        MessageId = messageId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static AddCaseMessageResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static AddCaseMessageResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static AddCaseMessageResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to add messages to this case."]
    };
}

/// <summary>
/// Result of marking case activity as viewed.
/// </summary>
public class MarkCaseActivityViewedResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static MarkCaseActivityViewedResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static MarkCaseActivityViewedResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static MarkCaseActivityViewedResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static MarkCaseActivityViewedResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to mark this case activity as viewed."]
    };
}
