using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for refund operations.
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Processes a full refund for an order.
    /// </summary>
    /// <param name="command">The full refund command.</param>
    /// <returns>The result of the full refund operation.</returns>
    Task<ProcessRefundResult> ProcessFullRefundAsync(ProcessFullRefundCommand command);

    /// <summary>
    /// Processes a partial refund for an order.
    /// </summary>
    /// <param name="command">The partial refund command.</param>
    /// <returns>The result of the partial refund operation.</returns>
    Task<ProcessRefundResult> ProcessPartialRefundAsync(ProcessPartialRefundCommand command);

    /// <summary>
    /// Gets a refund by its unique identifier.
    /// </summary>
    /// <param name="refundId">The refund identifier.</param>
    /// <returns>The refund result.</returns>
    Task<GetRefundResult> GetRefundAsync(Guid refundId);

    /// <summary>
    /// Gets all refunds for an order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>The refunds result.</returns>
    Task<GetRefundsResult> GetRefundsByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Checks if a seller can trigger a refund based on business rules.
    /// </summary>
    /// <param name="command">The refund eligibility check command.</param>
    /// <returns>The eligibility result.</returns>
    Task<RefundEligibilityResult> CheckSellerRefundEligibilityAsync(CheckRefundEligibilityCommand command);
}

/// <summary>
/// Command to process a full refund.
/// </summary>
public class ProcessFullRefundCommand
{
    /// <summary>
    /// Gets or sets the order ID to refund.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user initiating the refund.
    /// </summary>
    public string InitiatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user initiating the refund.
    /// </summary>
    public string InitiatedByRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Command to process a partial refund.
/// </summary>
public class ProcessPartialRefundCommand
{
    /// <summary>
    /// Gets or sets the order ID to refund.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID for seller-specific refunds.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user initiating the refund.
    /// </summary>
    public string InitiatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user initiating the refund.
    /// </summary>
    public string InitiatedByRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Command to check refund eligibility for a seller.
/// </summary>
public class CheckRefundEligibilityCommand
{
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the proposed refund amount.
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Result of processing a refund.
/// </summary>
public class ProcessRefundResult
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
    /// Gets the created refund.
    /// </summary>
    public Refund? Refund { get; private init; }

    /// <summary>
    /// Gets a value indicating whether provider errors occurred.
    /// </summary>
    public bool HasProviderErrors { get; private init; }

    /// <summary>
    /// Gets the provider error message if applicable.
    /// </summary>
    public string? ProviderErrorMessage { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="refund">The created refund.</param>
    /// <returns>A successful result.</returns>
    public static ProcessRefundResult Success(Refund refund) => new()
    {
        Succeeded = true,
        Errors = [],
        Refund = refund
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ProcessRefundResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ProcessRefundResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a failed result with provider error.
    /// </summary>
    /// <param name="providerError">The provider error message.</param>
    /// <param name="refund">The refund in failed state.</param>
    /// <returns>A failed result with provider error.</returns>
    public static ProcessRefundResult ProviderError(string providerError, Refund? refund = null) => new()
    {
        Succeeded = false,
        Errors = [providerError],
        HasProviderErrors = true,
        ProviderErrorMessage = providerError,
        Refund = refund
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ProcessRefundResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single refund.
/// </summary>
public class GetRefundResult
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
    /// Gets the refund.
    /// </summary>
    public Refund? Refund { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="refund">The refund.</param>
    /// <returns>A successful result.</returns>
    public static GetRefundResult Success(Refund refund) => new()
    {
        Succeeded = true,
        Errors = [],
        Refund = refund
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetRefundResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetRefundResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetRefundResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting refunds for an order.
/// </summary>
public class GetRefundsResult
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
    /// Gets the refunds.
    /// </summary>
    public IReadOnlyList<Refund> Refunds { get; private init; } = [];

    /// <summary>
    /// Gets the total refunded amount.
    /// </summary>
    public decimal TotalRefunded { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="refunds">The refunds.</param>
    /// <param name="totalRefunded">The total refunded amount.</param>
    /// <returns>A successful result.</returns>
    public static GetRefundsResult Success(IReadOnlyList<Refund> refunds, decimal totalRefunded) => new()
    {
        Succeeded = true,
        Errors = [],
        Refunds = refunds,
        TotalRefunded = totalRefunded
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetRefundsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetRefundsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetRefundsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of checking refund eligibility.
/// </summary>
public class RefundEligibilityResult
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
    /// Gets a value indicating whether the refund is eligible.
    /// </summary>
    public bool IsEligible { get; private init; }

    /// <summary>
    /// Gets the maximum refundable amount.
    /// </summary>
    public decimal MaxRefundableAmount { get; private init; }

    /// <summary>
    /// Gets the reason if not eligible.
    /// </summary>
    public string? IneligibilityReason { get; private init; }

    /// <summary>
    /// Creates a successful eligible result.
    /// </summary>
    /// <param name="maxRefundableAmount">The maximum refundable amount.</param>
    /// <returns>A successful eligible result.</returns>
    public static RefundEligibilityResult Eligible(decimal maxRefundableAmount) => new()
    {
        Succeeded = true,
        Errors = [],
        IsEligible = true,
        MaxRefundableAmount = maxRefundableAmount
    };

    /// <summary>
    /// Creates a successful ineligible result.
    /// </summary>
    /// <param name="reason">The reason for ineligibility.</param>
    /// <returns>A successful ineligible result.</returns>
    public static RefundEligibilityResult NotEligible(string reason) => new()
    {
        Succeeded = true,
        Errors = [],
        IsEligible = false,
        IneligibilityReason = reason,
        MaxRefundableAmount = 0m
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RefundEligibilityResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RefundEligibilityResult Failure(string error) => Failure([error]);
}
