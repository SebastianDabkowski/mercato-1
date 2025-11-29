using Mercato.Orders.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Detailed information about a case for the admin detail view.
/// </summary>
public class AdminCaseDetails
{
    /// <summary>
    /// Gets or sets the return request entity with all related data.
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of status history entries.
    /// </summary>
    public IReadOnlyList<CaseStatusHistory> StatusHistory { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of case messages.
    /// </summary>
    public IReadOnlyList<CaseMessage> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the refund information if linked.
    /// </summary>
    public CaseRefundInfo? RefundInfo { get; set; }
}

/// <summary>
/// Represents refund information linked to a case.
/// </summary>
public class CaseRefundInfo
{
    /// <summary>
    /// Gets or sets the refund ID.
    /// </summary>
    public Guid RefundId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the refund status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the refund was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the refund was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Result of a get case details query.
/// </summary>
public class GetAdminCaseDetailsResult
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
    /// Gets the case details.
    /// </summary>
    public AdminCaseDetails? CaseDetails { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="caseDetails">The case details.</param>
    /// <returns>A successful result.</returns>
    public static GetAdminCaseDetailsResult Success(AdminCaseDetails caseDetails) => new()
    {
        Succeeded = true,
        Errors = [],
        CaseDetails = caseDetails
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminCaseDetailsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminCaseDetailsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetAdminCaseDetailsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this case."]
    };
}
