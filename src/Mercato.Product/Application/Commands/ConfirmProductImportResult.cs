namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of confirming and executing a product import.
/// </summary>
public class ConfirmProductImportResult
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
    /// Gets a value indicating whether the request was not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the number of products successfully created.
    /// </summary>
    public int CreatedCount { get; private init; }

    /// <summary>
    /// Gets the number of products successfully updated.
    /// </summary>
    public int UpdatedCount { get; private init; }

    /// <summary>
    /// Gets the number of rows that failed during import.
    /// </summary>
    public int FailedCount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ConfirmProductImportResult Success(int createdCount, int updatedCount, int failedCount) => new()
    {
        Succeeded = true,
        Errors = [],
        CreatedCount = createdCount,
        UpdatedCount = updatedCount,
        FailedCount = failedCount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    public static ConfirmProductImportResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    public static ConfirmProductImportResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    public static ConfirmProductImportResult NotAuthorized(string message) => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = [message]
    };
}
