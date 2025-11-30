namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Query to get active consent types for display.
/// </summary>
public class GetConsentTypesQuery
{
    /// <summary>
    /// Gets or sets a value indicating whether to include only mandatory consents.
    /// </summary>
    public bool MandatoryOnly { get; set; }
}
