namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of creating a category attribute.
/// </summary>
public class CreateCategoryAttributeResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the ID of the created attribute, if successful.
    /// </summary>
    public Guid? AttributeId { get; private init; }

    /// <summary>
    /// Creates a successful result with the attribute ID.
    /// </summary>
    /// <param name="attributeId">The ID of the created attribute.</param>
    /// <returns>A successful result.</returns>
    public static CreateCategoryAttributeResult Success(Guid attributeId) => new()
    {
        Succeeded = true,
        Errors = [],
        AttributeId = attributeId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateCategoryAttributeResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateCategoryAttributeResult Failure(string error) => Failure([error]);
}
