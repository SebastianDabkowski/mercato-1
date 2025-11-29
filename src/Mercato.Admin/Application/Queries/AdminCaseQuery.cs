using Mercato.Orders.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for filtering admin cases (return requests and complaints).
/// </summary>
public class AdminCaseFilterQuery
{
    /// <summary>
    /// Gets or sets the search term to filter by case number, buyer ID, or store name.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the statuses to filter by (optional). If empty, all statuses are included.
    /// </summary>
    public IReadOnlyList<ReturnStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the case types to filter by (optional). If empty, all types are included.
    /// </summary>
    public IReadOnlyList<CaseType> CaseTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Represents a summary of a case for the admin list view.
/// </summary>
public class AdminCaseSummary
{
    /// <summary>
    /// Gets or sets the case ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable case number.
    /// </summary>
    public string CaseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of case.
    /// </summary>
    public CaseType CaseType { get; set; }

    /// <summary>
    /// Gets or sets the current status of the case.
    /// </summary>
    public ReturnStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (alias for privacy).
    /// </summary>
    public string BuyerAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the case was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the age of the case in days.
    /// </summary>
    public int AgeDays => (int)(DateTimeOffset.UtcNow - CreatedAt).TotalDays;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this case is escalated.
    /// </summary>
    public bool IsEscalated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is new activity on this case.
    /// </summary>
    public bool HasNewActivity { get; set; }
}

/// <summary>
/// Result of a filtered admin cases query.
/// </summary>
public class GetAdminCasesResult
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
    /// Gets the list of case summaries for the current page.
    /// </summary>
    public IReadOnlyList<AdminCaseSummary> Cases { get; private init; } = [];

    /// <summary>
    /// Gets the total number of cases matching the filter criteria.
    /// </summary>
    public int TotalCount { get; private init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; private init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="cases">The case summaries for the current page.</param>
    /// <param name="totalCount">The total number of cases matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetAdminCasesResult Success(IReadOnlyList<AdminCaseSummary> cases, int totalCount, int page, int pageSize) => new()
    {
        Succeeded = true,
        Errors = [],
        Cases = cases,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminCasesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminCasesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetAdminCasesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access cases."]
    };
}
