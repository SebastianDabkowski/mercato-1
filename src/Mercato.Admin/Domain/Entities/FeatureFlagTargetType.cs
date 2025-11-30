namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the targeting type for a feature flag.
/// Determines how the flag is evaluated for different users or groups.
/// </summary>
public enum FeatureFlagTargetType
{
    /// <summary>
    /// No targeting rules. The flag applies globally based on IsEnabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// The flag applies to all users when enabled.
    /// </summary>
    AllUsers = 1,

    /// <summary>
    /// The flag applies only to internal users (e.g., employees, admins).
    /// </summary>
    InternalUsers = 2,

    /// <summary>
    /// The flag applies to specific sellers identified by their IDs.
    /// </summary>
    SpecificSellers = 3,

    /// <summary>
    /// The flag applies to a percentage of users based on a hash of their ID.
    /// </summary>
    PercentageRollout = 4
}
