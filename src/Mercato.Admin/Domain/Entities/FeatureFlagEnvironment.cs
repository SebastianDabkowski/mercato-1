namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the target environment for a feature flag.
/// Feature flags can be configured differently for each environment.
/// </summary>
public enum FeatureFlagEnvironment
{
    /// <summary>
    /// Development environment for local testing and development.
    /// </summary>
    Development = 0,

    /// <summary>
    /// Testing environment for QA and integration testing.
    /// </summary>
    Testing = 1,

    /// <summary>
    /// Staging environment for pre-production validation.
    /// </summary>
    Staging = 2,

    /// <summary>
    /// Production environment for live users.
    /// </summary>
    Production = 3
}
