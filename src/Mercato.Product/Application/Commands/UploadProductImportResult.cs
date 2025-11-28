namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of uploading and validating a product import file.
/// </summary>
public class UploadProductImportResult
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
    /// Gets the import job ID if successful.
    /// </summary>
    public Guid? ImportJobId { get; private init; }

    /// <summary>
    /// Gets the total number of rows in the file.
    /// </summary>
    public int TotalRows { get; private init; }

    /// <summary>
    /// Gets the number of rows that will create new products.
    /// </summary>
    public int NewProductsCount { get; private init; }

    /// <summary>
    /// Gets the number of rows that will update existing products.
    /// </summary>
    public int UpdatedProductsCount { get; private init; }

    /// <summary>
    /// Gets the number of rows with validation errors.
    /// </summary>
    public int ErrorCount { get; private init; }

    /// <summary>
    /// Gets the validation errors per row.
    /// </summary>
    public IReadOnlyList<ProductImportRowValidationError> RowErrors { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with import summary.
    /// </summary>
    public static UploadProductImportResult Success(
        Guid importJobId,
        int totalRows,
        int newProductsCount,
        int updatedProductsCount,
        int errorCount,
        IReadOnlyList<ProductImportRowValidationError> rowErrors) => new()
    {
        Succeeded = errorCount == 0,
        Errors = [],
        ImportJobId = importJobId,
        TotalRows = totalRows,
        NewProductsCount = newProductsCount,
        UpdatedProductsCount = updatedProductsCount,
        ErrorCount = errorCount,
        RowErrors = rowErrors
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    public static UploadProductImportResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    public static UploadProductImportResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    public static UploadProductImportResult NotAuthorized(string message) => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = [message]
    };
}

/// <summary>
/// Represents a validation error for a specific row during import.
/// </summary>
public class ProductImportRowValidationError
{
    /// <summary>
    /// Gets or sets the row number in the file (1-based, excluding header).
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the column name where the error occurred.
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SKU of the product if available.
    /// </summary>
    public string? Sku { get; set; }
}
