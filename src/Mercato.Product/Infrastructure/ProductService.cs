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

    private static List<string> ValidateCreateCommand(CreateProductCommand command)
    {
        var errors = new List<string>();

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            errors.Add("Title is required.");
        }
        else if (command.Title.Length < ProductValidationConstants.TitleMinLength || 
                 command.Title.Length > ProductValidationConstants.TitleMaxLength)
        {
            errors.Add($"Title must be between {ProductValidationConstants.TitleMinLength} and {ProductValidationConstants.TitleMaxLength} characters.");
        }

        if (command.Price <= 0)
        {
            errors.Add("Price must be greater than 0.");
        }

        if (command.Stock < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(command.Category))
        {
            errors.Add("Category is required.");
        }
        else if (command.Category.Length < ProductValidationConstants.CategoryMinLength || 
                 command.Category.Length > ProductValidationConstants.CategoryMaxLength)
        {
            errors.Add($"Category must be between {ProductValidationConstants.CategoryMinLength} and {ProductValidationConstants.CategoryMaxLength} characters.");
        }

        if (command.Description != null && command.Description.Length > ProductValidationConstants.DescriptionMaxLength)
        {
            errors.Add($"Description must be at most {ProductValidationConstants.DescriptionMaxLength} characters.");
        }

        return errors;
    }
}
