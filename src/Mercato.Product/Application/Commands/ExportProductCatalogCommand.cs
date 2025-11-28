namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for exporting a product catalog to CSV or Excel format.
/// </summary>
public class ExportProductCatalogCommand
{
    /// <summary>
    /// Gets or sets the store ID to export products from.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID requesting the export.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the export format (CSV or Excel).
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>
    /// Gets or sets whether to export only filtered products.
    /// When true, only products matching the search/filter criteria are exported.
    /// </summary>
    public bool ApplyFilters { get; set; }

    /// <summary>
    /// Gets or sets the search query to filter products by title or description.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    public string? CategoryFilter { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public Domain.Entities.ProductStatus? StatusFilter { get; set; }
}

/// <summary>
/// Export format options for product catalog export.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Comma-separated values format.
    /// </summary>
    Csv = 0,

    /// <summary>
    /// Microsoft Excel format (.xlsx).
    /// </summary>
    Excel = 1
}
