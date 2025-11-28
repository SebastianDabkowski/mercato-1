namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of deleting a product image.
/// </summary>
public class DeleteProductImageResult
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
    /// Gets a value indicating whether the failure was due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static DeleteProductImageResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DeleteProductImageResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DeleteProductImageResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result with the specified error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A not authorized result.</returns>
    public static DeleteProductImageResult NotAuthorized(string error) => new()
    {
        Succeeded = false,
        Errors = [error],
        IsNotAuthorized = true
    };
}
