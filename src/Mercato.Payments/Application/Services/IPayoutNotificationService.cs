using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for sending payout notification emails to sellers.
/// </summary>
public interface IPayoutNotificationService
{
    /// <summary>
    /// Sends a payout processed notification email to the seller.
    /// </summary>
    /// <param name="payout">The payout that was processed.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<PayoutNotificationResult> SendPayoutProcessedNotificationAsync(Payout payout, string sellerEmail);
}

/// <summary>
/// Result of sending a payout notification.
/// </summary>
public class PayoutNotificationResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static PayoutNotificationResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static PayoutNotificationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static PayoutNotificationResult Failure(string error) => Failure([error]);
}
