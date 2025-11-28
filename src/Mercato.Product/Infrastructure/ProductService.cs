using Mercato.Product.Application.Commands;
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
}
