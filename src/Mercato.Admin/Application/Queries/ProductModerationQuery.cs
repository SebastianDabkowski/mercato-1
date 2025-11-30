using Mercato.Product.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for filtering products in the moderation queue.
/// </summary>
public class ProductModerationFilterQuery
{
    /// <summary>
    /// Gets or sets the search term to filter by title or description.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the moderation statuses to filter by (optional). If empty, all statuses are included.
    /// </summary>
    public IReadOnlyList<ProductModerationStatus> ModerationStatuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the category to filter by.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Represents a summary of a product for the admin moderation list view.
/// </summary>
public class ProductModerationSummary
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns this product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name (populated from seller module).
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a truncated preview of the product description.
    /// </summary>
    public string DescriptionPreview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product status.
    /// </summary>
    public ProductStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the moderation status.
    /// </summary>
    public ProductModerationStatus ModerationStatus { get; set; }

    /// <summary>
    /// Gets or sets when the product was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the product was last moderated (if applicable).
    /// </summary>
    public DateTimeOffset? ModeratedAt { get; set; }

    /// <summary>
    /// Gets or sets the first image URL if available.
    /// </summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Represents detailed information about a product for admin moderation.
/// </summary>
public class ProductModerationDetails
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product status.
    /// </summary>
    public ProductStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the moderation status.
    /// </summary>
    public ProductModerationStatus ModerationStatus { get; set; }

    /// <summary>
    /// Gets or sets the moderation reason (for rejected products).
    /// </summary>
    public string? ModerationReason { get; set; }

    /// <summary>
    /// Gets or sets when the product was last moderated.
    /// </summary>
    public DateTimeOffset? ModeratedAt { get; set; }

    /// <summary>
    /// Gets or sets who last moderated the product.
    /// </summary>
    public string? ModeratedBy { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product images as a JSON array.
    /// </summary>
    public string? Images { get; set; }

    /// <summary>
    /// Gets or sets the parsed image URLs.
    /// </summary>
    public IReadOnlyList<string> ImageUrls { get; set; } = [];

    /// <summary>
    /// Gets or sets when the product was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the product was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the moderation history for this product.
    /// </summary>
    public IReadOnlyList<ProductModerationHistoryEntry> ModerationHistory { get; set; } = [];
}

/// <summary>
/// Represents a single entry in the moderation history.
/// </summary>
public class ProductModerationHistoryEntry
{
    /// <summary>
    /// Gets or sets the decision ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who made the decision.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decision made (Approved or Rejected).
    /// </summary>
    public ProductModerationStatus Decision { get; set; }

    /// <summary>
    /// Gets or sets the reason for the decision.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the previous moderation status.
    /// </summary>
    public ProductModerationStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets when the decision was made.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Result of a filtered product moderation query.
/// </summary>
public class GetProductsForModerationResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the list of product summaries for the current page.
    /// </summary>
    public IReadOnlyList<ProductModerationSummary> Products { get; private init; } = [];

    /// <summary>
    /// Gets the total number of products matching the filter criteria.
    /// </summary>
    public int TotalCount { get; private init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; private init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets the list of available categories for filtering.
    /// </summary>
    public IReadOnlyList<string> AvailableCategories { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="products">The product summaries for the current page.</param>
    /// <param name="totalCount">The total number of products matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="availableCategories">The list of available categories.</param>
    /// <returns>A successful result.</returns>
    public static GetProductsForModerationResult Success(
        IReadOnlyList<ProductModerationSummary> products,
        int totalCount,
        int page,
        int pageSize,
        IReadOnlyList<string> availableCategories) => new()
    {
        Succeeded = true,
        Errors = [],
        Products = products,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        AvailableCategories = availableCategories
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetProductsForModerationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetProductsForModerationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetProductsForModerationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access product moderation."]
    };
}

/// <summary>
/// Result of a product moderation details query.
/// </summary>
public class GetProductModerationDetailsResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the product moderation details.
    /// </summary>
    public ProductModerationDetails? ProductDetails { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="productDetails">The product moderation details.</param>
    /// <returns>A successful result.</returns>
    public static GetProductModerationDetailsResult Success(ProductModerationDetails productDetails) => new()
    {
        Succeeded = true,
        Errors = [],
        ProductDetails = productDetails
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetProductModerationDetailsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetProductModerationDetailsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetProductModerationDetailsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access product moderation details."]
    };
}
