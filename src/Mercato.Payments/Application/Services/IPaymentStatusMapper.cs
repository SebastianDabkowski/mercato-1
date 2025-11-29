using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for mapping external payment provider status codes to internal payment statuses.
/// </summary>
public interface IPaymentStatusMapper
{
    /// <summary>
    /// Maps an external provider status code to an internal payment status.
    /// </summary>
    /// <param name="providerCode">The external provider status code.</param>
    /// <returns>The result containing the mapped internal payment status.</returns>
    PaymentStatusMappingResult MapProviderStatus(string providerCode);

    /// <summary>
    /// Gets a buyer-friendly message for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>A buyer-friendly message describing the status.</returns>
    string GetBuyerFriendlyMessage(PaymentStatus status);

    /// <summary>
    /// Gets a buyer-friendly error message (without technical details).
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <param name="internalError">The internal error message (will not be exposed).</param>
    /// <returns>A buyer-friendly error message.</returns>
    string GetBuyerFriendlyErrorMessage(PaymentStatus status, string? internalError = null);
}

/// <summary>
/// Result of mapping a provider status code to an internal payment status.
/// </summary>
public class PaymentStatusMappingResult
{
    /// <summary>
    /// Gets a value indicating whether the mapping succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the mapped payment status.
    /// </summary>
    public PaymentStatus Status { get; private init; }

    /// <summary>
    /// Gets an optional error message if the mapping failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets a value indicating whether this is an error status that should be logged.
    /// </summary>
    public bool IsErrorStatus { get; private init; }

    /// <summary>
    /// Creates a successful mapping result.
    /// </summary>
    /// <param name="status">The mapped status.</param>
    /// <param name="isErrorStatus">Whether this is an error status.</param>
    /// <returns>A successful result.</returns>
    public static PaymentStatusMappingResult Success(PaymentStatus status, bool isErrorStatus = false) => new()
    {
        Succeeded = true,
        Status = status,
        IsErrorStatus = isErrorStatus
    };

    /// <summary>
    /// Creates a failed mapping result for an unknown provider code.
    /// </summary>
    /// <param name="providerCode">The unknown provider code.</param>
    /// <returns>A failed result.</returns>
    public static PaymentStatusMappingResult Unknown(string providerCode) => new()
    {
        Succeeded = false,
        Status = PaymentStatus.Pending,
        ErrorMessage = $"Unknown provider status code: {providerCode}"
    };
}
