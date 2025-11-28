namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents the status of a product import job.
/// </summary>
public enum ProductImportStatus
{
    /// <summary>
    /// Job has been created but not yet started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being validated.
    /// </summary>
    Validating = 1,

    /// <summary>
    /// Job validation completed with errors, awaiting confirmation.
    /// </summary>
    ValidationFailed = 2,

    /// <summary>
    /// Job validation completed successfully, awaiting confirmation.
    /// </summary>
    AwaitingConfirmation = 3,

    /// <summary>
    /// Job is currently processing (importing products).
    /// </summary>
    Processing = 4,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Job completed with errors.
    /// </summary>
    CompletedWithErrors = 6,

    /// <summary>
    /// Job failed during processing.
    /// </summary>
    Failed = 7,

    /// <summary>
    /// Job was cancelled by the user.
    /// </summary>
    Cancelled = 8
}

/// <summary>
/// Represents a product import job.
/// </summary>
public class ProductImportJob
{
    /// <summary>
    /// Gets or sets the unique identifier for the import job.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this import job.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID who initiated the import.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the import job.
    /// </summary>
    public ProductImportStatus Status { get; set; } = ProductImportStatus.Pending;

    /// <summary>
    /// Gets or sets the total number of rows in the file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of rows that will create new products.
    /// </summary>
    public int NewProductsCount { get; set; }

    /// <summary>
    /// Gets or sets the number of rows that will update existing products.
    /// </summary>
    public int UpdatedProductsCount { get; set; }

    /// <summary>
    /// Gets or sets the number of rows with validation errors.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the number of rows successfully processed.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job was started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for row errors.
    /// </summary>
    public ICollection<ProductImportRowError> RowErrors { get; set; } = [];
}
