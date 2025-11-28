using Mercato.Product.Application.Commands;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

public class ProductVariantServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestVariantId = Guid.NewGuid();
    private static readonly string TestSellerId = "test-seller-id";

    private readonly Mock<IProductVariantRepository> _mockVariantRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ILogger<ProductVariantService>> _mockLogger;
    private readonly ProductVariantService _service;

    public ProductVariantServiceTests()
    {
        _mockVariantRepository = new Mock<IProductVariantRepository>(MockBehavior.Strict);
        _mockProductRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ProductVariantService>>();
        _service = new ProductVariantService(
            _mockVariantRepository.Object,
            _mockProductRepository.Object,
            _mockLogger.Object);
    }

    #region ConfigureVariantsAsync Tests

    [Fact]
    public async Task ConfigureVariantsAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        var product = CreateTestProduct();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockVariantRepository.Setup(r => r.DeleteVariantsByProductIdAsync(command.ProductId))
            .Returns(Task.CompletedTask);
        _mockVariantRepository.Setup(r => r.DeleteAttributesByProductIdAsync(command.ProductId))
            .Returns(Task.CompletedTask);
        _mockVariantRepository.Setup(r => r.AddAttributeAsync(It.IsAny<ProductVariantAttribute>()))
            .ReturnsAsync((ProductVariantAttribute a) => a);
        _mockVariantRepository.Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<ProductVariant>>()))
            .Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.VariantCount);
        _mockVariantRepository.Verify(r => r.AddAttributeAsync(It.IsAny<ProductVariantAttribute>()), Times.Once);
        _mockVariantRepository.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<ProductVariant>>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_EmptyProductId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.ProductId = Guid.Empty;

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.StoreId = Guid.Empty;

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.SellerId = string.Empty;

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_NotOwner_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains("You are not authorized to configure variants for this product.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_ArchivedProduct_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        var product = CreateTestProduct();
        product.Status = ProductStatus.Archived;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot configure variants for an archived product.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_TooManyAttributes_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.Attributes = Enumerable.Range(1, ProductVariantValidationConstants.MaxAttributesPerProduct + 1)
            .Select(i => new VariantAttributeDefinition
            {
                Name = $"Attribute{i}",
                Values = ["Value1"]
            })
            .ToList();

        var product = CreateTestProduct();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Maximum {ProductVariantValidationConstants.MaxAttributesPerProduct} variant attributes allowed per product.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_TooManyVariants_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.Variants = Enumerable.Range(1, ProductVariantValidationConstants.MaxVariantsPerProduct + 1)
            .Select(i => new VariantDefinition
            {
                AttributeValues = new Dictionary<string, string> { { "Size", $"Value{i}" } },
                Stock = 10
            })
            .ToList();

        var product = CreateTestProduct();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Maximum {ProductVariantValidationConstants.MaxVariantsPerProduct} variants allowed per product.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_NegativeVariantPrice_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.Variants[0].Price = -10m;

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Variant price must be greater than 0.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_NegativeVariantStock_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        command.Variants[0].Stock = -10;

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Variant stock cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task ConfigureVariantsAsync_UpdatesProductHasVariantsFlag()
    {
        // Arrange
        var command = CreateValidConfigureCommand();
        var product = CreateTestProduct();
        Mercato.Product.Domain.Entities.Product? capturedProduct = null;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockVariantRepository.Setup(r => r.DeleteVariantsByProductIdAsync(command.ProductId))
            .Returns(Task.CompletedTask);
        _mockVariantRepository.Setup(r => r.DeleteAttributesByProductIdAsync(command.ProductId))
            .Returns(Task.CompletedTask);
        _mockVariantRepository.Setup(r => r.AddAttributeAsync(It.IsAny<ProductVariantAttribute>()))
            .ReturnsAsync((ProductVariantAttribute a) => a);
        _mockVariantRepository.Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<ProductVariant>>()))
            .Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Callback<Mercato.Product.Domain.Entities.Product>(p => capturedProduct = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfigureVariantsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedProduct);
        Assert.True(capturedProduct.HasVariants);
    }

    #endregion

    #region UpdateVariantAsync Tests

    [Fact]
    public async Task UpdateVariantAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var variant = CreateTestVariant();
        var product = CreateTestProduct();

        _mockVariantRepository.Setup(r => r.GetByIdAsync(command.VariantId))
            .ReturnsAsync(variant);
        _mockProductRepository.Setup(r => r.GetByIdAsync(variant.ProductId))
            .ReturnsAsync(product);
        _mockVariantRepository.Setup(r => r.UpdateAsync(It.IsAny<ProductVariant>()))
            .Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockVariantRepository.Verify(r => r.UpdateAsync(It.Is<ProductVariant>(v =>
            v.Sku == command.Sku &&
            v.Price == command.Price &&
            v.Stock == command.Stock &&
            v.IsActive == command.IsActive
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateVariantAsync_EmptyVariantId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.VariantId = Guid.Empty;

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Variant ID is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateVariantAsync_VariantNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();

        _mockVariantRepository.Setup(r => r.GetByIdAsync(command.VariantId))
            .ReturnsAsync((ProductVariant?)null);

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Variant not found.", result.Errors);
    }

    [Fact]
    public async Task UpdateVariantAsync_NotOwner_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var variant = CreateTestVariant();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockVariantRepository.Setup(r => r.GetByIdAsync(command.VariantId))
            .ReturnsAsync(variant);
        _mockProductRepository.Setup(r => r.GetByIdAsync(variant.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains("You are not authorized to update this variant.", result.Errors);
    }

    [Fact]
    public async Task UpdateVariantAsync_ArchivedProduct_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var variant = CreateTestVariant();
        var product = CreateTestProduct();
        product.Status = ProductStatus.Archived;

        _mockVariantRepository.Setup(r => r.GetByIdAsync(command.VariantId))
            .ReturnsAsync(variant);
        _mockProductRepository.Setup(r => r.GetByIdAsync(variant.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot update variants of an archived product.", result.Errors);
    }

    [Fact]
    public async Task UpdateVariantAsync_NegativePrice_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.Price = -10m;

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Variant price must be greater than 0.", result.Errors);
    }

    [Fact]
    public async Task UpdateVariantAsync_NegativeStock_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.Stock = -10;

        // Act
        var result = await _service.UpdateVariantAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Variant stock cannot be negative.", result.Errors);
    }

    #endregion

    #region GetVariantsByProductIdAsync Tests

    [Fact]
    public async Task GetVariantsByProductIdAsync_WhenVariantsExist_ReturnsVariants()
    {
        // Arrange
        var variants = new List<ProductVariant>
        {
            CreateTestVariant(),
            CreateTestVariant(Guid.NewGuid())
        };

        _mockVariantRepository.Setup(r => r.GetByProductIdAsync(TestProductId))
            .ReturnsAsync(variants);

        // Act
        var result = await _service.GetVariantsByProductIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetVariantsByProductIdAsync_WhenNoVariants_ReturnsEmptyList()
    {
        // Arrange
        _mockVariantRepository.Setup(r => r.GetByProductIdAsync(TestProductId))
            .ReturnsAsync(new List<ProductVariant>());

        // Act
        var result = await _service.GetVariantsByProductIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetActiveVariantsByProductIdAsync Tests

    [Fact]
    public async Task GetActiveVariantsByProductIdAsync_ReturnsOnlyActiveVariants()
    {
        // Arrange
        var variants = new List<ProductVariant>
        {
            CreateTestVariant()
        };

        _mockVariantRepository.Setup(r => r.GetActiveByProductIdAsync(TestProductId))
            .ReturnsAsync(variants);

        // Act
        var result = await _service.GetActiveVariantsByProductIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    #endregion

    #region GetVariantByIdAsync Tests

    [Fact]
    public async Task GetVariantByIdAsync_WhenVariantExists_ReturnsVariant()
    {
        // Arrange
        var variant = CreateTestVariant();

        _mockVariantRepository.Setup(r => r.GetByIdAsync(TestVariantId))
            .ReturnsAsync(variant);

        // Act
        var result = await _service.GetVariantByIdAsync(TestVariantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestVariantId, result.Id);
    }

    [Fact]
    public async Task GetVariantByIdAsync_WhenVariantNotExists_ReturnsNull()
    {
        // Arrange
        _mockVariantRepository.Setup(r => r.GetByIdAsync(TestVariantId))
            .ReturnsAsync((ProductVariant?)null);

        // Act
        var result = await _service.GetVariantByIdAsync(TestVariantId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAttributesByProductIdAsync Tests

    [Fact]
    public async Task GetAttributesByProductIdAsync_WhenAttributesExist_ReturnsAttributes()
    {
        // Arrange
        var attributes = new List<ProductVariantAttribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = TestProductId,
                Name = "Size",
                Values =
                [
                    new ProductVariantAttributeValue { Id = Guid.NewGuid(), Value = "Small" },
                    new ProductVariantAttributeValue { Id = Guid.NewGuid(), Value = "Large" }
                ]
            }
        };

        _mockVariantRepository.Setup(r => r.GetAttributesByProductIdAsync(TestProductId))
            .ReturnsAsync(attributes);

        // Act
        var result = await _service.GetAttributesByProductIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Size", result[0].Name);
        Assert.Equal(2, result[0].Values.Count);
    }

    #endregion

    #region RemoveVariantsAsync Tests

    [Fact]
    public async Task RemoveVariantsAsync_ValidParameters_ReturnsSuccess()
    {
        // Arrange
        var product = CreateTestProduct();
        product.HasVariants = true;

        _mockProductRepository.Setup(r => r.GetByIdAsync(TestProductId))
            .ReturnsAsync(product);
        _mockVariantRepository.Setup(r => r.DeleteVariantsByProductIdAsync(TestProductId))
            .Returns(Task.CompletedTask);
        _mockVariantRepository.Setup(r => r.DeleteAttributesByProductIdAsync(TestProductId))
            .Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RemoveVariantsAsync(TestProductId, TestStoreId, TestSellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.VariantCount);
    }

    [Fact]
    public async Task RemoveVariantsAsync_EmptyProductId_ReturnsFailure()
    {
        // Act
        var result = await _service.RemoveVariantsAsync(Guid.Empty, TestStoreId, TestSellerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task RemoveVariantsAsync_NotOwner_ReturnsNotAuthorized()
    {
        // Arrange
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockProductRepository.Setup(r => r.GetByIdAsync(TestProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.RemoveVariantsAsync(TestProductId, TestStoreId, TestSellerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task RemoveVariantsAsync_SetsHasVariantsToFalse()
    {
        // Arrange
        var product = CreateTestProduct();
        product.HasVariants = true;
        Mercato.Product.Domain.Entities.Product? capturedProduct = null;

        _mockProductRepository.Setup(r => r.GetByIdAsync(TestProductId))
            .ReturnsAsync(product);
        _mockVariantRepository.Setup(r => r.DeleteVariantsByProductIdAsync(TestProductId))
            .Returns(Task.CompletedTask);
        _mockVariantRepository.Setup(r => r.DeleteAttributesByProductIdAsync(TestProductId))
            .Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Callback<Mercato.Product.Domain.Entities.Product>(p => capturedProduct = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RemoveVariantsAsync(TestProductId, TestStoreId, TestSellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedProduct);
        Assert.False(capturedProduct.HasVariants);
    }

    #endregion

    #region Helper Methods

    private static ConfigureProductVariantsCommand CreateValidConfigureCommand()
    {
        return new ConfigureProductVariantsCommand
        {
            ProductId = TestProductId,
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            Attributes =
            [
                new VariantAttributeDefinition
                {
                    Name = "Size",
                    Values = ["Small", "Large"]
                }
            ],
            Variants =
            [
                new VariantDefinition
                {
                    AttributeValues = new Dictionary<string, string> { { "Size", "Small" } },
                    Sku = "PROD-SM",
                    Price = 99.99m,
                    Stock = 50,
                    IsActive = true
                },
                new VariantDefinition
                {
                    AttributeValues = new Dictionary<string, string> { { "Size", "Large" } },
                    Sku = "PROD-LG",
                    Price = 109.99m,
                    Stock = 30,
                    IsActive = true
                }
            ]
        };
    }

    private static UpdateProductVariantCommand CreateValidUpdateCommand()
    {
        return new UpdateProductVariantCommand
        {
            VariantId = TestVariantId,
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            Sku = "PROD-SM-UPDATED",
            Price = 89.99m,
            Stock = 100,
            IsActive = true
        };
    }

    private static Mercato.Product.Domain.Entities.Product CreateTestProduct(Guid? id = null)
    {
        return new Mercato.Product.Domain.Entities.Product
        {
            Id = id ?? TestProductId,
            StoreId = TestStoreId,
            Title = "Test Product",
            Description = "A test product description",
            Price = 99.99m,
            Stock = 100,
            Category = "Electronics",
            Status = ProductStatus.Draft,
            HasVariants = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static ProductVariant CreateTestVariant(Guid? id = null)
    {
        return new ProductVariant
        {
            Id = id ?? TestVariantId,
            ProductId = TestProductId,
            Sku = "PROD-SM",
            Price = 99.99m,
            Stock = 50,
            AttributeCombination = "{\"Size\":\"Small\"}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
