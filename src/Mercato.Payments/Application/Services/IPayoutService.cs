using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for payout operations.
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// Schedules payouts for eligible sellers based on released escrow entries.
    /// </summary>
    /// <param name="command">The schedule payouts command.</param>
    /// <returns>The result of the schedule payouts operation.</returns>
    Task<SchedulePayoutsResult> SchedulePayoutsAsync(SchedulePayoutsCommand command);

    /// <summary>
    /// Processes scheduled payouts.
    /// </summary>
    /// <param name="command">The process payouts command.</param>
    /// <returns>The result of the process payouts operation.</returns>
    Task<ProcessPayoutsResult> ProcessPayoutsAsync(ProcessPayoutsCommand command);

    /// <summary>
    /// Retries failed payouts that are eligible for retry.
    /// </summary>
    /// <param name="command">The retry payouts command.</param>
    /// <returns>The result of the retry payouts operation.</returns>
    Task<RetryPayoutsResult> RetryPayoutsAsync(RetryPayoutsCommand command);

    /// <summary>
    /// Gets a payout by its identifier.
    /// </summary>
    /// <param name="payoutId">The payout identifier.</param>
    /// <returns>The result containing the payout.</returns>
    Task<GetPayoutResult> GetPayoutAsync(Guid payoutId);

    /// <summary>
    /// Gets all payouts for a seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>The result containing payouts.</returns>
    Task<GetPayoutsResult> GetPayoutsBySellerIdAsync(Guid sellerId, PayoutStatus? status = null);
}

/// <summary>
/// Command to schedule payouts for eligible sellers.
/// </summary>
public class SchedulePayoutsCommand
{
    /// <summary>
    /// Gets or sets the scheduled date for the payouts.
    /// </summary>
    public DateTimeOffset ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets the payout schedule frequency.
    /// </summary>
    public PayoutScheduleFrequency ScheduleFrequency { get; set; } = PayoutScheduleFrequency.Weekly;

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of scheduling payouts.
/// </summary>
public class SchedulePayoutsResult
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
    /// Gets the scheduled payouts.
    /// </summary>
    public IReadOnlyList<Payout> Payouts { get; private init; } = [];

    /// <summary>
    /// Gets the number of sellers with below-threshold balances that rolled over.
    /// </summary>
    public int RolledOverCount { get; private init; }

    /// <summary>
    /// Creates a successful result with scheduled payouts.
    /// </summary>
    /// <param name="payouts">The scheduled payouts.</param>
    /// <param name="rolledOverCount">The number of rolled over balances.</param>
    /// <returns>A successful result.</returns>
    public static SchedulePayoutsResult Success(IReadOnlyList<Payout> payouts, int rolledOverCount = 0) => new()
    {
        Succeeded = true,
        Errors = [],
        Payouts = payouts,
        RolledOverCount = rolledOverCount
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SchedulePayoutsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SchedulePayoutsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static SchedulePayoutsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to process scheduled payouts.
/// </summary>
public class ProcessPayoutsCommand
{
    /// <summary>
    /// Gets or sets the date/time to process payouts scheduled before.
    /// If not specified, processes all scheduled payouts up to now.
    /// </summary>
    public DateTimeOffset? ProcessBefore { get; set; }

    /// <summary>
    /// Gets or sets whether to create a batch for processing.
    /// </summary>
    public bool CreateBatch { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of processing payouts.
/// </summary>
public class ProcessPayoutsResult
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
    /// Gets the processed payouts.
    /// </summary>
    public IReadOnlyList<Payout> Payouts { get; private init; } = [];

    /// <summary>
    /// Gets the batch ID if batching was used.
    /// </summary>
    public Guid? BatchId { get; private init; }

    /// <summary>
    /// Gets the count of successfully processed payouts.
    /// </summary>
    public int SuccessCount { get; private init; }

    /// <summary>
    /// Gets the count of failed payouts.
    /// </summary>
    public int FailedCount { get; private init; }

    /// <summary>
    /// Creates a successful result with processed payouts.
    /// </summary>
    /// <param name="payouts">The processed payouts.</param>
    /// <param name="batchId">The batch ID if batching was used.</param>
    /// <param name="successCount">The count of successfully processed payouts.</param>
    /// <param name="failedCount">The count of failed payouts.</param>
    /// <returns>A successful result.</returns>
    public static ProcessPayoutsResult Success(
        IReadOnlyList<Payout> payouts,
        Guid? batchId = null,
        int successCount = 0,
        int failedCount = 0) => new()
    {
        Succeeded = true,
        Errors = [],
        Payouts = payouts,
        BatchId = batchId,
        SuccessCount = successCount,
        FailedCount = failedCount
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ProcessPayoutsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ProcessPayoutsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ProcessPayoutsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to retry failed payouts.
/// </summary>
public class RetryPayoutsCommand
{
    /// <summary>
    /// Gets or sets a specific payout ID to retry.
    /// If not specified, retries all eligible failed payouts.
    /// </summary>
    public Guid? PayoutId { get; set; }

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of retrying payouts.
/// </summary>
public class RetryPayoutsResult
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
    /// Gets the retried payouts.
    /// </summary>
    public IReadOnlyList<Payout> Payouts { get; private init; } = [];

    /// <summary>
    /// Gets the count of successfully retried payouts.
    /// </summary>
    public int SuccessCount { get; private init; }

    /// <summary>
    /// Gets the count of payouts that failed again.
    /// </summary>
    public int FailedCount { get; private init; }

    /// <summary>
    /// Creates a successful result with retried payouts.
    /// </summary>
    /// <param name="payouts">The retried payouts.</param>
    /// <param name="successCount">The count of successfully retried payouts.</param>
    /// <param name="failedCount">The count of payouts that failed again.</param>
    /// <returns>A successful result.</returns>
    public static RetryPayoutsResult Success(
        IReadOnlyList<Payout> payouts,
        int successCount = 0,
        int failedCount = 0) => new()
    {
        Succeeded = true,
        Errors = [],
        Payouts = payouts,
        SuccessCount = successCount,
        FailedCount = failedCount
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RetryPayoutsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RetryPayoutsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RetryPayoutsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single payout.
/// </summary>
public class GetPayoutResult
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
    /// Gets the payout.
    /// </summary>
    public Payout? Payout { get; private init; }

    /// <summary>
    /// Creates a successful result with the payout.
    /// </summary>
    /// <param name="payout">The payout.</param>
    /// <returns>A successful result.</returns>
    public static GetPayoutResult Success(Payout payout) => new()
    {
        Succeeded = true,
        Errors = [],
        Payout = payout
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetPayoutResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetPayoutResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetPayoutResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting multiple payouts.
/// </summary>
public class GetPayoutsResult
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
    /// Gets the payouts.
    /// </summary>
    public IReadOnlyList<Payout> Payouts { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with payouts.
    /// </summary>
    /// <param name="payouts">The payouts.</param>
    /// <returns>A successful result.</returns>
    public static GetPayoutsResult Success(IReadOnlyList<Payout> payouts) => new()
    {
        Succeeded = true,
        Errors = [],
        Payouts = payouts
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetPayoutsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetPayoutsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetPayoutsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
