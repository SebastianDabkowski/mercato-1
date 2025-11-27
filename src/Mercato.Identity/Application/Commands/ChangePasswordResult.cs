namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a password change operation.
/// </summary>
public class ChangePasswordResult
{
    /// <summary>
    /// Gets a value indicating whether the password change was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current password was incorrect.
    /// </summary>
    public bool IsIncorrectCurrentPassword { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user was not found.
    /// </summary>
    public bool IsUserNotFound { get; init; }

    /// <summary>
    /// Gets the error messages if the password change failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ChangePasswordResult Success()
    {
        return new ChangePasswordResult
        {
            Succeeded = true
        };
    }

    /// <summary>
    /// Creates a result indicating the current password was incorrect.
    /// </summary>
    public static ChangePasswordResult IncorrectCurrentPassword()
    {
        return new ChangePasswordResult
        {
            Succeeded = false,
            IsIncorrectCurrentPassword = true,
            Errors = ["The current password is incorrect."]
        };
    }

    /// <summary>
    /// Creates a result indicating the user was not found.
    /// </summary>
    public static ChangePasswordResult UserNotFound()
    {
        return new ChangePasswordResult
        {
            Succeeded = false,
            IsUserNotFound = true,
            Errors = ["User not found."]
        };
    }

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    public static ChangePasswordResult Failure(IEnumerable<string> errors)
    {
        return new ChangePasswordResult
        {
            Succeeded = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static ChangePasswordResult Failure(string error)
    {
        return new ChangePasswordResult
        {
            Succeeded = false,
            Errors = [error]
        };
    }
}
