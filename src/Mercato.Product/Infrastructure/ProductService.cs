using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Queries;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for product management operations.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateProductResult> CreateProductAsync(CreateProductCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateProductResult.Failure(validationErrors);
        }

        try
        {
            var product = new Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                StoreId = command.StoreId,
                Title = command.Title,
                Description = command.Description,
                Price = command.Price,
                Stock = command.Stock,
                Category = command.Category,
                Weight = command.Weight,
                Length = command.Length,
                Width = command.Width,
                Height = command.Height,
                ShippingMethods = command.ShippingMethods,
                Images = command.Images,
                Status = ProductStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            await _repository.AddAsync(product);

            _logger.LogInformation(
                "Product {ProductId} created for store {StoreId} with status Draft",
                product.Id,
                command.StoreId);

            return CreateProductResult.Success(product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for store {StoreId}", command.StoreId);
            return CreateProductResult.Failure("An error occurred while creating the product.");
        }
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Product?> GetProductByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.Product>> GetProductsByStoreIdAsync(Guid storeId)
    {
        return await _repository.GetByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<UpdateProductResult> UpdateProductAsync(UpdateProductCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateProductResult.Failure(validationErrors);
        }

        try
        {
            var product = await _repository.GetByIdAsync(command.ProductId);
            if (product == null)
            {
                return UpdateProductResult.Failure("Product not found.");
            }

            if (product.StoreId != command.StoreId)
            {
                return UpdateProductResult.NotAuthorized("You are not authorized to update this product.");
            }

            if (product.Status == ProductStatus.Archived)
            {
                return UpdateProductResult.Failure("Cannot update an archived product.");
            }

            product.Title = command.Title;
            product.Description = command.Description;
            product.Price = command.Price;
            product.Stock = command.Stock;
            product.Category = command.Category;
            product.Weight = command.Weight;
            product.Length = command.Length;
            product.Width = command.Width;
            product.Height = command.Height;
            product.ShippingMethods = command.ShippingMethods;
            product.Images = command.Images;
            product.LastUpdatedAt = DateTimeOffset.UtcNow;
            product.LastUpdatedBy = command.SellerId;

            await _repository.UpdateAsync(product);

            _logger.LogInformation(
                "Product {ProductId} updated by seller {SellerId}",
                command.ProductId,
                command.SellerId);

            return UpdateProductResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", command.ProductId);
            return UpdateProductResult.Failure("An error occurred while updating the product.");
        }
    }

    /// <inheritdoc />
    public async Task<ArchiveProductResult> ArchiveProductAsync(ArchiveProductCommand command)
    {
        var validationErrors = ValidateArchiveCommand(command);
        if (validationErrors.Count > 0)
        {
            return ArchiveProductResult.Failure(validationErrors);
        }

        try
        {
            var product = await _repository.GetByIdAsync(command.ProductId);
            if (product == null)
            {
                return ArchiveProductResult.Failure("Product not found.");
            }

            if (product.StoreId != command.StoreId)
            {
                return ArchiveProductResult.NotAuthorized("You are not authorized to archive this product.");
            }

            if (product.Status == ProductStatus.Archived)
            {
                return ArchiveProductResult.Failure("Product is already archived.");
            }

            product.Status = ProductStatus.Archived;
            product.ArchivedAt = DateTimeOffset.UtcNow;
            product.ArchivedBy = command.SellerId;
            product.LastUpdatedAt = DateTimeOffset.UtcNow;
            product.LastUpdatedBy = command.SellerId;

            await _repository.UpdateAsync(product);

            _logger.LogInformation(
                "Product {ProductId} archived by seller {SellerId}",
                command.ProductId,
                command.SellerId);

            return ArchiveProductResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving product {ProductId}", command.ProductId);
            return ArchiveProductResult.Failure("An error occurred while archiving the product.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.Product>> GetActiveProductsByStoreIdAsync(Guid storeId)
    {
        return await _repository.GetActiveByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<ChangeProductStatusResult> ChangeProductStatusAsync(ChangeProductStatusCommand command)
    {
        var validationErrors = ValidateChangeStatusCommand(command);
        if (validationErrors.Count > 0)
        {
            return ChangeProductStatusResult.Failure(validationErrors);
        }

        try
        {
            var product = await _repository.GetByIdAsync(command.ProductId);
            if (product == null)
            {
                return ChangeProductStatusResult.Failure("Product not found.");
            }

            // Admin override skips store authorization check
            if (!command.IsAdminOverride && product.StoreId != command.StoreId)
            {
                return ChangeProductStatusResult.NotAuthorized("You are not authorized to change this product's status.");
            }

            // Validate the transition
            var transitionErrors = ValidateStatusTransition(product, command.NewStatus, command.IsAdminOverride);
            if (transitionErrors.Count > 0)
            {
                return ChangeProductStatusResult.Failure(transitionErrors);
            }

            var previousStatus = product.Status;
            product.Status = command.NewStatus;
            product.LastUpdatedAt = DateTimeOffset.UtcNow;
            product.LastUpdatedBy = command.SellerId;

            // Set archived fields if transitioning to Archived
            if (command.NewStatus == ProductStatus.Archived)
            {
                product.ArchivedAt = DateTimeOffset.UtcNow;
                product.ArchivedBy = command.SellerId;
            }

            await _repository.UpdateAsync(product);

            _logger.LogInformation(
                "Product {ProductId} status changed from {PreviousStatus} to {NewStatus} by {SellerId}{AdminOverride}",
                command.ProductId,
                previousStatus,
                command.NewStatus,
                command.SellerId,
                command.IsAdminOverride ? " (admin override)" : "");

            return ChangeProductStatusResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status of product {ProductId}", command.ProductId);
            return ChangeProductStatusResult.Failure("An error occurred while changing the product status.");
        }
    }

    private static List<string> ValidateCreateCommand(CreateProductCommand command)
    {
        var errors = new List<string>();

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        ValidateProductFields(command.Title, command.Description, command.Price, command.Stock, command.Category, errors);
        ValidateShippingFields(command.Weight, command.Length, command.Width, command.Height, command.ShippingMethods, errors);
        ValidateImagesField(command.Images, errors);

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateProductCommand command)
    {
        var errors = new List<string>();

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        ValidateProductFields(command.Title, command.Description, command.Price, command.Stock, command.Category, errors);
        ValidateShippingFields(command.Weight, command.Length, command.Width, command.Height, command.ShippingMethods, errors);
        ValidateImagesField(command.Images, errors);

        return errors;
    }

    private static List<string> ValidateArchiveCommand(ArchiveProductCommand command)
    {
        var errors = new List<string>();

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        return errors;
    }

    private static void ValidateProductFields(string title, string? description, decimal price, int stock, string category, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title is required.");
        }
        else if (title.Length < ProductValidationConstants.TitleMinLength || 
                 title.Length > ProductValidationConstants.TitleMaxLength)
        {
            errors.Add($"Title must be between {ProductValidationConstants.TitleMinLength} and {ProductValidationConstants.TitleMaxLength} characters.");
        }

        if (price <= 0)
        {
            errors.Add("Price must be greater than 0.");
        }

        if (stock < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            errors.Add("Category is required.");
        }
        else if (category.Length < ProductValidationConstants.CategoryMinLength || 
                 category.Length > ProductValidationConstants.CategoryMaxLength)
        {
            errors.Add($"Category must be between {ProductValidationConstants.CategoryMinLength} and {ProductValidationConstants.CategoryMaxLength} characters.");
        }

        if (description != null && description.Length > ProductValidationConstants.DescriptionMaxLength)
        {
            errors.Add($"Description must be at most {ProductValidationConstants.DescriptionMaxLength} characters.");
        }
    }

    private static void ValidateShippingFields(decimal? weight, decimal? length, decimal? width, decimal? height, string? shippingMethods, List<string> errors)
    {
        if (weight.HasValue)
        {
            if (weight.Value < 0)
            {
                errors.Add("Weight cannot be negative.");
            }
            else if (weight.Value > ProductValidationConstants.WeightMaxKg)
            {
                errors.Add($"Weight must be at most {ProductValidationConstants.WeightMaxKg} kg.");
            }
        }

        if (length.HasValue)
        {
            if (length.Value < 0)
            {
                errors.Add("Length cannot be negative.");
            }
            else if (length.Value > ProductValidationConstants.DimensionMaxCm)
            {
                errors.Add($"Length must be at most {ProductValidationConstants.DimensionMaxCm} cm.");
            }
        }

        if (width.HasValue)
        {
            if (width.Value < 0)
            {
                errors.Add("Width cannot be negative.");
            }
            else if (width.Value > ProductValidationConstants.DimensionMaxCm)
            {
                errors.Add($"Width must be at most {ProductValidationConstants.DimensionMaxCm} cm.");
            }
        }

        if (height.HasValue)
        {
            if (height.Value < 0)
            {
                errors.Add("Height cannot be negative.");
            }
            else if (height.Value > ProductValidationConstants.DimensionMaxCm)
            {
                errors.Add($"Height must be at most {ProductValidationConstants.DimensionMaxCm} cm.");
            }
        }

        if (shippingMethods != null && shippingMethods.Length > ProductValidationConstants.ShippingMethodsMaxLength)
        {
            errors.Add($"Shipping methods must be at most {ProductValidationConstants.ShippingMethodsMaxLength} characters.");
        }
    }

    private static void ValidateImagesField(string? images, List<string> errors)
    {
        if (images != null && images.Length > ProductValidationConstants.ImagesMaxLength)
        {
            errors.Add($"Images must be at most {ProductValidationConstants.ImagesMaxLength} characters.");
        }
    }

    private static List<string> ValidateChangeStatusCommand(ChangeProductStatusCommand command)
    {
        var errors = new List<string>();

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        // StoreId can be empty for admin overrides
        if (!command.IsAdminOverride && command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateStatusTransition(Domain.Entities.Product product, ProductStatus newStatus, bool isAdminOverride)
    {
        var errors = new List<string>();
        var currentStatus = product.Status;

        // Cannot change status of an archived product
        if (currentStatus == ProductStatus.Archived)
        {
            errors.Add("Cannot change the status of an archived product.");
            return errors;
        }

        // Same status is allowed (no-op)
        if (currentStatus == newStatus)
        {
            return errors;
        }

        // Validate allowed transitions based on current status
        switch (currentStatus)
        {
            case ProductStatus.Draft:
                // From Draft: can go to Active (with validation) or Archived
                if (newStatus == ProductStatus.Active)
                {
                    ValidateActiveTransitionRequirements(product, errors);
                }
                else if (newStatus != ProductStatus.Archived && !isAdminOverride)
                {
                    // Admin can override to Suspended directly
                    errors.Add($"Cannot transition from Draft to {newStatus}. Only Active or Archived transitions are allowed.");
                }
                break;

            case ProductStatus.Active:
                // From Active: can go to Suspended, Archived, or back to Draft with admin override
                // Cannot go back to Draft without admin
                if (newStatus == ProductStatus.Draft && !isAdminOverride)
                {
                    errors.Add("Cannot transition from Active to Draft. This transition requires admin approval.");
                }
                break;

            case ProductStatus.Suspended:
                // From Suspended: can go to Active (with re-validation) or Archived
                // Cannot go back to Draft without admin
                if (newStatus == ProductStatus.Active)
                {
                    ValidateActiveTransitionRequirements(product, errors);
                }
                else if (newStatus == ProductStatus.Draft && !isAdminOverride)
                {
                    errors.Add("Cannot transition from Suspended to Draft. This transition requires admin approval.");
                }
                break;

            case ProductStatus.Inactive:
                // From Inactive: can go to Active (with re-validation), Suspended, or Archived
                if (newStatus == ProductStatus.Active)
                {
                    ValidateActiveTransitionRequirements(product, errors);
                }
                else if (newStatus == ProductStatus.Draft && !isAdminOverride)
                {
                    errors.Add("Cannot transition from Inactive to Draft. This transition requires admin approval.");
                }
                break;

            case ProductStatus.OutOfStock:
                // From OutOfStock: can go to Active (with re-validation), Suspended, or Archived
                if (newStatus == ProductStatus.Active)
                {
                    ValidateActiveTransitionRequirements(product, errors);
                }
                else if (newStatus == ProductStatus.Draft && !isAdminOverride)
                {
                    errors.Add("Cannot transition from OutOfStock to Draft. This transition requires admin approval.");
                }
                break;
        }

        return errors;
    }

    private static void ValidateActiveTransitionRequirements(Domain.Entities.Product product, List<string> errors)
    {
        // Check for minimum data quality rules required for Active status:
        // images, description, category, price, stock

        // Description is required for Active products
        if (string.IsNullOrWhiteSpace(product.Description))
        {
            errors.Add("Description is required to set product to Active.");
        }

        // Category is required (already validated during create/update but double-check)
        if (string.IsNullOrWhiteSpace(product.Category))
        {
            errors.Add("Category is required to set product to Active.");
        }

        // Price must be greater than 0
        if (product.Price <= 0)
        {
            errors.Add("Price must be greater than 0 to set product to Active.");
        }

        // Stock must not be negative
        if (product.Stock < 0)
        {
            errors.Add("Stock cannot be negative to set product to Active.");
        }

        // At least one image is required
        if (string.IsNullOrWhiteSpace(product.Images) || product.Images == "[]")
        {
            errors.Add("At least one image is required to set product to Active.");
        }
    }

    /// <inheritdoc />
    public async Task<BulkUpdatePriceStockResult> BulkUpdatePriceStockAsync(BulkUpdatePriceStockCommand command)
    {
        var validationErrors = ValidateBulkUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return BulkUpdatePriceStockResult.Failure(validationErrors);
        }

        try
        {
            var products = await _repository.GetByIdsAsync(command.ProductIds);

            if (products.Count == 0)
            {
                return BulkUpdatePriceStockResult.Failure("No products found with the specified IDs.");
            }

            var successCount = 0;
            var failedProducts = new List<BulkUpdateProductFailure>();
            var productsToUpdate = new List<Domain.Entities.Product>();

            foreach (var product in products)
            {
                // Authorization check: product must belong to the seller's store
                if (product.StoreId != command.StoreId)
                {
                    failedProducts.Add(new BulkUpdateProductFailure
                    {
                        ProductId = product.Id,
                        ProductTitle = product.Title,
                        Error = "You are not authorized to update this product."
                    });
                    continue;
                }

                // Cannot update archived products
                if (product.Status == ProductStatus.Archived)
                {
                    failedProducts.Add(new BulkUpdateProductFailure
                    {
                        ProductId = product.Id,
                        ProductTitle = product.Title,
                        Error = "Cannot update an archived product."
                    });
                    continue;
                }

                // Calculate new price if price update is specified
                decimal? newPrice = null;
                if (command.PriceUpdate != null)
                {
                    newPrice = CalculateNewPrice(product.Price, command.PriceUpdate);
                    if (newPrice <= 0)
                    {
                        failedProducts.Add(new BulkUpdateProductFailure
                        {
                            ProductId = product.Id,
                            ProductTitle = product.Title,
                            Error = "Resulting price would be zero or negative."
                        });
                        continue;
                    }
                }

                // Calculate new stock if stock update is specified
                int? newStock = null;
                if (command.StockUpdate != null)
                {
                    newStock = CalculateNewStock(product.Stock, command.StockUpdate);
                    if (newStock < 0)
                    {
                        failedProducts.Add(new BulkUpdateProductFailure
                        {
                            ProductId = product.Id,
                            ProductTitle = product.Title,
                            Error = "Resulting stock would be negative."
                        });
                        continue;
                    }
                }

                // Apply the updates
                if (newPrice.HasValue)
                {
                    product.Price = newPrice.Value;
                }

                if (newStock.HasValue)
                {
                    product.Stock = newStock.Value;
                }

                product.LastUpdatedAt = DateTimeOffset.UtcNow;
                product.LastUpdatedBy = command.SellerId;

                productsToUpdate.Add(product);
                successCount++;
            }

            // Update all successful products in a batch
            if (productsToUpdate.Count > 0)
            {
                await _repository.UpdateManyAsync(productsToUpdate);
            }

            _logger.LogInformation(
                "Bulk update completed for store {StoreId} by seller {SellerId}: {SuccessCount} succeeded, {FailureCount} failed",
                command.StoreId,
                command.SellerId,
                successCount,
                failedProducts.Count);

            return BulkUpdatePriceStockResult.Success(successCount, failedProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update for store {StoreId}", command.StoreId);
            return BulkUpdatePriceStockResult.Failure("An error occurred while updating the products.");
        }
    }

    private static List<string> ValidateBulkUpdateCommand(BulkUpdatePriceStockCommand command)
    {
        var errors = new List<string>();

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        if (command.ProductIds == null || command.ProductIds.Count == 0)
        {
            errors.Add("At least one product ID is required.");
        }

        if (command.PriceUpdate == null && command.StockUpdate == null)
        {
            errors.Add("At least one update (price or stock) must be specified.");
        }

        if (command.PriceUpdate != null)
        {
            ValidatePriceUpdate(command.PriceUpdate, errors);
        }

        if (command.StockUpdate != null)
        {
            ValidateStockUpdate(command.StockUpdate, errors);
        }

        return errors;
    }

    private static void ValidatePriceUpdate(BulkPriceUpdate priceUpdate, List<string> errors)
    {
        switch (priceUpdate.UpdateType)
        {
            case BulkPriceUpdateType.Fixed:
                if (priceUpdate.Value <= 0)
                {
                    errors.Add("Fixed price must be greater than 0.");
                }
                break;
            case BulkPriceUpdateType.PercentageIncrease:
            case BulkPriceUpdateType.PercentageDecrease:
                if (priceUpdate.Value <= 0)
                {
                    errors.Add("Percentage must be greater than 0.");
                }
                if (priceUpdate.Value > 100 && priceUpdate.UpdateType == BulkPriceUpdateType.PercentageDecrease)
                {
                    errors.Add("Percentage decrease cannot exceed 100%.");
                }
                break;
            case BulkPriceUpdateType.AmountIncrease:
            case BulkPriceUpdateType.AmountDecrease:
                if (priceUpdate.Value <= 0)
                {
                    errors.Add("Amount must be greater than 0.");
                }
                break;
        }
    }

    private static void ValidateStockUpdate(BulkStockUpdate stockUpdate, List<string> errors)
    {
        switch (stockUpdate.UpdateType)
        {
            case BulkStockUpdateType.Fixed:
                if (stockUpdate.Value < 0)
                {
                    errors.Add("Fixed stock cannot be negative.");
                }
                break;
            case BulkStockUpdateType.Increase:
            case BulkStockUpdateType.Decrease:
                if (stockUpdate.Value <= 0)
                {
                    errors.Add("Stock adjustment amount must be greater than 0.");
                }
                break;
        }
    }

    private static decimal CalculateNewPrice(decimal currentPrice, BulkPriceUpdate priceUpdate)
    {
        return priceUpdate.UpdateType switch
        {
            BulkPriceUpdateType.Fixed => priceUpdate.Value,
            BulkPriceUpdateType.PercentageIncrease => currentPrice * (1 + priceUpdate.Value / 100),
            BulkPriceUpdateType.PercentageDecrease => currentPrice * (1 - priceUpdate.Value / 100),
            BulkPriceUpdateType.AmountIncrease => currentPrice + priceUpdate.Value,
            BulkPriceUpdateType.AmountDecrease => currentPrice - priceUpdate.Value,
            _ => currentPrice
        };
    }

    private static int CalculateNewStock(int currentStock, BulkStockUpdate stockUpdate)
    {
        return stockUpdate.UpdateType switch
        {
            BulkStockUpdateType.Fixed => stockUpdate.Value,
            BulkStockUpdateType.Increase => currentStock + stockUpdate.Value,
            BulkStockUpdateType.Decrease => currentStock - stockUpdate.Value,
            _ => currentStock
        };
    }

    /// <inheritdoc />
    public async Task<ExportProductCatalogResult> ExportProductCatalogAsync(ExportProductCatalogCommand command)
    {
        var validationErrors = ValidateExportCommand(command);
        if (validationErrors.Count > 0)
        {
            return ExportProductCatalogResult.Failure(validationErrors);
        }

        try
        {
            // Get products for the store
            var products = await _repository.GetActiveByStoreIdAsync(command.StoreId);

            // Apply filters if requested
            if (command.ApplyFilters)
            {
                products = ApplyExportFilters(products, command);
            }

            // Generate the export file
            byte[] fileContent;
            string fileName;
            string contentType;

            if (command.Format == ExportFormat.Excel)
            {
                fileContent = GenerateExcelExport(products);
                fileName = $"products_export_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }
            else
            {
                fileContent = GenerateCsvExport(products);
                fileName = $"products_export_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.csv";
                contentType = "text/csv";
            }

            _logger.LogInformation(
                "Product catalog exported for store {StoreId}: {Count} products, format {Format}",
                command.StoreId,
                products.Count,
                command.Format);

            return ExportProductCatalogResult.Success(fileContent, fileName, contentType, products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting product catalog for store {StoreId}", command.StoreId);
            return ExportProductCatalogResult.Failure("An error occurred while exporting the product catalog.");
        }
    }

    private static List<string> ValidateExportCommand(ExportProductCatalogCommand command)
    {
        var errors = new List<string>();

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        return errors;
    }

    private static IReadOnlyList<Domain.Entities.Product> ApplyExportFilters(
        IReadOnlyList<Domain.Entities.Product> products,
        ExportProductCatalogCommand command)
    {
        var filtered = products.AsEnumerable();

        // Apply search query filter (searches in title and description)
        if (!string.IsNullOrWhiteSpace(command.SearchQuery))
        {
            var query = command.SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(command.CategoryFilter))
        {
            filtered = filtered.Where(p =>
                p.Category.Equals(command.CategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Apply status filter
        if (command.StatusFilter.HasValue)
        {
            filtered = filtered.Where(p => p.Status == command.StatusFilter.Value);
        }

        return filtered.ToList();
    }

    private static byte[] GenerateCsvExport(IReadOnlyList<Domain.Entities.Product> products)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        using var csv = new CsvWriter(writer, config);

        // Write header
        csv.WriteField("SKU");
        csv.WriteField("Title");
        csv.WriteField("Description");
        csv.WriteField("Price");
        csv.WriteField("Stock");
        csv.WriteField("Category");
        csv.WriteField("Status");
        csv.WriteField("Weight");
        csv.WriteField("Length");
        csv.WriteField("Width");
        csv.WriteField("Height");
        csv.WriteField("ShippingMethods");
        csv.WriteField("Images");
        csv.NextRecord();

        // Write data rows
        foreach (var product in products)
        {
            csv.WriteField(product.Sku ?? string.Empty);
            csv.WriteField(product.Title);
            csv.WriteField(product.Description ?? string.Empty);
            csv.WriteField(product.Price.ToString(CultureInfo.InvariantCulture));
            csv.WriteField(product.Stock.ToString(CultureInfo.InvariantCulture));
            csv.WriteField(product.Category);
            csv.WriteField(product.Status.ToString());
            csv.WriteField(product.Weight?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(product.Length?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(product.Width?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(product.Height?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(product.ShippingMethods ?? string.Empty);
            csv.WriteField(product.Images ?? string.Empty);
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    private static byte[] GenerateExcelExport(IReadOnlyList<Domain.Entities.Product> products)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        // Write header row
        var headers = new[]
        {
            "SKU", "Title", "Description", "Price", "Stock", "Category", "Status",
            "Weight", "Length", "Width", "Height", "ShippingMethods", "Images"
        };

        for (var col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        // Write data rows
        for (var row = 0; row < products.Count; row++)
        {
            var product = products[row];
            var excelRow = row + 2; // Start from row 2 (after header)

            worksheet.Cell(excelRow, 1).Value = product.Sku ?? string.Empty;
            worksheet.Cell(excelRow, 2).Value = product.Title;
            worksheet.Cell(excelRow, 3).Value = product.Description ?? string.Empty;
            worksheet.Cell(excelRow, 4).Value = (double)product.Price;
            worksheet.Cell(excelRow, 5).Value = product.Stock;
            worksheet.Cell(excelRow, 6).Value = product.Category;
            worksheet.Cell(excelRow, 7).Value = product.Status.ToString();

            // Handle nullable numeric fields - leave cell empty (Blank) if no value
            if (product.Weight.HasValue)
                worksheet.Cell(excelRow, 8).Value = (double)product.Weight.Value;
            if (product.Length.HasValue)
                worksheet.Cell(excelRow, 9).Value = (double)product.Length.Value;
            if (product.Width.HasValue)
                worksheet.Cell(excelRow, 10).Value = (double)product.Width.Value;
            if (product.Height.HasValue)
                worksheet.Cell(excelRow, 11).Value = (double)product.Height.Value;

            worksheet.Cell(excelRow, 12).Value = product.ShippingMethods ?? string.Empty;
            worksheet.Cell(excelRow, 13).Value = product.Images ?? string.Empty;
        }

        // Auto-fit columns for better readability
        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> GetProductsByCategoryAsync(string categoryName, int page, int pageSize)
    {
        return await _repository.GetActiveByCategoryAsync(categoryName, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> SearchProductsAsync(string searchQuery, int page, int pageSize)
    {
        return await _repository.SearchActiveProductsAsync(searchQuery, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<FilteredProductsResult> SearchProductsWithFiltersAsync(ProductFilterQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var (products, totalCount) = await _repository.SearchActiveProductsWithFiltersAsync(
            filter.SearchQuery,
            filter.Category,
            filter.MinPrice,
            filter.MaxPrice,
            filter.Condition,
            filter.StoreId,
            filter.Page,
            filter.PageSize,
            filter.SortBy);

        return FilteredProductsResult.Success(products, totalCount, filter);
    }

    /// <inheritdoc />
    public async Task<(decimal? MinPrice, decimal? MaxPrice)> GetActivePriceRangeAsync()
    {
        return await _repository.GetActivePriceRangeAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveProductStoreIdsAsync()
    {
        return await _repository.GetActiveProductStoreIdsAsync();
    }
}
