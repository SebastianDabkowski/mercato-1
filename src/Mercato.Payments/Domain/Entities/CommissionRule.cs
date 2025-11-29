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
    /// Gets or sets the date and time when this rule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this rule was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

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

        return $"{scope} - {CommissionRate:F4}%{constraints}";
    }
}
