namespace Mercato.Buyer.Domain.Entities;

/// <summary>
/// Represents a type of consent that can be collected from users.
/// </summary>
public class ConsentType
{
    /// <summary>
    /// Gets or sets the unique identifier for the consent type.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the code identifying this consent type (e.g., "NEWSLETTER", "PROFILING", "THIRD_PARTY_SHARING").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for this consent type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what this consent type covers.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this consent type is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this consent is mandatory for registration.
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Gets or sets the display order for UI presentation.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this consent type was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the consent versions associated with this consent type.
    /// </summary>
    public ICollection<ConsentVersion> Versions { get; set; } = [];
}
