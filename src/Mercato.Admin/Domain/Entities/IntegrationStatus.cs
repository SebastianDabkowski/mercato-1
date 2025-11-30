namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the current status of an integration.
/// </summary>
public enum IntegrationStatus
{
    /// <summary>
    /// Integration is active and functioning normally.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Integration is inactive or disabled.
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Integration is in an error state.
    /// </summary>
    Error = 2
}
