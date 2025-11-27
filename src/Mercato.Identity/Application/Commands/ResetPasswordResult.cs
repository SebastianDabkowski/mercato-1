namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a password reset operation.
/// </summary>
public class ResetPasswordResult
{
    /// <summary>
    /// Gets a value indicating whether the password reset was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the token was invalid.
    /// </summary>
    public bool IsInvalidToken { get; init; }

    /// <summary>
    /// Gets a value indicating whether the token has expired.
    /// </summary>
    public bool IsExpiredToken { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user was not found.
    /// </summary>
    public bool IsUserNotFound { get; init; }

    /// <summary>
    /// Gets the error messages if the reset failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ResetPasswordResult Success()
    {
        return new ResetPasswordResult
        {
            Succeeded = true
        };
    }

    /// <summary>
    /// Creates a result indicating the token was invalid.
    /// </summary>
    public static ResetPasswordResult InvalidToken()
    {
        return new ResetPasswordResult
        {
            Succeeded = false,
            IsInvalidToken = true,
            Errors = ["The password reset token is invalid."]
        };
    }

    /// <summary>
    /// Creates a result indicating the token has expired.
    /// </summary>
    public static ResetPasswordResult ExpiredToken()
    {
        return new ResetPasswordResult
        {
            Succeeded = false,
            IsExpiredToken = true,
            Errors = ["The password reset token has expired."]
        };
    }

    /// <summary>
    /// Creates a result indicating the user was not found.
    /// </summary>
    public static ResetPasswordResult UserNotFound()
    {
        return new ResetPasswordResult
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
    public static ResetPasswordResult Failure(IEnumerable<string> errors)
    {
        return new ResetPasswordResult
        {
            Succeeded = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static ResetPasswordResult Failure(string error)
    {
        return new ResetPasswordResult
        {
            Succeeded = false,
            Errors = [error]
        };
    }
}
