namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents the type of a category attribute.
/// </summary>
public enum CategoryAttributeType
{
    /// <summary>
    /// A free-form text field.
    /// </summary>
    Text = 0,

    /// <summary>
    /// A numeric field.
    /// </summary>
    Number = 1,

    /// <summary>
    /// A selection from a predefined list of options.
    /// </summary>
    List = 2
}
