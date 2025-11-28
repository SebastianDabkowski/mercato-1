namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents an available payment method.
/// </summary>
public class PaymentMethod
{
    /// <summary>
    /// Gets or sets the unique identifier for the payment method.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the payment method.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the payment method.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon CSS class for the payment method.
    /// </summary>
    public string IconClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this payment method is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default payment method.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the sort order for display purposes.
    /// </summary>
    public int SortOrder { get; set; }
}
