namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a VAT (Value Added Tax) rule configuration for a specific country and optional category.
/// VAT rules define tax rates that are applied to transactions based on country, category, and effective dates.
/// </summary>
public class VatRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the VAT rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the descriptive name of this VAT rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code this rule applies to.
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax rate as a percentage (e.g., 20.0 for 20% VAT).
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Gets or sets the optional category ID this rule applies to.
    /// If null, the rule applies to all categories in the country.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this rule becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the optional date and time when this rule expires.
    /// If null, the rule has no expiration date.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// Gets or sets the priority of this rule.
    /// Higher priority rules take precedence when multiple rules match.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// Inactive rules are not applied to transactions.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this rule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this rule.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this rule was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this rule.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
