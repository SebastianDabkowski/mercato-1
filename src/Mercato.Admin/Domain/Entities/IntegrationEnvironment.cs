namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the environment for an integration configuration.
/// </summary>
public enum IntegrationEnvironment
{
    /// <summary>
    /// Sandbox/testing environment.
    /// </summary>
    Sandbox = 0,

    /// <summary>
    /// Production environment.
    /// </summary>
    Production = 1
}
