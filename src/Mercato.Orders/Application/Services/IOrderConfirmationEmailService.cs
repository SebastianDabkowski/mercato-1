using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for sending order confirmation emails.
/// </summary>
public interface IOrderConfirmationEmailService
{
    /// <summary>
    /// Sends an order confirmation email to the buyer.
    /// </summary>
    /// <param name="order">The order to send confirmation for.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<SendEmailResult> SendOrderConfirmationAsync(Order order, string buyerEmail);
}

/// <summary>
/// Result of sending an email.
/// </summary>
public class SendEmailResult
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
    public static SendEmailResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SendEmailResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SendEmailResult Failure(string error) => Failure([error]);
}
