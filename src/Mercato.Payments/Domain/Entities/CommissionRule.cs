namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a commission rule that defines commission rates for transactions.
/// </summary>
public class CommissionRule
{
    /// <summary>
    /// Gets or sets the unique identifier for this commission rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this commission rule for identification.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID this rule applies to.
    /// If null, the rule applies globally or by category.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the category ID this rule applies to.
    /// If null, the rule applies globally or by seller.
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the commission rate as a percentage (e.g., 10.5 for 10.5%).
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Gets or sets the optional fixed fee amount added to the commission.
    /// </summary>
    public decimal FixedFee { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum commission amount.
    /// </summary>
    public decimal? MinCommission { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum commission amount.
    /// </summary>
    public decimal? MaxCommission { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of this rule. Higher priority rules take precedence.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the effective date when this rule becomes applicable.
    /// Transactions on or after this date will use this rule if it matches.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this rule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this rule was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this rule.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last modified this rule.
    /// </summary>
    public string? LastModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the version number for optimistic concurrency and audit tracking.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets a human-readable description of this rule for auditing purposes.
    /// </summary>
    /// <returns>A description of the rule.</returns>
    public string GetDescription()
    {
        var scope = (SellerId, CategoryId) switch
        {
            (null, null) => "Global default",
            (not null, null) => $"Seller: {SellerId}",
            (null, not null) => $"Category: {CategoryId}",
            (not null, not null) => $"Seller: {SellerId}, Category: {CategoryId}"
        };

        var constraints = "";
        if (MinCommission.HasValue || MaxCommission.HasValue)
        {
            var min = MinCommission.HasValue ? $"min: {MinCommission:F2}" : "";
            var max = MaxCommission.HasValue ? $"max: {MaxCommission:F2}" : "";
            var separator = MinCommission.HasValue && MaxCommission.HasValue ? ", " : "";
            constraints = $" ({min}{separator}{max})";
        }

        var fixedFeeText = FixedFee > 0 ? $" + {FixedFee:F2} fixed" : "";
        return $"{scope} - {CommissionRate:F4}%{fixedFeeText}{constraints}";
    }

    /// <summary>
    /// Checks if two rules have overlapping applicability that could cause conflicts.
    /// </summary>
    /// <param name="other">The other rule to check against.</param>
    /// <returns>True if the rules overlap; otherwise, false.</returns>
    public bool OverlapsWith(CommissionRule other)
    {
        if (other == null || other.Id == Id)
        {
            return false;
        }

        // Check seller scope overlap
        var sellerOverlaps = (SellerId == null && other.SellerId == null) ||
                             (SellerId != null && other.SellerId != null && SellerId == other.SellerId) ||
                             (SellerId == null && other.SellerId != null) ||
                             (SellerId != null && other.SellerId == null);

        // Check category scope overlap
        var categoryOverlaps = (CategoryId == null && other.CategoryId == null) ||
                               (CategoryId != null && other.CategoryId != null && 
                                string.Equals(CategoryId, other.CategoryId, StringComparison.OrdinalIgnoreCase)) ||
                               (CategoryId == null && other.CategoryId != null) ||
                               (CategoryId != null && other.CategoryId == null);

        // Exact match on both seller and category scope creates a conflict
        var exactScopeMatch = SellerId == other.SellerId && 
                              string.Equals(CategoryId, other.CategoryId, StringComparison.OrdinalIgnoreCase);

        return exactScopeMatch;
    }
}
