using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for admin product moderation operations.
/// </summary>
public interface IProductModerationService
{
    /// <summary>
    /// Gets filtered and paginated products for admin moderation view.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the filtered products.</returns>
    Task<GetProductsForModerationResult> GetProductsForModerationAsync(ProductModerationFilterQuery query);

    /// <summary>
    /// Gets full details of a specific product for moderation.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The result containing the product details.</returns>
    Task<GetProductModerationDetailsResult> GetProductDetailsAsync(Guid productId);

    /// <summary>
    /// Approves a product, making it eligible for Active status.
    /// </summary>
    /// <param name="command">The approval command.</param>
    /// <returns>The result of the approval operation.</returns>
    Task<ApproveProductResult> ApproveProductAsync(ApproveProductCommand command);

    /// <summary>
    /// Rejects a product, marking it as not eligible for display.
    /// Sends a notification to the seller with the rejection reason.
    /// </summary>
    /// <param name="command">The rejection command.</param>
    /// <returns>The result of the rejection operation.</returns>
    Task<RejectProductResult> RejectProductAsync(RejectProductCommand command);

    /// <summary>
    /// Bulk approves or rejects multiple products.
    /// </summary>
    /// <param name="command">The bulk moderation command.</param>
    /// <returns>The result of the bulk moderation operation.</returns>
    Task<BulkModerateProductsResult> BulkModerateProductsAsync(BulkModerateProductsCommand command);
}
