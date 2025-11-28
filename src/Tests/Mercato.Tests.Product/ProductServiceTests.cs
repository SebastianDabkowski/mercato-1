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
        Assert.True(result.IsNotAuthorized);
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
        Assert.True(result.IsNotAuthorized);
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

    #region Shipping Parameter Validation Tests

    [Fact]
    public async Task CreateProductAsync_ValidShippingParameters_Succeeds()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Weight = 2.5m;
        command.Length = 30.0m;
        command.Width = 20.0m;
        command.Height = 10.0m;
        command.ShippingMethods = "standard,express";
        command.Images = "[\"https://example.com/image1.jpg\"]";

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product p) => p);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Weight == 2.5m &&
            p.Length == 30.0m &&
            p.Width == 20.0m &&
            p.Height == 10.0m &&
            p.ShippingMethods == "standard,express" &&
            p.Images == "[\"https://example.com/image1.jpg\"]"
        )), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_NegativeWeight_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Weight = -1.0m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Weight cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_WeightExceedsMax_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Weight = 1001m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Weight must be at most {ProductValidationConstants.WeightMaxKg} kg.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_NegativeLength_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Length = -1.0m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Length cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_LengthExceedsMax_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Length = 501m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Length must be at most {ProductValidationConstants.DimensionMaxCm} cm.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_NegativeWidth_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Width = -1.0m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Width cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_WidthExceedsMax_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Width = 501m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Width must be at most {ProductValidationConstants.DimensionMaxCm} cm.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_NegativeHeight_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Height = -1.0m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Height cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_HeightExceedsMax_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Height = 501m;

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Height must be at most {ProductValidationConstants.DimensionMaxCm} cm.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_ShippingMethodsTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ShippingMethods = new string('A', ProductValidationConstants.ShippingMethodsMaxLength + 1);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Shipping methods must be at most {ProductValidationConstants.ShippingMethodsMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateProductAsync_ImagesTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Images = new string('A', ProductValidationConstants.ImagesMaxLength + 1);

        // Act
        var result = await _service.CreateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Images must be at most {ProductValidationConstants.ImagesMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task UpdateProductAsync_ValidShippingParameters_Succeeds()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.Weight = 2.5m;
        command.Length = 30.0m;
        command.Width = 20.0m;
        command.Height = 10.0m;
        command.ShippingMethods = "standard,express";
        command.Images = "[\"https://example.com/image1.jpg\"]";

        var product = CreateTestProduct();

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Weight == 2.5m &&
            p.Length == 30.0m &&
            p.Width == 20.0m &&
            p.Height == 10.0m &&
            p.ShippingMethods == "standard,express" &&
            p.Images == "[\"https://example.com/image1.jpg\"]"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_NegativeWeight_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.Weight = -1.0m;

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Weight cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task UpdateProductAsync_NullShippingParameters_Succeeds()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.Weight = null;
        command.Length = null;
        command.Width = null;
        command.Height = null;
        command.ShippingMethods = null;
        command.Images = null;

        var product = CreateTestProduct();

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
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

    #region ChangeProductStatusAsync Tests

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToActive_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;
        product.Description = "Valid description";
        product.Images = "[\"https://example.com/image1.jpg\"]";

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Active
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToActive_MissingDescription_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;
        product.Description = null;
        product.Images = "[\"https://example.com/image1.jpg\"]";

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Description is required to set product to Active.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToActive_MissingImages_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;
        product.Description = "Valid description";
        product.Images = null;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("At least one image is required to set product to Active.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToActive_EmptyImages_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;
        product.Description = "Valid description";
        product.Images = "[]";

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("At least one image is required to set product to Active.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ActiveToSuspended_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Suspended);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Suspended
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ActiveToDraft_WithoutAdmin_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Draft);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot transition from Active to Draft. This transition requires admin approval.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ActiveToDraft_WithAdminOverride_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Draft);
        command.IsAdminOverride = true;
        var product = CreateTestProduct();
        product.Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Draft
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ArchivedProduct_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Archived;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot change the status of an archived product.", result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()), Times.Never);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ToArchived_SetsArchivedFields()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Archived);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Archived &&
            p.ArchivedAt != null &&
            p.ArchivedBy == command.SellerId
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_NotOwner_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains("You are not authorized to change this product's status.", result.Errors);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_AdminOverride_BypassesStoreCheck()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Suspended);
        command.IsAdminOverride = true;
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store, but admin override should allow

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Suspended
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_SameStatus_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Draft);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_SuspendedToActive_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Suspended;
        product.Description = "Valid description";
        product.Images = "[\"https://example.com/image1.jpg\"]";

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Active
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToSuspended_WithoutAdmin_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Suspended);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot transition from Draft to Suspended. Only Active or Archived transitions are allowed.", result.Errors);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToSuspended_WithAdminOverride_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Suspended);
        command.IsAdminOverride = true;
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Mercato.Product.Domain.Entities.Product>(p =>
            p.Status == ProductStatus.Suspended
        )), Times.Once);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_ValidationErrors_ReturnsFailure()
    {
        // Arrange
        var command = new ChangeProductStatusCommand
        {
            ProductId = Guid.Empty,
            SellerId = string.Empty,
            StoreId = Guid.Empty,
            NewStatus = ProductStatus.Active
        };

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
        Assert.Contains("Store ID is required.", result.Errors);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_AdminOverride_EmptyStoreIdAllowed()
    {
        // Arrange
        var command = new ChangeProductStatusCommand
        {
            ProductId = TestProductId,
            SellerId = "admin-user",
            StoreId = Guid.Empty,
            NewStatus = ProductStatus.Suspended,
            IsAdminOverride = true
        };
        var product = CreateTestProduct();
        product.Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Mercato.Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ChangeProductStatusAsync_DraftToActive_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = CreateValidChangeStatusCommand(ProductStatus.Active);
        var product = CreateTestProduct();
        product.Status = ProductStatus.Draft;
        product.Description = null;
        product.Category = string.Empty;
        product.Price = 0;
        product.Images = null;

        _mockRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.ChangeProductStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count >= 4);
        Assert.Contains("Description is required to set product to Active.", result.Errors);
        Assert.Contains("Category is required to set product to Active.", result.Errors);
        Assert.Contains("Price must be greater than 0 to set product to Active.", result.Errors);
        Assert.Contains("At least one image is required to set product to Active.", result.Errors);
    }

    private static ChangeProductStatusCommand CreateValidChangeStatusCommand(ProductStatus newStatus)
    {
        return new ChangeProductStatusCommand
        {
            ProductId = TestProductId,
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            NewStatus = newStatus,
            IsAdminOverride = false
        };
    }

    #endregion

    #region BulkUpdatePriceStockAsync Tests

    [Fact]
    public async Task BulkUpdatePriceStockAsync_FixedPrice_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1),
            CreateTestProduct(productId2)
        };
        products[0].Price = 50.00m;
        products[1].Price = 75.00m;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1, productId2],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.All(products, p => Assert.Equal(100.00m, p.Price));
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_PercentageIncrease_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Price = 100.00m;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.PercentageIncrease,
                Value = 10
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(110.00m, products[0].Price);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_PercentageDecrease_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Price = 100.00m;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.PercentageDecrease,
                Value = 20
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(80.00m, products[0].Price);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_FixedStock_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Stock = 50;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            StockUpdate = new BulkStockUpdate
            {
                UpdateType = BulkStockUpdateType.Fixed,
                Value = 100
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(100, products[0].Stock);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_IncreaseStock_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Stock = 50;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            StockUpdate = new BulkStockUpdate
            {
                UpdateType = BulkStockUpdateType.Increase,
                Value = 25
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(75, products[0].Stock);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_DecreaseStock_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Stock = 50;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            StockUpdate = new BulkStockUpdate
            {
                UpdateType = BulkStockUpdateType.Decrease,
                Value = 20
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(30, products[0].Stock);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_BothPriceAndStock_UpdatesAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Price = 100.00m;
        products[0].Stock = 50;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 200.00m
            },
            StockUpdate = new BulkStockUpdate
            {
                UpdateType = BulkStockUpdateType.Fixed,
                Value = 100
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(200.00m, products[0].Price);
        Assert.Equal(100, products[0].Stock);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_NegativeResultingPrice_FailsForThatProduct()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Price = 10.00m;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.AmountDecrease,
                Value = 20.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains(result.FailedProducts, f => f.Error.Contains("zero or negative"));
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_NegativeResultingStock_FailsForThatProduct()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Stock = 10;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            StockUpdate = new BulkStockUpdate
            {
                UpdateType = BulkStockUpdateType.Decrease,
                Value = 20
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains(result.FailedProducts, f => f.Error.Contains("negative"));
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_ArchivedProduct_FailsForThatProduct()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1),
            CreateTestProduct(productId2)
        };
        products[0].Status = ProductStatus.Archived;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1, productId2],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains(result.FailedProducts, f => f.ProductId == productId1 && f.Error.Contains("archived"));
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_WrongStoreId_FailsForThatProduct()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].StoreId = Guid.NewGuid(); // Different store

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains(result.FailedProducts, f => f.Error.Contains("not authorized"));
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_EmptyProductIds_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("At least one product ID is required.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_NoUpdateSpecified_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [Guid.NewGuid()],
            PriceUpdate = null,
            StockUpdate = null
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("At least one update (price or stock) must be specified.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_EmptyStoreId_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = Guid.Empty,
            ProductIds = [Guid.NewGuid()],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_EmptySellerId_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = string.Empty,
            StoreId = TestStoreId,
            ProductIds = [Guid.NewGuid()],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_NoProductsFound_ReturnsFailure()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [Guid.NewGuid()],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("No products found with the specified IDs.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_FixedPriceZero_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [Guid.NewGuid()],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 0
            }
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Fixed price must be greater than 0.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_NegativeFixedStock_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [Guid.NewGuid()],
            StockUpdate = new BulkStockUpdate
            {
                UpdateType = BulkStockUpdateType.Fixed,
                Value = -1
            }
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Fixed stock cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_PercentageDecreaseOver100_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [Guid.NewGuid()],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.PercentageDecrease,
                Value = 150
            }
        };

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Percentage decrease cannot exceed 100%.", result.Errors);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_UpdatesLastUpdatedFields()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        var originalUpdatedAt = products[0].LastUpdatedAt;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.Fixed,
                Value = 100.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(TestSellerId, products[0].LastUpdatedBy);
        Assert.True(products[0].LastUpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_AmountIncrease_UpdatesPrice()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Price = 100.00m;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.AmountIncrease,
                Value = 25.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(125.00m, products[0].Price);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_AmountDecrease_UpdatesPrice()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1)
        };
        products[0].Price = 100.00m;

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.AmountDecrease,
                Value = 25.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(75.00m, products[0].Price);
    }

    [Fact]
    public async Task BulkUpdatePriceStockAsync_PartialSuccess_ReturnsSuccessWithFailures()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(productId1),
            CreateTestProduct(productId2)
        };
        products[0].Price = 100.00m;
        products[1].Price = 10.00m; // This will result in negative price

        var command = new BulkUpdatePriceStockCommand
        {
            SellerId = TestSellerId,
            StoreId = TestStoreId,
            ProductIds = [productId1, productId2],
            PriceUpdate = new BulkPriceUpdate
            {
                UpdateType = BulkPriceUpdateType.AmountDecrease,
                Value = 50.00m
            }
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<Mercato.Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdatePriceStockAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Equal(50.00m, products[0].Price);
        Assert.Contains(result.FailedProducts, f => f.ProductId == productId2);
    }

    #endregion

    #region ExportProductCatalogAsync Tests

    [Fact]
    public async Task ExportProductCatalogAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidExportCommand();
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid()),
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.FileContent);
        Assert.NotNull(result.FileName);
        Assert.NotNull(result.ContentType);
        Assert.Equal(2, result.ExportedCount);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_CsvFormat_ReturnsCorrectContentType()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.Format = ExportFormat.Csv;
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("text/csv", result.ContentType);
        Assert.EndsWith(".csv", result.FileName);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_ExcelFormat_ReturnsCorrectContentType()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.Format = ExportFormat.Excel;
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.EndsWith(".xlsx", result.FileName);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.StoreId = Guid.Empty;

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.SellerId = string.Empty;

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_NoProducts_ReturnsSuccessWithZeroCount()
    {
        // Arrange
        var command = CreateValidExportCommand();

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExportedCount);
        Assert.NotNull(result.FileContent);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_WithSearchFilter_AppliesFilter()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.ApplyFilters = true;
        command.SearchQuery = "Test Product";

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid()), // Title: "Test Product"
            new Mercato.Product.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId,
                Title = "Other Item",
                Description = "Description",
                Price = 10.00m,
                Stock = 10,
                Category = "Category"
            }
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.ExportedCount); // Only "Test Product" matches
    }

    [Fact]
    public async Task ExportProductCatalogAsync_WithCategoryFilter_AppliesFilter()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.ApplyFilters = true;
        command.CategoryFilter = "Electronics";

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            new Mercato.Product.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId,
                Title = "Product 1",
                Category = "Electronics",
                Price = 10.00m,
                Stock = 10
            },
            new Mercato.Product.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId,
                Title = "Product 2",
                Category = "Clothing",
                Price = 10.00m,
                Stock = 10
            }
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.ExportedCount);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_WithStatusFilter_AppliesFilter()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.ApplyFilters = true;
        command.StatusFilter = ProductStatus.Active;

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            new Mercato.Product.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId,
                Title = "Active Product",
                Category = "Category",
                Price = 10.00m,
                Stock = 10,
                Status = ProductStatus.Active
            },
            new Mercato.Product.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                StoreId = TestStoreId,
                Title = "Draft Product",
                Category = "Category",
                Price = 10.00m,
                Stock = 10,
                Status = ProductStatus.Draft
            }
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.ExportedCount);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_FiltersNotApplied_ExportsAllProducts()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.ApplyFilters = false;
        command.SearchQuery = "ShouldBeIgnored";
        command.CategoryFilter = "ShouldBeIgnored";

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid()),
            CreateTestProduct(Guid.NewGuid()),
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.ExportedCount);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_CsvContainsCorrectHeaders()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.Format = ExportFormat.Csv;

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(products);

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.FileContent!);
        Assert.Contains("SKU", csvContent);
        Assert.Contains("Title", csvContent);
        Assert.Contains("Description", csvContent);
        Assert.Contains("Price", csvContent);
        Assert.Contains("Stock", csvContent);
        Assert.Contains("Category", csvContent);
        Assert.Contains("Status", csvContent);
    }

    [Fact]
    public async Task ExportProductCatalogAsync_CsvContainsProductData()
    {
        // Arrange
        var command = CreateValidExportCommand();
        command.Format = ExportFormat.Csv;

        var product = CreateTestProduct(Guid.NewGuid());
        product.Sku = "SKU123";
        product.Title = "Test Product Title";
        product.Category = "Test Category";

        _mockRepository.Setup(r => r.GetActiveByStoreIdAsync(command.StoreId))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product> { product });

        // Act
        var result = await _service.ExportProductCatalogAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        var csvContent = System.Text.Encoding.UTF8.GetString(result.FileContent!);
        Assert.Contains("SKU123", csvContent);
        Assert.Contains("Test Product Title", csvContent);
        Assert.Contains("Test Category", csvContent);
    }

    private static ExportProductCatalogCommand CreateValidExportCommand()
    {
        return new ExportProductCatalogCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            Format = ExportFormat.Csv,
            ApplyFilters = false
        };
    }

    #endregion

    #region GetProductsByCategoryAsync Tests

    [Fact]
    public async Task GetProductsByCategoryAsync_ReturnsProductsForCategory()
    {
        // Arrange
        var categoryName = "Electronics";
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid()),
            CreateTestProduct(Guid.NewGuid())
        };
        products[0].Category = categoryName;
        products[0].Status = ProductStatus.Active;
        products[1].Category = categoryName;
        products[1].Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.GetActiveByCategoryAsync(categoryName, 1, 12))
            .ReturnsAsync((products, 2));

        // Act
        var (resultProducts, totalCount) = await _service.GetProductsByCategoryAsync(categoryName, 1, 12);

        // Assert
        Assert.NotNull(resultProducts);
        Assert.Equal(2, resultProducts.Count);
        Assert.Equal(2, totalCount);
        _mockRepository.Verify(r => r.GetActiveByCategoryAsync(categoryName, 1, 12), Times.Once);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_WhenNoProducts_ReturnsEmptyList()
    {
        // Arrange
        var categoryName = "EmptyCategory";

        _mockRepository.Setup(r => r.GetActiveByCategoryAsync(categoryName, 1, 12))
            .ReturnsAsync((new List<Mercato.Product.Domain.Entities.Product>(), 0));

        // Act
        var (resultProducts, totalCount) = await _service.GetProductsByCategoryAsync(categoryName, 1, 12);

        // Assert
        Assert.NotNull(resultProducts);
        Assert.Empty(resultProducts);
        Assert.Equal(0, totalCount);
        _mockRepository.Verify(r => r.GetActiveByCategoryAsync(categoryName, 1, 12), Times.Once);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var categoryName = "Electronics";
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid())
        };
        products[0].Category = categoryName;
        products[0].Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.GetActiveByCategoryAsync(categoryName, 2, 10))
            .ReturnsAsync((products, 15));

        // Act
        var (resultProducts, totalCount) = await _service.GetProductsByCategoryAsync(categoryName, 2, 10);

        // Assert
        Assert.NotNull(resultProducts);
        Assert.Single(resultProducts);
        Assert.Equal(15, totalCount);
        _mockRepository.Verify(r => r.GetActiveByCategoryAsync(categoryName, 2, 10), Times.Once);
    }

    #endregion

    #region SearchProductsAsync Tests

    [Fact]
    public async Task SearchProductsAsync_ReturnsMatchingProducts()
    {
        // Arrange
        var searchQuery = "laptop";
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid()),
            CreateTestProduct(Guid.NewGuid())
        };
        products[0].Title = "Gaming Laptop";
        products[0].Status = ProductStatus.Active;
        products[1].Title = "Work Laptop";
        products[1].Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.SearchActiveProductsAsync(searchQuery, 1, 12))
            .ReturnsAsync((products, 2));

        // Act
        var (resultProducts, totalCount) = await _service.SearchProductsAsync(searchQuery, 1, 12);

        // Assert
        Assert.NotNull(resultProducts);
        Assert.Equal(2, resultProducts.Count);
        Assert.Equal(2, totalCount);
        _mockRepository.Verify(r => r.SearchActiveProductsAsync(searchQuery, 1, 12), Times.Once);
    }

    [Fact]
    public async Task SearchProductsAsync_WhenNoProducts_ReturnsEmptyList()
    {
        // Arrange
        var searchQuery = "nonexistent";

        _mockRepository.Setup(r => r.SearchActiveProductsAsync(searchQuery, 1, 12))
            .ReturnsAsync((new List<Mercato.Product.Domain.Entities.Product>(), 0));

        // Act
        var (resultProducts, totalCount) = await _service.SearchProductsAsync(searchQuery, 1, 12);

        // Assert
        Assert.NotNull(resultProducts);
        Assert.Empty(resultProducts);
        Assert.Equal(0, totalCount);
        _mockRepository.Verify(r => r.SearchActiveProductsAsync(searchQuery, 1, 12), Times.Once);
    }

    [Fact]
    public async Task SearchProductsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var searchQuery = "phone";
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid())
        };
        products[0].Title = "Smart Phone";
        products[0].Status = ProductStatus.Active;

        _mockRepository.Setup(r => r.SearchActiveProductsAsync(searchQuery, 2, 10))
            .ReturnsAsync((products, 15));

        // Act
        var (resultProducts, totalCount) = await _service.SearchProductsAsync(searchQuery, 2, 10);

        // Assert
        Assert.NotNull(resultProducts);
        Assert.Single(resultProducts);
        Assert.Equal(15, totalCount);
        _mockRepository.Verify(r => r.SearchActiveProductsAsync(searchQuery, 2, 10), Times.Once);
    }

    #endregion
}
