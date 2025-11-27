namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of creating a product.
/// </summary>
public class CreateProductResult
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
    /// Gets the ID of the created product, if successful.
    /// </summary>
    public Guid? ProductId { get; private init; }

    /// <summary>
    /// Creates a successful result with the product ID.
    /// </summary>
    /// <param name="productId">The ID of the created product.</param>
    /// <returns>A successful result.</returns>
    public static CreateProductResult Success(Guid productId) => new()
    {
        Succeeded = true,
        Errors = [],
        ProductId = productId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateProductResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateProductResult Failure(string error) => Failure([error]);
}
