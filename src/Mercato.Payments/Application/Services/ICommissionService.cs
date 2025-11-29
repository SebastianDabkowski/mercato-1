using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for commission operations.
/// </summary>
public interface ICommissionService
{
    /// <summary>
    /// Calculates commission for a payment transaction at confirmation time.
    /// </summary>
    /// <param name="command">The calculate commission command.</param>
    /// <returns>The result of the commission calculation.</returns>
    Task<CalculateCommissionResult> CalculateCommissionAsync(CalculateCommissionCommand command);

    /// <summary>
    /// Recalculates commission for a partial refund.
    /// </summary>
    /// <param name="command">The recalculate partial refund command.</param>
    /// <returns>The result of the recalculation.</returns>
    Task<RecalculatePartialRefundResult> RecalculatePartialRefundAsync(RecalculatePartialRefundCommand command);

    /// <summary>
    /// Gets commission records by order ID.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>The result containing commission records.</returns>
    Task<GetCommissionRecordsResult> GetCommissionRecordsByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets commission records by seller ID.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The result containing commission records.</returns>
    Task<GetCommissionRecordsResult> GetCommissionRecordsBySellerIdAsync(Guid sellerId);
}

/// <summary>
/// Represents a seller allocation for commission calculation.
/// </summary>
public class CommissionSellerAllocation
{
    /// <summary>
    /// Gets or sets the seller's store ID.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the amount allocated to this seller.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the category ID for this seller's products (optional).
    /// </summary>
    public string? CategoryId { get; set; }
}

/// <summary>
/// Command to calculate commission for a payment transaction.
/// </summary>
public class CalculateCommissionCommand
{
    /// <summary>
    /// Gets or sets the payment transaction ID.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller allocations (amounts and categories per seller).
    /// </summary>
    public IReadOnlyList<CommissionSellerAllocation> SellerAllocations { get; set; } = [];
}

/// <summary>
/// Result of calculating commission for a payment transaction.
/// </summary>
public class CalculateCommissionResult
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
    /// Gets the created commission records.
    /// </summary>
    public IReadOnlyList<CommissionRecord> CommissionRecords { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with commission records.
    /// </summary>
    /// <param name="records">The created commission records.</param>
    /// <returns>A successful result.</returns>
    public static CalculateCommissionResult Success(IReadOnlyList<CommissionRecord> records) => new()
    {
        Succeeded = true,
        Errors = [],
        CommissionRecords = records
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CalculateCommissionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CalculateCommissionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CalculateCommissionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to recalculate commission for a partial refund.
/// </summary>
public class RecalculatePartialRefundCommand
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
    /// Gets or sets the refund amount.
    /// </summary>
    public decimal RefundAmount { get; set; }
}

/// <summary>
/// Result of recalculating commission for a partial refund.
/// </summary>
public class RecalculatePartialRefundResult
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
    /// Gets the updated commission record.
    /// </summary>
    public CommissionRecord? UpdatedRecord { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated record.
    /// </summary>
    /// <param name="record">The updated commission record.</param>
    /// <returns>A successful result.</returns>
    public static RecalculatePartialRefundResult Success(CommissionRecord record) => new()
    {
        Succeeded = true,
        Errors = [],
        UpdatedRecord = record
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RecalculatePartialRefundResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RecalculatePartialRefundResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RecalculatePartialRefundResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting commission records.
/// </summary>
public class GetCommissionRecordsResult
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
    /// Gets the commission records.
    /// </summary>
    public IReadOnlyList<CommissionRecord> Records { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with commission records.
    /// </summary>
    /// <param name="records">The commission records.</param>
    /// <returns>A successful result.</returns>
    public static GetCommissionRecordsResult Success(IReadOnlyList<CommissionRecord> records) => new()
    {
        Succeeded = true,
        Errors = [],
        Records = records
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionRecordsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionRecordsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCommissionRecordsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
