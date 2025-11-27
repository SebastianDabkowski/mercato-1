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
                return UpdateProductResult.Failure("You are not authorized to update this product.");
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
                return ArchiveProductResult.Failure("You are not authorized to archive this product.");
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
}
