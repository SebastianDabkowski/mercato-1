namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a validation error for a specific row in an import file.
/// </summary>
public class ProductImportRowError
{
    /// <summary>
    /// Gets or sets the unique identifier for this error.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the import job ID this error belongs to.
    /// </summary>
    public Guid ImportJobId { get; set; }

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

    /// <summary>
    /// Gets or sets the navigation property to the parent import job.
    /// </summary>
    public ProductImportJob? ImportJob { get; set; }
}
