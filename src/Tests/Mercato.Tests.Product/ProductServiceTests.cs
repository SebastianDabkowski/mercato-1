using Mercato.Product.Application.Commands;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

public class ProductServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly string TestSellerId = "test-seller-id";

    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _mockRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ProductService>>();
        _service = new ProductService(_mockRepository.Object, _mockLogger.Object);
    }

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product p) => p);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ProductId);
        Assert.NotEqual(Guid.Empty, result.ProductId.Value);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.StoreId == command.StoreId &&
            p.Title == command.Title &&
            p.Description == command.Description &&
            p.Price == command.Price &&
            p.Stock == command.Stock &&
            p.Category == command.Category &&
            p.Status == ProductStatus.Draft
        )), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ProductCreatedWithDraftStatus()
    {
        // Arrange
        var command = CreateValidCommand();
        Mercato.Product.Domain.Entities.Product? capturedProduct = null;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Callback<Mercato.Product.Domain.Entities.Product>(p => capturedProduct = p)
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product p) => p);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedProduct);
        Assert.Equal(ProductStatus.Draft, capturedProduct.Status);
    }

    [Fact]
    public async Task CreateProductAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.StoreId = Guid.Empty;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_EmptyTitle_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Title = string.Empty;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Title is required.", result.Errors);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Theory]
    [InlineData("A")]
    public async Task CreateProductAsync_TitleTooShort_ReturnsFailure(string title)
    {
        // Arrange
        var command = CreateValidCommand();
        command.Title = title;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Title must be between {ProductValidationConstants.TitleMinLength} and {ProductValidationConstants.TitleMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_TitleTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Title = new string('A', ProductValidationConstants.TitleMaxLength + 1);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Title must be between {ProductValidationConstants.TitleMinLength} and {ProductValidationConstants.TitleMaxLength} characters.", result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateProductAsync_InvalidPrice_ReturnsFailure(decimal price)
    {
        // Arrange
        var command = CreateValidCommand();
        command.Price = price;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Price must be greater than 0.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_NegativeStock_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Stock = -1;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Stock cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_EmptyCategory_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Category = string.Empty;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Category is required.", result.Errors);
    }

    [Theory]
    [InlineData("A")]
    public async Task CreateProductAsync_CategoryTooShort_ReturnsFailure(string category)
    {
        // Arrange
        var command = CreateValidCommand();
        command.Category = category;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Category must be between {ProductValidationConstants.CategoryMinLength} and {ProductValidationConstants.CategoryMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_CategoryTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Category = new string('A', ProductValidationConstants.CategoryMaxLength + 1);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Category must be between {ProductValidationConstants.CategoryMinLength} and {ProductValidationConstants.CategoryMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_DescriptionTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Description = new string('A', ProductValidationConstants.DescriptionMaxLength + 1);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Description must be at most {ProductValidationConstants.DescriptionMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_NullDescription_Succeeds()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Description = null;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product p) => p);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Description == null
        )), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ZeroStock_Succeeds()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Stock = 0;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product p) => p);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CreateProductAsync_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            StoreId = Guid.Empty,
            Title = string.Empty,
            Price = -1,
            Stock = -1,
            Category = string.Empty
        };

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count >= 4);
        Assert.Contains("Store ID is required.", result.Errors);
        Assert.Contains("Title is required.", result.Errors);
        Assert.Contains("Price must be greater than 0.", result.Errors);
        Assert.Contains("Category is required.", result.Errors);
    }

    #endregion

    #region GetProductByIdAsync Tests

    [Fact]
    public async Task GetProductByIdAsync_WhenProductExists_ReturnsProduct()
    {
        // Arrange
        var expectedProduct = CreateTestProduct();
        _mockRepository.Setup(r => r.GetByIdAsync(TestProductId))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _service.GetProductByIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProduct.Id, result.Id);
        Assert.Equal(expectedProduct.Title, result.Title);
        _mockRepository.Verify(r => r.GetByIdAsync(TestProductId), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductNotExists_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(TestProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.GetProductByIdAsync(TestProductId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(TestProductId), Times.Once);
    }

    #endregion

    #region GetProductsByStoreIdAsync Tests

    [Fact]
    public async Task GetProductsByStoreIdAsync_WhenProductsExist_ReturnsProducts()
    {
        // Arrange
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(),
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetProductsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetByStoreIdAsync(TestStoreId), Times.Once);
    }

    [Fact]
    public async Task GetProductsByStoreIdAsync_WhenNoProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());

        // Act
        var result = await _service.GetProductsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetByStoreIdAsync(TestStoreId), Times.Once);
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var product = CreateTestProduct();

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Title == command.Title &&
            p.Description == command.Description &&
            p.Price == command.Price &&
            p.Stock == command.Stock &&
            p.Category == command.Category &&
            p.LastUpdatedBy == command.SellerId
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_NotOwner_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store ID

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("You are not authorized to update this product.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ArchivedProduct_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var product = CreateTestProduct();
        product.Status = ProductStatus.Archived;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot update an archived product.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ValidationErrors_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateProductCommand
        {
            ProductId = Guid.Empty,
            SellerId = string.Empty,
            StoreId = Guid.Empty,
            Title = string.Empty,
            Price = -1,
            Stock = -1,
            Category = string.Empty
        };

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
        Assert.Contains("Store ID is required.", result.Errors);
        Assert.Contains("Seller ID is required.", result.Errors);
        Assert.Contains("Title is required.", result.Errors);
        Assert.Contains("Category is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateProductAsync_EmptyProductId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.ProductId = Guid.Empty;

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateProductAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.SellerId = string.Empty;

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    #endregion

    #region ArchiveProductAsync Tests

    [Fact]
    public async Task ArchiveProductAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidArchiveCommand();
        var product = CreateTestProduct();

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ArchiveProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Archived &&
            p.ArchivedBy == command.SellerId &&
            p.ArchivedAt != null
        )), Times.Once);
    }

    [Fact]
    public async Task ArchiveProductAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidArchiveCommand();

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.ArchiveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveProductAsync_NotOwner_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidArchiveCommand();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store ID

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ArchiveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("You are not authorized to archive this product.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveProductAsync_AlreadyArchived_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidArchiveCommand();
        var product = CreateTestProduct();
        product.Status = ProductStatus.Archived;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ArchiveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product is already archived.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveProductAsync_ValidationErrors_ReturnsFailure()
    {
        // Arrange
        var command = new ArchiveProductCommand
        {
            ProductId = Guid.Empty,
            SellerId = string.Empty,
            StoreId = Guid.Empty
        };

        // Act
        var result = await _service.ArchiveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
        Assert.Contains("Store ID is required.", result.Errors);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    #endregion

    #region GetActiveProductsByStoreIdAsync Tests

    [Fact]
    public async Task GetActiveProductsByStoreIdAsync_WhenProductsExist_ReturnsActiveProducts()
    {
        // Arrange
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(),
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(TestStoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetActiveProductsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetActiveByStoreIdAsync(TestStoreId), Times.Once);
    }

    [Fact]
    public async Task GetActiveProductsByStoreIdAsync_WhenNoProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(TestStoreId))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());

        // Act
        var result = await _service.GetActiveProductsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetActiveByStoreIdAsync(TestStoreId), Times.Once);
    }

    [Fact]
    public async Task GetActiveProductsByStoreIdAsync_ExcludesArchivedProducts()
    {
        // Arrange
        var activeProduct = CreateTestProduct();
        var products = new List<Mercato.Product.Domain.Entities.Product> { activeProduct };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(TestStoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetActiveProductsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, p => Assert.NotEqual(ProductStatus.Archived, p.Status));
        _mockRepository.Verify(r => r.GetActiveByStoreIdAsync(TestStoreId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static CreateProductCommand CreateValidCommand()
    {
        return new CreateProductCommand
        {
            StoreId = TestStoreId,
            Title = "Test Product",
            Description = "A test product description",
            Price = 99.99m,
            Stock = 100,
            Category = "Electronics"
        };
    }

    private static UpdateProductCommand CreateValidUpdateCommand()
    {
        return new UpdateProductCommand
        {
            ProductId = TestProductId,
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            Title = "Updated Product",
            Description = "An updated product description",
            Price = 149.99m,
            Stock = 50,
            Category = "Updated Category"
        };
    }

    private static ArchiveProductCommand CreateValidArchiveCommand()
    {
        return new ArchiveProductCommand
        {
            ProductId = TestProductId,
            SellerId = TestSellerId,
            StoreId = TestStoreId
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
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
