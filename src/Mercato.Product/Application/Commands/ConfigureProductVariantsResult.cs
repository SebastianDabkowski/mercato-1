namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of configuring product variants.
/// </summary>
public class ConfigureProductVariantsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the number of variants created or updated.
    /// </summary>
    public int VariantCount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="variantCount">The number of variants configured.</param>
    /// <returns>A successful result.</returns>
    public static ConfigureProductVariantsResult Success(int variantCount) => new()
    {
        Succeeded = true,
        Errors = [],
        VariantCount = variantCount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ConfigureProductVariantsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ConfigureProductVariantsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <param name="error">The authorization error message.</param>
    /// <returns>A not authorized result.</returns>
    public static ConfigureProductVariantsResult NotAuthorized(string error) => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = [error]
    };
}
