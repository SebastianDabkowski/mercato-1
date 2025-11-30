namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Command to record a user's consent decision.
/// </summary>
public class RecordConsentCommand
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent version ID.
    /// </summary>
    public Guid ConsentVersionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent is granted.
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the consent was given.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }
}
