using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for escrow operations.
/// </summary>
public interface IEscrowService
{
    /// <summary>
    /// Creates escrow entries for a multi-seller order when payment is confirmed.
    /// </summary>
    /// <param name="command">The hold escrow command.</param>
    /// <returns>The result of the hold escrow operation.</returns>
    Task<HoldEscrowResult> HoldEscrowAsync(HoldEscrowCommand command);

    /// <summary>
    /// Releases escrow funds to the seller after order fulfillment.
    /// </summary>
    /// <param name="command">The release escrow command.</param>
    /// <returns>The result of the release escrow operation.</returns>
    Task<ReleaseEscrowResult> ReleaseEscrowAsync(ReleaseEscrowCommand command);

    /// <summary>
    /// Refunds escrow funds to the buyer when an order is cancelled.
    /// </summary>
    /// <param name="command">The refund escrow command.</param>
    /// <returns>The result of the refund escrow operation.</returns>
    Task<RefundEscrowResult> RefundEscrowAsync(RefundEscrowCommand command);

    /// <summary>
    /// Gets all escrow entries for an order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>The result containing escrow entries.</returns>
    Task<GetEscrowEntriesResult> GetEscrowEntriesByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets all escrow entries for a seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>The result containing escrow entries.</returns>
    Task<GetEscrowEntriesResult> GetEscrowEntriesBySellerIdAsync(Guid sellerId, EscrowStatus? status = null);
}

/// <summary>
/// Represents a seller allocation for escrow.
/// </summary>
public class SellerAllocation
{
    /// <summary>
    /// Gets or sets the seller's store ID.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the amount allocated to this seller.
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Command to hold funds in escrow for a multi-seller order.
/// </summary>
public class HoldEscrowCommand
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
    /// Gets or sets the seller allocations (amounts per seller).
    /// </summary>
    public IReadOnlyList<SellerAllocation> SellerAllocations { get; set; } = [];

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of holding funds in escrow.
/// </summary>
public class HoldEscrowResult
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
    /// Gets the created escrow entries.
    /// </summary>
    public IReadOnlyList<EscrowEntry> Entries { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with escrow entries.
    /// </summary>
    /// <param name="entries">The created escrow entries.</param>
    /// <returns>A successful result.</returns>
    public static HoldEscrowResult Success(IReadOnlyList<EscrowEntry> entries) => new()
    {
        Succeeded = true,
        Errors = [],
        Entries = entries
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static HoldEscrowResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static HoldEscrowResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static HoldEscrowResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to release escrow funds to a seller.
/// </summary>
public class ReleaseEscrowCommand
{
    /// <summary>
    /// Gets or sets the order ID to release escrow for.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID to release escrow for.
    /// If null, releases escrow for all sellers in the order.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of releasing escrow funds.
/// </summary>
public class ReleaseEscrowResult
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
    /// Gets the updated escrow entries.
    /// </summary>
    public IReadOnlyList<EscrowEntry> Entries { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with updated escrow entries.
    /// </summary>
    /// <param name="entries">The updated escrow entries.</param>
    /// <returns>A successful result.</returns>
    public static ReleaseEscrowResult Success(IReadOnlyList<EscrowEntry> entries) => new()
    {
        Succeeded = true,
        Errors = [],
        Entries = entries
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ReleaseEscrowResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ReleaseEscrowResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ReleaseEscrowResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to refund escrow funds to the buyer.
/// </summary>
public class RefundEscrowCommand
{
    /// <summary>
    /// Gets or sets the order ID to refund escrow for.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID to refund escrow for.
    /// If null, refunds escrow for all sellers in the order.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of refunding escrow funds.
/// </summary>
public class RefundEscrowResult
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
    /// Gets the updated escrow entries.
    /// </summary>
    public IReadOnlyList<EscrowEntry> Entries { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with updated escrow entries.
    /// </summary>
    /// <param name="entries">The updated escrow entries.</param>
    /// <returns>A successful result.</returns>
    public static RefundEscrowResult Success(IReadOnlyList<EscrowEntry> entries) => new()
    {
        Succeeded = true,
        Errors = [],
        Entries = entries
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RefundEscrowResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RefundEscrowResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RefundEscrowResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting escrow entries.
/// </summary>
public class GetEscrowEntriesResult
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
    /// Gets the escrow entries.
    /// </summary>
    public IReadOnlyList<EscrowEntry> Entries { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with escrow entries.
    /// </summary>
    /// <param name="entries">The escrow entries.</param>
    /// <returns>A successful result.</returns>
    public static GetEscrowEntriesResult Success(IReadOnlyList<EscrowEntry> entries) => new()
    {
        Succeeded = true,
        Errors = [],
        Entries = entries
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetEscrowEntriesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetEscrowEntriesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetEscrowEntriesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
