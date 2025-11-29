using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for payment operations.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Gets all available payment methods.
    /// </summary>
    /// <returns>The result containing available payment methods.</returns>
    Task<GetPaymentMethodsResult> GetPaymentMethodsAsync();

    /// <summary>
    /// Initiates a payment transaction.
    /// </summary>
    /// <param name="command">The initiate payment command.</param>
    /// <returns>The result of the payment initiation.</returns>
    Task<InitiatePaymentResult> InitiatePaymentAsync(InitiatePaymentCommand command);

    /// <summary>
    /// Processes a payment callback from the payment provider.
    /// </summary>
    /// <param name="command">The process callback command.</param>
    /// <returns>The result of the callback processing.</returns>
    Task<ProcessPaymentCallbackResult> ProcessPaymentCallbackAsync(ProcessPaymentCallbackCommand command);

    /// <summary>
    /// Gets a payment transaction by ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The payment transaction if found.</returns>
    Task<GetPaymentTransactionResult> GetTransactionAsync(Guid transactionId, string buyerId);

    /// <summary>
    /// Submits a BLIK code for a pending BLIK payment.
    /// </summary>
    /// <param name="command">The BLIK code submission command.</param>
    /// <returns>The result of the BLIK code submission.</returns>
    Task<SubmitBlikCodeResult> SubmitBlikCodeAsync(SubmitBlikCodeCommand command);
}

/// <summary>
/// Result of getting available payment methods.
/// </summary>
public class GetPaymentMethodsResult
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
    /// Gets the available payment methods.
    /// </summary>
    public IReadOnlyList<PaymentMethod> Methods { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with payment methods.
    /// </summary>
    /// <param name="methods">The available payment methods.</param>
    /// <returns>A successful result.</returns>
    public static GetPaymentMethodsResult Success(IReadOnlyList<PaymentMethod> methods) => new()
    {
        Succeeded = true,
        Errors = [],
        Methods = methods
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetPaymentMethodsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetPaymentMethodsResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Command to initiate a payment.
/// </summary>
public class InitiatePaymentCommand
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment method ID.
    /// </summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the return URL after payment completion.
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cancel URL if payment is cancelled.
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BLIK code for BLIK payments (6 digits).
    /// </summary>
    public string? BlikCode { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key for provider retry handling.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Result of initiating a payment.
/// </summary>
public class InitiatePaymentResult
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
    /// Gets the transaction ID.
    /// </summary>
    public Guid TransactionId { get; private init; }

    /// <summary>
    /// Gets the redirect URL for the payment provider.
    /// </summary>
    public string? RedirectUrl { get; private init; }

    /// <summary>
    /// Gets a value indicating whether a redirect is required.
    /// </summary>
    public bool RequiresRedirect { get; private init; }

    /// <summary>
    /// Gets a value indicating whether BLIK code entry is required.
    /// </summary>
    public bool RequiresBlikCode { get; private init; }

    /// <summary>
    /// Creates a successful result with redirect URL.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="redirectUrl">The redirect URL.</param>
    /// <returns>A successful result.</returns>
    public static InitiatePaymentResult SuccessWithRedirect(Guid transactionId, string redirectUrl) => new()
    {
        Succeeded = true,
        Errors = [],
        TransactionId = transactionId,
        RedirectUrl = redirectUrl,
        RequiresRedirect = true
    };

    /// <summary>
    /// Creates a successful result without redirect (e.g., simulated payment).
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>A successful result.</returns>
    public static InitiatePaymentResult SuccessWithoutRedirect(Guid transactionId) => new()
    {
        Succeeded = true,
        Errors = [],
        TransactionId = transactionId,
        RequiresRedirect = false
    };

    /// <summary>
    /// Creates a result indicating BLIK code is required.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>A result requiring BLIK code entry.</returns>
    public static InitiatePaymentResult RequiresBlikCodeEntry(Guid transactionId) => new()
    {
        Succeeded = true,
        Errors = [],
        TransactionId = transactionId,
        RequiresRedirect = false,
        RequiresBlikCode = true
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static InitiatePaymentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static InitiatePaymentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static InitiatePaymentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to process a payment callback.
/// </summary>
public class ProcessPaymentCallbackCommand
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID for authorization.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the payment was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the external reference ID from the provider.
    /// </summary>
    public string? ExternalReferenceId { get; set; }
}

/// <summary>
/// Result of processing a payment callback.
/// </summary>
public class ProcessPaymentCallbackResult
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
    /// Gets the updated payment transaction.
    /// </summary>
    public PaymentTransaction? Transaction { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="transaction">The updated transaction.</param>
    /// <returns>A successful result.</returns>
    public static ProcessPaymentCallbackResult Success(PaymentTransaction transaction) => new()
    {
        Succeeded = true,
        Errors = [],
        Transaction = transaction
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ProcessPaymentCallbackResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ProcessPaymentCallbackResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ProcessPaymentCallbackResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a payment transaction.
/// </summary>
public class GetPaymentTransactionResult
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
    /// Gets the payment transaction.
    /// </summary>
    public PaymentTransaction? Transaction { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <returns>A successful result.</returns>
    public static GetPaymentTransactionResult Success(PaymentTransaction transaction) => new()
    {
        Succeeded = true,
        Errors = [],
        Transaction = transaction
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetPaymentTransactionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetPaymentTransactionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetPaymentTransactionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to submit a BLIK code for payment.
/// </summary>
public class SubmitBlikCodeCommand
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID for authorization.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BLIK code (6 digits).
    /// </summary>
    public string BlikCode { get; set; } = string.Empty;
}

/// <summary>
/// Result of submitting a BLIK code.
/// </summary>
public class SubmitBlikCodeResult
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
    /// Gets the updated payment transaction.
    /// </summary>
    public PaymentTransaction? Transaction { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the payment completed successfully.
    /// </summary>
    public bool IsPaymentSuccessful { get; private init; }

    /// <summary>
    /// Creates a successful result with completed payment.
    /// </summary>
    /// <param name="transaction">The updated transaction.</param>
    /// <returns>A successful result.</returns>
    public static SubmitBlikCodeResult Success(PaymentTransaction transaction) => new()
    {
        Succeeded = true,
        Errors = [],
        Transaction = transaction,
        IsPaymentSuccessful = transaction.Status == PaymentStatus.Completed
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SubmitBlikCodeResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SubmitBlikCodeResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static SubmitBlikCodeResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
