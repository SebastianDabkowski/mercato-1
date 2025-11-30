namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Query to get a user's current consents.
/// </summary>
public class GetUserConsentsQuery
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}
