namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents information about a linked social login.
/// </summary>
public class LinkedAccountInfo
{
    /// <summary>
    /// Gets or sets the login provider name (e.g., "Google", "Facebook").
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider key (unique ID from the provider).
    /// </summary>
    public string ProviderKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the provider.
    /// </summary>
    public string ProviderDisplayName { get; init; } = string.Empty;
}
