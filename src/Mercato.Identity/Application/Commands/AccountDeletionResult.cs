namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of an account deletion operation.
/// </summary>
public class AccountDeletionResult
{
    /// <summary>
    /// Gets a value indicating whether the deletion was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user was not found.
    /// </summary>
    public bool IsUserNotFound { get; init; }

    /// <summary>
    /// Gets a value indicating whether deletion was blocked due to unresolved conditions.
    /// </summary>
    public bool IsBlocked { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the error messages if the deletion failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets the blocking conditions that prevent deletion.
    /// </summary>
    public IReadOnlyList<string> BlockingConditions { get; init; } = [];

    /// <summary>
    /// Gets the timestamp when the deletion was completed.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="deletedAt">The timestamp when deletion was completed.</param>
    public static AccountDeletionResult Success(DateTimeOffset deletedAt)
    {
        return new AccountDeletionResult
        {
            Succeeded = true,
            DeletedAt = deletedAt,
            Errors = []
        };
    }

    /// <summary>
    /// Creates a result indicating the user was not found.
    /// </summary>
    public static AccountDeletionResult UserNotFound()
    {
        return new AccountDeletionResult
        {
            Succeeded = false,
            IsUserNotFound = true,
            Errors = ["User not found."]
        };
    }

    /// <summary>
    /// Creates a result indicating deletion is blocked due to unresolved conditions.
    /// </summary>
    /// <param name="blockingConditions">The list of conditions blocking deletion.</param>
    public static AccountDeletionResult Blocked(IReadOnlyList<string> blockingConditions)
    {
        return new AccountDeletionResult
        {
            Succeeded = false,
            IsBlocked = true,
            BlockingConditions = blockingConditions,
            Errors = ["Account deletion is blocked due to unresolved conditions."]
        };
    }

    /// <summary>
    /// Creates a result indicating the operation was not authorized.
    /// </summary>
    public static AccountDeletionResult NotAuthorized()
    {
        return new AccountDeletionResult
        {
            Succeeded = false,
            IsNotAuthorized = true,
            Errors = ["You are not authorized to delete this account."]
        };
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static AccountDeletionResult Failure(string error)
    {
        return new AccountDeletionResult
        {
            Succeeded = false,
            Errors = [error]
        };
    }

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    public static AccountDeletionResult Failure(IReadOnlyList<string> errors)
    {
        return new AccountDeletionResult
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
