namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Command to record multiple consent decisions at once (e.g., during registration).
/// </summary>
public class RecordMultipleConsentsCommand
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of consent decisions.
    /// </summary>
    public List<ConsentDecision> Consents { get; set; } = [];

    /// <summary>
    /// Gets or sets the IP address from which the consents were given.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Represents a single consent decision within a batch.
/// </summary>
public class ConsentDecision
{
    /// <summary>
    /// Gets or sets the consent type code.
    /// </summary>
    public string ConsentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether consent is granted.
    /// </summary>
    public bool IsGranted { get; set; }
}
