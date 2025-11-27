namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a password reset request.
/// </summary>
public class ForgotPasswordResult
{
    /// <summary>
    /// Gets a value indicating whether the request was processed.
    /// Note: For security reasons, this returns true even if the email doesn't exist
    /// to prevent email enumeration attacks.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the password reset token if the request was successful and the email exists.
    /// In production, this should be sent via email rather than returned directly.
    /// </summary>
    public string? ResetToken { get; init; }

    /// <summary>
    /// Gets the user's ID if found (for internal use only).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the error message if request failed (only for unexpected errors).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="resetToken">The password reset token.</param>
    /// <param name="userId">The user's ID.</param>
    public static ForgotPasswordResult Success(string resetToken, string userId)
    {
        return new ForgotPasswordResult
        {
            Succeeded = true,
            ResetToken = resetToken,
            UserId = userId
        };
    }

    /// <summary>
    /// Creates a result indicating the request was processed (email not found scenario).
    /// For security, we don't reveal if the email exists or not.
    /// </summary>
    public static ForgotPasswordResult EmailNotFound()
    {
        return new ForgotPasswordResult
        {
            Succeeded = true // Return success to prevent email enumeration
        };
    }

    /// <summary>
    /// Creates a failed result for rate limiting.
    /// </summary>
    public static ForgotPasswordResult RateLimited()
    {
        return new ForgotPasswordResult
        {
            Succeeded = false,
            ErrorMessage = "Too many password reset requests. Please try again later."
        };
    }

    /// <summary>
    /// Creates a failed result for unexpected errors.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public static ForgotPasswordResult Failure(string errorMessage)
    {
        return new ForgotPasswordResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }
}
