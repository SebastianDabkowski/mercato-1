namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command for recording an admin decision on an escalated case.
/// </summary>
public class RecordAdminDecisionCommand
{
    /// <summary>
    /// Gets or sets the case ID.
    /// </summary>
    public Guid CaseId { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID making the decision.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decision type (e.g., "OverrideSellerDecision", "EnforceRefund", "CloseWithoutAction").
    /// </summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the decision.
    /// </summary>
    public string DecisionReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status to set after the decision.
    /// </summary>
    public Mercato.Orders.Domain.Entities.ReturnStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the refund amount to enforce (optional, for EnforceRefund decision).
    /// </summary>
    public decimal? RefundAmount { get; set; }
}

/// <summary>
/// Result of a record admin decision operation.
/// </summary>
public class RecordAdminDecisionResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static RecordAdminDecisionResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RecordAdminDecisionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RecordAdminDecisionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RecordAdminDecisionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to make decisions on this case."]
    };
}
