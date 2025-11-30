namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query model for filtering user accounts.
/// </summary>
public class UserAccountFilterQuery
{
    /// <summary>
    /// Gets or sets the role filter. If specified, only users with this role are returned.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the status filter. If specified, only users with this status are returned.
    /// </summary>
    public UserAccountStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the search term for filtering by email, name, or user ID.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
