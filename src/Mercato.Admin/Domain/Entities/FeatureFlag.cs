namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a feature flag configuration for controlling feature availability.
/// Feature flags can be targeted to specific environments, user groups, or percentage rollouts.
/// </summary>
public class FeatureFlag
{
    /// <summary>
    /// Gets or sets the unique identifier for the feature flag.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique key identifier for the flag (e.g., "enable_new_checkout").
    /// This key is used in code to check feature availability.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the feature flag.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the feature flag.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this flag is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the target environment for this flag.
    /// </summary>
    public FeatureFlagEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the targeting type for this flag.
    /// </summary>
    public FeatureFlagTargetType TargetType { get; set; }

    /// <summary>
    /// Gets or sets the target value as JSON.
    /// For SpecificSellers: JSON array of seller IDs (e.g., ["seller-1", "seller-2"]).
    /// For PercentageRollout: percentage value as string (e.g., "25" for 25%).
    /// </summary>
    public string? TargetValue { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this flag was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this flag.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this flag was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this flag.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
