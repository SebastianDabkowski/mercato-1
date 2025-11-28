using System.Text.Json;
using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for product variant management operations.
/// </summary>
public class ProductVariantService : IProductVariantService
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductVariantService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductVariantService"/> class.
    /// </summary>
    /// <param name="variantRepository">The variant repository.</param>
    /// <param name="productRepository">The product repository.</param>
    /// <param name="logger">The logger.</param>
    public ProductVariantService(
        IProductVariantRepository variantRepository,
        IProductRepository productRepository,
        ILogger<ProductVariantService> logger)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ConfigureProductVariantsResult> ConfigureVariantsAsync(ConfigureProductVariantsCommand command)
    {
        var validationErrors = ValidateConfigureCommand(command);
        if (validationErrors.Count > 0)
        {
            return ConfigureProductVariantsResult.Failure(validationErrors);
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId);
        if (product == null)
        {
            return ConfigureProductVariantsResult.Failure("Product not found.");
        }

        if (product.StoreId != command.StoreId)
        {
            return ConfigureProductVariantsResult.NotAuthorized("You are not authorized to configure variants for this product.");
        }

        if (product.Status == ProductStatus.Archived)
        {
            return ConfigureProductVariantsResult.Failure("Cannot configure variants for an archived product.");
        }

        // Validate attribute limits
        if (command.Attributes.Count > ProductVariantValidationConstants.MaxAttributesPerProduct)
        {
            return ConfigureProductVariantsResult.Failure(
                $"Maximum {ProductVariantValidationConstants.MaxAttributesPerProduct} variant attributes allowed per product.");
        }

        foreach (var attr in command.Attributes)
        {
            if (attr.Values.Count > ProductVariantValidationConstants.MaxValuesPerAttribute)
            {
                return ConfigureProductVariantsResult.Failure(
                    $"Maximum {ProductVariantValidationConstants.MaxValuesPerAttribute} values allowed per attribute '{attr.Name}'.");
            }
        }

        if (command.Variants.Count > ProductVariantValidationConstants.MaxVariantsPerProduct)
        {
            return ConfigureProductVariantsResult.Failure(
                $"Maximum {ProductVariantValidationConstants.MaxVariantsPerProduct} variants allowed per product.");
        }

        try
        {
            // Delete existing variants and attributes
            await _variantRepository.DeleteVariantsByProductIdAsync(command.ProductId);
            await _variantRepository.DeleteAttributesByProductIdAsync(command.ProductId);

            // Create new attributes
            var now = DateTimeOffset.UtcNow;
            var displayOrder = 0;

            foreach (var attrDef in command.Attributes)
            {
                var attribute = new ProductVariantAttribute
                {
                    Id = Guid.NewGuid(),
                    ProductId = command.ProductId,
                    Name = attrDef.Name,
                    DisplayOrder = displayOrder++,
                    CreatedAt = now
                };

                var valueOrder = 0;
                foreach (var value in attrDef.Values)
                {
                    attribute.Values.Add(new ProductVariantAttributeValue
                    {
                        Id = Guid.NewGuid(),
                        VariantAttributeId = attribute.Id,
                        Value = value,
                        DisplayOrder = valueOrder++,
                        CreatedAt = now
                    });
                }

                await _variantRepository.AddAttributeAsync(attribute);
            }

            // Create variants
            var variants = new List<ProductVariant>();
            foreach (var variantDef in command.Variants)
            {
                var attributeCombination = JsonSerializer.Serialize(variantDef.AttributeValues);

                var variant = new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = command.ProductId,
                    Sku = variantDef.Sku,
                    Price = variantDef.Price,
                    Stock = variantDef.Stock,
                    Images = variantDef.Images,
                    AttributeCombination = attributeCombination,
                    IsActive = variantDef.IsActive,
                    CreatedAt = now,
                    LastUpdatedAt = now
                };

                variants.Add(variant);
            }

            if (variants.Count > 0)
            {
                await _variantRepository.AddManyAsync(variants);
            }

            // Update product to indicate it has variants
            product.HasVariants = command.Variants.Count > 0;
            product.LastUpdatedAt = now;
            product.LastUpdatedBy = command.SellerId;
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation(
                "Configured {VariantCount} variants for product {ProductId} by seller {SellerId}",
                command.Variants.Count, command.ProductId, command.SellerId);

            return ConfigureProductVariantsResult.Success(command.Variants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring variants for product {ProductId}", command.ProductId);
            return ConfigureProductVariantsResult.Failure("An error occurred while configuring variants.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateProductVariantResult> UpdateVariantAsync(UpdateProductVariantCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateProductVariantResult.Failure(validationErrors);
        }

        var variant = await _variantRepository.GetByIdAsync(command.VariantId);
        if (variant == null)
        {
            return UpdateProductVariantResult.Failure("Variant not found.");
        }

        var product = await _productRepository.GetByIdAsync(variant.ProductId);
        if (product == null)
        {
            return UpdateProductVariantResult.Failure("Product not found.");
        }

        if (product.StoreId != command.StoreId)
        {
            return UpdateProductVariantResult.NotAuthorized("You are not authorized to update this variant.");
        }

        if (product.Status == ProductStatus.Archived)
        {
            return UpdateProductVariantResult.Failure("Cannot update variants of an archived product.");
        }

        try
        {
            variant.Sku = command.Sku;
            variant.Price = command.Price;
            variant.Stock = command.Stock;
            variant.Images = command.Images;
            variant.IsActive = command.IsActive;
            variant.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _variantRepository.UpdateAsync(variant);

            product.LastUpdatedAt = DateTimeOffset.UtcNow;
            product.LastUpdatedBy = command.SellerId;
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation(
                "Updated variant {VariantId} for product {ProductId} by seller {SellerId}",
                command.VariantId, variant.ProductId, command.SellerId);

            return UpdateProductVariantResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variant {VariantId}", command.VariantId);
            return UpdateProductVariantResult.Failure("An error occurred while updating the variant.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductVariant>> GetVariantsByProductIdAsync(Guid productId)
    {
        return await _variantRepository.GetByProductIdAsync(productId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductVariant>> GetActiveVariantsByProductIdAsync(Guid productId)
    {
        return await _variantRepository.GetActiveByProductIdAsync(productId);
    }

    /// <inheritdoc />
    public async Task<ProductVariant?> GetVariantByIdAsync(Guid id)
    {
        return await _variantRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductVariantAttribute>> GetAttributesByProductIdAsync(Guid productId)
    {
        return await _variantRepository.GetAttributesByProductIdAsync(productId);
    }

    /// <inheritdoc />
    public async Task<ConfigureProductVariantsResult> RemoveVariantsAsync(Guid productId, Guid storeId, string sellerId)
    {
        if (productId == Guid.Empty)
        {
            return ConfigureProductVariantsResult.Failure("Product ID is required.");
        }

        if (storeId == Guid.Empty)
        {
            return ConfigureProductVariantsResult.Failure("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return ConfigureProductVariantsResult.Failure("Seller ID is required.");
        }

        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return ConfigureProductVariantsResult.Failure("Product not found.");
        }

        if (product.StoreId != storeId)
        {
            return ConfigureProductVariantsResult.NotAuthorized("You are not authorized to remove variants from this product.");
        }

        if (product.Status == ProductStatus.Archived)
        {
            return ConfigureProductVariantsResult.Failure("Cannot remove variants from an archived product.");
        }

        try
        {
            await _variantRepository.DeleteVariantsByProductIdAsync(productId);
            await _variantRepository.DeleteAttributesByProductIdAsync(productId);

            product.HasVariants = false;
            product.LastUpdatedAt = DateTimeOffset.UtcNow;
            product.LastUpdatedBy = sellerId;
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation(
                "Removed variants from product {ProductId} by seller {SellerId}",
                productId, sellerId);

            return ConfigureProductVariantsResult.Success(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing variants from product {ProductId}", productId);
            return ConfigureProductVariantsResult.Failure("An error occurred while removing variants.");
        }
    }

    private static List<string> ValidateConfigureCommand(ConfigureProductVariantsCommand command)
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

        foreach (var attr in command.Attributes)
        {
            if (string.IsNullOrWhiteSpace(attr.Name))
            {
                errors.Add("Attribute name is required.");
            }
            else if (attr.Name.Length > ProductVariantValidationConstants.AttributeNameMaxLength)
            {
                errors.Add($"Attribute name '{attr.Name}' exceeds maximum length of {ProductVariantValidationConstants.AttributeNameMaxLength} characters.");
            }

            foreach (var value in attr.Values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"Attribute value cannot be empty for attribute '{attr.Name}'.");
                }
                else if (value.Length > ProductVariantValidationConstants.AttributeValueMaxLength)
                {
                    errors.Add($"Attribute value '{value}' exceeds maximum length of {ProductVariantValidationConstants.AttributeValueMaxLength} characters.");
                }
            }
        }

        foreach (var variant in command.Variants)
        {
            if (variant.Price.HasValue && variant.Price.Value <= 0)
            {
                errors.Add("Variant price must be greater than 0.");
            }

            if (variant.Stock < 0)
            {
                errors.Add("Variant stock cannot be negative.");
            }
        }

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateProductVariantCommand command)
    {
        var errors = new List<string>();

        if (command.VariantId == Guid.Empty)
        {
            errors.Add("Variant ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        if (command.Price.HasValue && command.Price.Value <= 0)
        {
            errors.Add("Variant price must be greater than 0.");
        }

        if (command.Stock < 0)
        {
            errors.Add("Variant stock cannot be negative.");
        }

        return errors;
    }
}
