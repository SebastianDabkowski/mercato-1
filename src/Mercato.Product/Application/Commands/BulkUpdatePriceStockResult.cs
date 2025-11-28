namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of a bulk update operation for price and stock.
/// </summary>
public class BulkUpdatePriceStockResult
{
    /// <summary>
    /// Gets a value indicating whether the overall operation succeeded.
    /// The operation succeeds if at least one product was updated.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of general errors that prevented the operation from starting.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the failure was due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the number of products successfully updated.
    /// </summary>
    public int SuccessCount { get; private init; }

    /// <summary>
    /// Gets the number of products that failed to update.
    /// </summary>
    public int FailureCount { get; private init; }

    /// <summary>
    /// Gets the details of products that failed to update.
    /// </summary>
    public IReadOnlyList<BulkUpdateProductFailure> FailedProducts { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="successCount">The number of products successfully updated.</param>
    /// <param name="failedProducts">Optional list of products that failed to update.</param>
    /// <returns>A successful result.</returns>
    public static BulkUpdatePriceStockResult Success(
        int successCount,
        IReadOnlyList<BulkUpdateProductFailure>? failedProducts = null) => new()
    {
        Succeeded = successCount > 0,
        Errors = [],
        SuccessCount = successCount,
        FailureCount = failedProducts?.Count ?? 0,
        FailedProducts = failedProducts ?? []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static BulkUpdatePriceStockResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors,
        SuccessCount = 0,
        FailureCount = 0,
        FailedProducts = []
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static BulkUpdatePriceStockResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a failed result for an authorization error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result with IsNotAuthorized set to true.</returns>
    public static BulkUpdatePriceStockResult NotAuthorized(string error) => new()
    {
        Succeeded = false,
        Errors = [error],
        IsNotAuthorized = true,
        SuccessCount = 0,
        FailureCount = 0,
        FailedProducts = []
    };
}

/// <summary>
/// Details about a product that failed to update during bulk operation.
/// </summary>
public class BulkUpdateProductFailure
{
    /// <summary>
    /// Gets or sets the product ID that failed.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title for display purposes.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message explaining why the update failed.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
