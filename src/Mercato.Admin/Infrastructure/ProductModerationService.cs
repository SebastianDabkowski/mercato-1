using System.Text.Json;
using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for admin product moderation operations.
/// </summary>
public class ProductModerationService : IProductModerationService
{
    /// <summary>
    /// Maximum length for description preview in list view.
    /// </summary>
    private const int DescriptionPreviewMaxLength = 100;

    private readonly IProductModerationRepository _productModerationRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProductModerationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductModerationService"/> class.
    /// </summary>
    /// <param name="productModerationRepository">The product moderation repository.</param>
    /// <param name="storeRepository">The store repository.</param>
    /// <param name="adminAuditRepository">The admin audit repository.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger.</param>
    public ProductModerationService(
        IProductModerationRepository productModerationRepository,
        IStoreRepository storeRepository,
        IAdminAuditRepository adminAuditRepository,
        INotificationService notificationService,
        ILogger<ProductModerationService> logger)
    {
        _productModerationRepository = productModerationRepository ?? throw new ArgumentNullException(nameof(productModerationRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _adminAuditRepository = adminAuditRepository ?? throw new ArgumentNullException(nameof(adminAuditRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GetProductsForModerationResult> GetProductsForModerationAsync(ProductModerationFilterQuery query)
    {
        try
        {
            var (products, totalCount) = await _productModerationRepository.GetProductsForModerationAsync(
                query.ModerationStatuses.Count > 0 ? query.ModerationStatuses : null,
                query.Category,
                query.SearchTerm,
                query.Page,
                query.PageSize);

            // Get store information for the products
            var storeIds = products.Select(p => p.StoreId).Distinct().ToList();
            var stores = await _storeRepository.GetByIdsAsync(storeIds);
            var storeDict = stores.ToDictionary(s => s.Id, s => s.Name);

            // Get available categories
            var categories = await _productModerationRepository.GetDistinctCategoriesAsync();

            var productSummaries = products.Select(p => new ProductModerationSummary
            {
                Id = p.Id,
                Title = p.Title,
                StoreId = p.StoreId,
                StoreName = storeDict.GetValueOrDefault(p.StoreId, "Unknown Store"),
                DescriptionPreview = TruncateDescription(p.Description),
                Category = p.Category,
                Price = p.Price,
                Status = p.Status,
                ModerationStatus = p.ModerationStatus,
                CreatedAt = p.CreatedAt,
                ModeratedAt = p.ModeratedAt,
                ImageUrl = ParseFirstImageUrl(p.Images)
            }).ToList();

            return GetProductsForModerationResult.Success(productSummaries, totalCount, query.Page, query.PageSize, categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for moderation");
            return GetProductsForModerationResult.Failure("An error occurred while retrieving products for moderation.");
        }
    }

    /// <inheritdoc />
    public async Task<GetProductModerationDetailsResult> GetProductDetailsAsync(Guid productId)
    {
        try
        {
            var product = await _productModerationRepository.GetProductForModerationAsync(productId);
            if (product == null)
            {
                return GetProductModerationDetailsResult.Failure("Product not found.");
            }

            // Get store information
            var store = await _storeRepository.GetByIdAsync(product.StoreId);

            // Get moderation history
            var history = await _productModerationRepository.GetModerationHistoryAsync(productId);

            var productDetails = new ProductModerationDetails
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                Status = product.Status,
                ModerationStatus = product.ModerationStatus,
                ModerationReason = product.ModerationReason,
                ModeratedAt = product.ModeratedAt,
                ModeratedBy = product.ModeratedBy,
                StoreId = product.StoreId,
                StoreName = store?.Name ?? "Unknown Store",
                SellerId = store?.SellerId ?? string.Empty,
                Images = product.Images,
                ImageUrls = ParseImageUrls(product.Images),
                CreatedAt = product.CreatedAt,
                LastUpdatedAt = product.LastUpdatedAt,
                ModerationHistory = history.Select(h => new ProductModerationHistoryEntry
                {
                    Id = h.Id,
                    AdminUserId = h.AdminUserId,
                    Decision = h.Decision,
                    Reason = h.Reason,
                    PreviousStatus = h.PreviousStatus,
                    CreatedAt = h.CreatedAt
                }).ToList()
            };

            return GetProductModerationDetailsResult.Success(productDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product details for product {ProductId}", productId);
            return GetProductModerationDetailsResult.Failure("An error occurred while retrieving product details.");
        }
    }

    /// <inheritdoc />
    public async Task<ApproveProductResult> ApproveProductAsync(ApproveProductCommand command)
    {
        try
        {
            var validationErrors = ValidateApproveProductCommand(command);
            if (validationErrors.Count > 0)
            {
                return ApproveProductResult.Failure(validationErrors);
            }

            var product = await _productModerationRepository.GetProductForModerationAsync(command.ProductId);
            if (product == null)
            {
                return ApproveProductResult.Failure("Product not found.");
            }

            var previousModerationStatus = product.ModerationStatus;
            var previousProductStatus = product.Status;

            // Update product moderation status
            product.ModerationStatus = ProductModerationStatus.Approved;
            product.ModerationReason = command.Reason;
            product.ModeratedAt = DateTimeOffset.UtcNow;
            product.ModeratedBy = command.AdminUserId;

            // Set product to Active if it was in a state that allows activation
            if (product.Status == ProductStatus.Draft || product.Status == ProductStatus.Inactive)
            {
                product.Status = ProductStatus.Active;
                product.LastUpdatedAt = DateTimeOffset.UtcNow;
                product.LastUpdatedBy = command.AdminUserId;
            }

            await _productModerationRepository.UpdateModerationStatusAsync(product);

            // Record moderation decision
            await _productModerationRepository.AddModerationDecisionAsync(new ProductModerationDecision
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                AdminUserId = command.AdminUserId,
                Decision = ProductModerationStatus.Approved,
                Reason = command.Reason,
                PreviousStatus = previousModerationStatus,
                PreviousProductStatus = previousProductStatus,
                CreatedAt = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "ApproveProduct",
                EntityType = "Product",
                EntityId = product.Id.ToString(),
                Details = $"Approved product. Previous moderation status: {previousModerationStatus}. Reason: {command.Reason ?? "N/A"}",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Send notification to seller
            var store = await _storeRepository.GetByIdAsync(product.StoreId);
            if (store != null && !string.IsNullOrEmpty(store.SellerId))
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationCommand
                {
                    UserId = store.SellerId,
                    Title = "Product Approved",
                    Message = $"Your product \"{product.Title}\" has been approved and is now visible to buyers.",
                    Type = NotificationType.ProductApproved,
                    RelatedEntityId = product.Id,
                    RelatedUrl = $"/Seller/Products/Details/{product.Id}"
                });
            }

            _logger.LogInformation("Product {ProductId} approved by admin {AdminUserId}", product.Id, command.AdminUserId);

            return ApproveProductResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving product {ProductId}", command.ProductId);
            return ApproveProductResult.Failure("An error occurred while approving the product.");
        }
    }

    /// <inheritdoc />
    public async Task<RejectProductResult> RejectProductAsync(RejectProductCommand command)
    {
        try
        {
            var validationErrors = ValidateRejectProductCommand(command);
            if (validationErrors.Count > 0)
            {
                return RejectProductResult.Failure(validationErrors);
            }

            var product = await _productModerationRepository.GetProductForModerationAsync(command.ProductId);
            if (product == null)
            {
                return RejectProductResult.Failure("Product not found.");
            }

            var previousModerationStatus = product.ModerationStatus;
            var previousProductStatus = product.Status;

            // Update product moderation status
            product.ModerationStatus = ProductModerationStatus.Rejected;
            product.ModerationReason = command.Reason;
            product.ModeratedAt = DateTimeOffset.UtcNow;
            product.ModeratedBy = command.AdminUserId;

            // Set product to Inactive if it was Active
            if (product.Status == ProductStatus.Active)
            {
                product.Status = ProductStatus.Inactive;
                product.LastUpdatedAt = DateTimeOffset.UtcNow;
                product.LastUpdatedBy = command.AdminUserId;
            }

            await _productModerationRepository.UpdateModerationStatusAsync(product);

            // Record moderation decision
            await _productModerationRepository.AddModerationDecisionAsync(new ProductModerationDecision
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                AdminUserId = command.AdminUserId,
                Decision = ProductModerationStatus.Rejected,
                Reason = command.Reason,
                PreviousStatus = previousModerationStatus,
                PreviousProductStatus = previousProductStatus,
                CreatedAt = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "RejectProduct",
                EntityType = "Product",
                EntityId = product.Id.ToString(),
                Details = $"Rejected product. Previous moderation status: {previousModerationStatus}. Reason: {command.Reason}",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Send notification to seller
            var store = await _storeRepository.GetByIdAsync(product.StoreId);
            if (store != null && !string.IsNullOrEmpty(store.SellerId))
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationCommand
                {
                    UserId = store.SellerId,
                    Title = "Product Rejected",
                    Message = $"Your product \"{product.Title}\" has been rejected. Reason: {command.Reason}",
                    Type = NotificationType.ProductRejected,
                    RelatedEntityId = product.Id,
                    RelatedUrl = $"/Seller/Products/Details/{product.Id}"
                });
            }

            _logger.LogInformation("Product {ProductId} rejected by admin {AdminUserId}. Reason: {Reason}", 
                product.Id, command.AdminUserId, command.Reason);

            return RejectProductResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting product {ProductId}", command.ProductId);
            return RejectProductResult.Failure("An error occurred while rejecting the product.");
        }
    }

    /// <inheritdoc />
    public async Task<BulkModerateProductsResult> BulkModerateProductsAsync(BulkModerateProductsCommand command)
    {
        try
        {
            var validationErrors = ValidateBulkModerateProductsCommand(command);
            if (validationErrors.Count > 0)
            {
                return BulkModerateProductsResult.Failure(validationErrors);
            }

            var products = await _productModerationRepository.GetProductsByIdsAsync(command.ProductIds);
            if (products.Count == 0)
            {
                return BulkModerateProductsResult.Failure("No products found with the specified IDs.");
            }

            var successCount = 0;
            var failureCount = 0;
            var errors = new List<string>();

            foreach (var product in products)
            {
                try
                {
                    var previousModerationStatus = product.ModerationStatus;
                    var previousProductStatus = product.Status;

                    if (command.Approve)
                    {
                        product.ModerationStatus = ProductModerationStatus.Approved;
                        product.ModerationReason = command.Reason;
                        product.ModeratedAt = DateTimeOffset.UtcNow;
                        product.ModeratedBy = command.AdminUserId;

                        if (product.Status == ProductStatus.Draft || product.Status == ProductStatus.Inactive)
                        {
                            product.Status = ProductStatus.Active;
                            product.LastUpdatedAt = DateTimeOffset.UtcNow;
                            product.LastUpdatedBy = command.AdminUserId;
                        }
                    }
                    else
                    {
                        product.ModerationStatus = ProductModerationStatus.Rejected;
                        product.ModerationReason = command.Reason;
                        product.ModeratedAt = DateTimeOffset.UtcNow;
                        product.ModeratedBy = command.AdminUserId;

                        if (product.Status == ProductStatus.Active)
                        {
                            product.Status = ProductStatus.Inactive;
                            product.LastUpdatedAt = DateTimeOffset.UtcNow;
                            product.LastUpdatedBy = command.AdminUserId;
                        }
                    }

                    // Record moderation decision
                    await _productModerationRepository.AddModerationDecisionAsync(new ProductModerationDecision
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        AdminUserId = command.AdminUserId,
                        Decision = command.Approve ? ProductModerationStatus.Approved : ProductModerationStatus.Rejected,
                        Reason = command.Reason,
                        PreviousStatus = previousModerationStatus,
                        PreviousProductStatus = previousProductStatus,
                        CreatedAt = DateTimeOffset.UtcNow,
                        IpAddress = command.IpAddress
                    });

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to moderate product {ProductId} in bulk operation", product.Id);
                    errors.Add($"Failed to moderate product {product.Id}: {ex.Message}");
                    failureCount++;
                }
            }

            // Update all products in bulk
            if (successCount > 0)
            {
                await _productModerationRepository.UpdateModerationStatusBulkAsync(products.Where(p => 
                    (command.Approve && p.ModerationStatus == ProductModerationStatus.Approved) ||
                    (!command.Approve && p.ModerationStatus == ProductModerationStatus.Rejected)));

                // Create audit log entry
                await _adminAuditRepository.AddAsync(new AdminAuditLog
                {
                    Id = Guid.NewGuid(),
                    AdminUserId = command.AdminUserId,
                    Action = command.Approve ? "BulkApproveProducts" : "BulkRejectProducts",
                    EntityType = "Product",
                    EntityId = string.Join(",", command.ProductIds),
                    Details = $"Bulk {(command.Approve ? "approved" : "rejected")} {successCount} products. Reason: {command.Reason ?? "N/A"}",
                    Timestamp = DateTimeOffset.UtcNow,
                    IpAddress = command.IpAddress
                });

                // Send notifications to sellers
                var storeIds = products.Select(p => p.StoreId).Distinct().ToList();
                var stores = await _storeRepository.GetByIdsAsync(storeIds);
                var storeDict = stores.ToDictionary(s => s.Id);

                foreach (var product in products.Where(p => 
                    (command.Approve && p.ModerationStatus == ProductModerationStatus.Approved) ||
                    (!command.Approve && p.ModerationStatus == ProductModerationStatus.Rejected)))
                {
                    if (storeDict.TryGetValue(product.StoreId, out var store) && !string.IsNullOrEmpty(store.SellerId))
                    {
                        await _notificationService.CreateNotificationAsync(new CreateNotificationCommand
                        {
                            UserId = store.SellerId,
                            Title = command.Approve ? "Product Approved" : "Product Rejected",
                            Message = command.Approve 
                                ? $"Your product \"{product.Title}\" has been approved and is now visible to buyers."
                                : $"Your product \"{product.Title}\" has been rejected. Reason: {command.Reason}",
                            Type = command.Approve ? NotificationType.ProductApproved : NotificationType.ProductRejected,
                            RelatedEntityId = product.Id,
                            RelatedUrl = $"/Seller/Products/Details/{product.Id}"
                        });
                    }
                }
            }

            _logger.LogInformation("Bulk moderation completed by admin {AdminUserId}. Action: {Action}. Success: {SuccessCount}, Failures: {FailureCount}",
                command.AdminUserId, command.Approve ? "Approve" : "Reject", successCount, failureCount);

            if (failureCount > 0 && successCount > 0)
            {
                return BulkModerateProductsResult.PartialSuccess(successCount, failureCount, errors);
            }
            else if (failureCount > 0)
            {
                return BulkModerateProductsResult.Failure(errors);
            }

            return BulkModerateProductsResult.Success(successCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk product moderation");
            return BulkModerateProductsResult.Failure("An error occurred while performing bulk moderation.");
        }
    }

    private static List<string> ValidateApproveProductCommand(ApproveProductCommand command)
    {
        var errors = new List<string>();

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRejectProductCommand(RejectProductCommand command)
    {
        var errors = new List<string>();

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Rejection reason is required.");
        }

        return errors;
    }

    private static List<string> ValidateBulkModerateProductsCommand(BulkModerateProductsCommand command)
    {
        var errors = new List<string>();

        if (command.ProductIds == null || command.ProductIds.Count == 0)
        {
            errors.Add("At least one product ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (!command.Approve && string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Rejection reason is required for bulk rejection.");
        }

        return errors;
    }

    private static string TruncateDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        if (description.Length <= DescriptionPreviewMaxLength)
        {
            return description;
        }

        return description[..DescriptionPreviewMaxLength] + "...";
    }

    private static string? ParseFirstImageUrl(string? imagesJson)
    {
        if (string.IsNullOrEmpty(imagesJson))
        {
            return null;
        }

        try
        {
            var images = JsonSerializer.Deserialize<string[]>(imagesJson);
            return images?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ParseImageUrls(string? imagesJson)
    {
        if (string.IsNullOrEmpty(imagesJson))
        {
            return [];
        }

        try
        {
            var images = JsonSerializer.Deserialize<string[]>(imagesJson);
            return images ?? [];
        }
        catch
        {
            return [];
        }
    }
}
