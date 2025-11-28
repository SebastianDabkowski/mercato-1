using Mercato.Product.Application.Commands;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

/// <summary>
/// Unit tests for the ProductImageService.
/// </summary>
public class ProductImageServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly string TestSellerId = "test-seller-id";

    private readonly Mock<IProductImageRepository> _mockImageRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ILogger<ProductImageService>> _mockLogger;
    private readonly string _testUploadsPath;
    private readonly ProductImageService _service;

    public ProductImageServiceTests()
    {
        _mockImageRepository = new Mock<IProductImageRepository>(MockBehavior.Strict);
        _mockProductRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ProductImageService>>();

        // Use a temporary directory for tests
        _testUploadsPath = Path.Combine(Path.GetTempPath(), $"ProductImageTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testUploadsPath);

        _service = new ProductImageService(
            _mockImageRepository.Object,
            _mockProductRepository.Object,
            _mockLogger.Object,
            _testUploadsPath);
    }

    #region UploadImageAsync Tests

    [Fact]
    public async Task UploadImageAsync_EmptyProductId_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.ProductId = Guid.Empty;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task UploadImageAsync_EmptyStoreId_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.StoreId = Guid.Empty;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task UploadImageAsync_EmptySellerId_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.SellerId = string.Empty;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task UploadImageAsync_EmptyFileName_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileName = string.Empty;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("File name is required.", result.Errors);
    }

    [Theory]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData(".tiff")]
    [InlineData(".exe")]
    [InlineData(".pdf")]
    public async Task UploadImageAsync_UnsupportedFileExtension_ReturnsValidationError(string extension)
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileName = $"test{extension}";

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not allowed") && e.Contains(extension));
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".JPG")]
    [InlineData(".JPEG")]
    [InlineData(".PNG")]
    [InlineData(".WEBP")]
    public async Task UploadImageAsync_SupportedFileExtension_PassesValidation(string extension)
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileName = $"test{extension}";

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetImageCountByProductIdAsync(command.ProductId))
            .ReturnsAsync(0);
        _mockImageRepository.Setup(r => r.AddAsync(It.IsAny<ProductImage>()))
            .ReturnsAsync((ProductImage img) => img);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert - Passes validation (may fail on actual file processing but that's OK)
        Assert.DoesNotContain(result.Errors, e => e.Contains("extension") && e.Contains("not allowed"));
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/bmp")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    public async Task UploadImageAsync_UnsupportedContentType_ReturnsValidationError(string contentType)
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.ContentType = contentType;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Content type") && e.Contains("not allowed"));
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task UploadImageAsync_SupportedContentType_PassesValidation(string contentType)
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.ContentType = contentType;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetImageCountByProductIdAsync(command.ProductId))
            .ReturnsAsync(0);
        _mockImageRepository.Setup(r => r.AddAsync(It.IsAny<ProductImage>()))
            .ReturnsAsync((ProductImage img) => img);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Contains("Content type") && e.Contains("not allowed"));
    }

    [Fact]
    public async Task UploadImageAsync_ZeroFileSize_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileSize = 0;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("File size must be greater than 0.", result.Errors);
    }

    [Fact]
    public async Task UploadImageAsync_FileSizeExceedsLimit_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileSize = ProductImageValidationConstants.MaxFileSizeBytes + 1;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("exceeds maximum allowed size"));
    }

    [Fact]
    public async Task UploadImageAsync_FileAtExactLimit_PassesValidation()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileSize = ProductImageValidationConstants.MaxFileSizeBytes;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetImageCountByProductIdAsync(command.ProductId))
            .ReturnsAsync(0);
        _mockImageRepository.Setup(r => r.AddAsync(It.IsAny<ProductImage>()))
            .ReturnsAsync((ProductImage img) => img);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Contains("exceeds maximum allowed size"));
    }

    [Fact]
    public async Task UploadImageAsync_NullFileStream_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileStream = null;

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("File stream is required.", result.Errors);
    }

    [Fact]
    public async Task UploadImageAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        var command = CreateValidUploadCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task UploadImageAsync_WrongStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains(result.Errors, e => e.Contains("not authorized"));
    }

    [Fact]
    public async Task UploadImageAsync_MaxImagesReached_ReturnsError()
    {
        // Arrange
        var command = CreateValidUploadCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetImageCountByProductIdAsync(command.ProductId))
            .ReturnsAsync(ProductImageValidationConstants.MaxImagesPerProduct);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Maximum number of images"));
    }

    [Fact]
    public async Task UploadImageAsync_FirstImage_SetsAsMain()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.SetAsMain = false;
        ProductImage? capturedImage = null;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetImageCountByProductIdAsync(command.ProductId))
            .ReturnsAsync(0);
        _mockImageRepository.Setup(r => r.AddAsync(It.IsAny<ProductImage>()))
            .Callback<ProductImage>(img => capturedImage = img)
            .ReturnsAsync((ProductImage img) => img);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedImage);
        Assert.True(capturedImage.IsMain);
    }

    [Fact]
    public async Task UploadImageAsync_SetAsMainTrue_SetsAsMain()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.SetAsMain = true;
        ProductImage? capturedImage = null;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetImageCountByProductIdAsync(command.ProductId))
            .ReturnsAsync(1);
        _mockImageRepository.Setup(r => r.AddAsync(It.IsAny<ProductImage>()))
            .Callback<ProductImage>(img => capturedImage = img)
            .ReturnsAsync((ProductImage img) => img);
        _mockImageRepository.Setup(r => r.SetMainImageAsync(command.ProductId, It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedImage);
        Assert.True(capturedImage.IsMain);
        _mockImageRepository.Verify(r => r.SetMainImageAsync(command.ProductId, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new UploadProductImageCommand
        {
            ProductId = Guid.Empty,
            StoreId = Guid.Empty,
            SellerId = string.Empty,
            FileName = string.Empty,
            ContentType = string.Empty,
            FileSize = 0,
            FileStream = null
        };

        // Act
        var result = await _service.UploadImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count >= 5);
    }

    #endregion

    #region DeleteImageAsync Tests

    [Fact]
    public async Task DeleteImageAsync_EmptyImageId_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        command.ImageId = Guid.Empty;

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image ID is required.", result.Errors);
    }

    [Fact]
    public async Task DeleteImageAsync_EmptyProductId_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        command.ProductId = Guid.Empty;

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task DeleteImageAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        var command = CreateValidDeleteCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task DeleteImageAsync_WrongStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task DeleteImageAsync_ImageNotFound_ReturnsError()
    {
        // Arrange
        var command = CreateValidDeleteCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetByIdAsync(command.ImageId))
            .ReturnsAsync((ProductImage?)null);

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image not found.", result.Errors);
    }

    [Fact]
    public async Task DeleteImageAsync_ImageBelongsToDifferentProduct_ReturnsError()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var image = CreateTestImage();
        image.ProductId = Guid.NewGuid(); // Different product

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetByIdAsync(command.ImageId))
            .ReturnsAsync(image);

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image does not belong to the specified product.", result.Errors);
    }

    [Fact]
    public async Task DeleteImageAsync_ValidCommand_DeletesImage()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var image = CreateTestImage();
        image.ProductId = command.ProductId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetByIdAsync(command.ImageId))
            .ReturnsAsync(image);
        _mockImageRepository.Setup(r => r.DeleteAsync(command.ImageId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockImageRepository.Verify(r => r.DeleteAsync(command.ImageId), Times.Once);
    }

    [Fact]
    public async Task DeleteImageAsync_DeletingMainImage_SetsNewMainImage()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var image = CreateTestImage();
        image.ProductId = command.ProductId;
        image.IsMain = true;

        var remainingImages = new List<ProductImage>
        {
            new() { Id = Guid.NewGuid(), ProductId = command.ProductId, IsMain = false }
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetByIdAsync(command.ImageId))
            .ReturnsAsync(image);
        _mockImageRepository.Setup(r => r.DeleteAsync(command.ImageId))
            .Returns(Task.CompletedTask);
        _mockImageRepository.Setup(r => r.GetByProductIdAsync(command.ProductId))
            .ReturnsAsync(remainingImages);
        _mockImageRepository.Setup(r => r.SetMainImageAsync(command.ProductId, remainingImages[0].Id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteImageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockImageRepository.Verify(r => r.SetMainImageAsync(command.ProductId, remainingImages[0].Id), Times.Once);
    }

    #endregion

    #region SetMainImageAsync Tests

    [Fact]
    public async Task SetMainImageAsync_EmptyImageId_ReturnsValidationError()
    {
        // Arrange
        var command = CreateValidSetMainCommand();
        command.ImageId = Guid.Empty;

        // Act
        var result = await _service.SetMainImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image ID is required.", result.Errors);
    }

    [Fact]
    public async Task SetMainImageAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        var command = CreateValidSetMainCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync((Mercato.Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.SetMainImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task SetMainImageAsync_WrongStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidSetMainCommand();
        var product = CreateTestProduct();
        product.StoreId = Guid.NewGuid(); // Different store

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.SetMainImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task SetMainImageAsync_ImageNotFound_ReturnsError()
    {
        // Arrange
        var command = CreateValidSetMainCommand();

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetByIdAsync(command.ImageId))
            .ReturnsAsync((ProductImage?)null);

        // Act
        var result = await _service.SetMainImageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image not found.", result.Errors);
    }

    [Fact]
    public async Task SetMainImageAsync_ValidCommand_SetsMainImage()
    {
        // Arrange
        var command = CreateValidSetMainCommand();
        var image = CreateTestImage();
        image.ProductId = command.ProductId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(command.ProductId))
            .ReturnsAsync(CreateTestProduct());
        _mockImageRepository.Setup(r => r.GetByIdAsync(command.ImageId))
            .ReturnsAsync(image);
        _mockImageRepository.Setup(r => r.SetMainImageAsync(command.ProductId, command.ImageId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetMainImageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockImageRepository.Verify(r => r.SetMainImageAsync(command.ProductId, command.ImageId), Times.Once);
    }

    #endregion

    #region GetImagesByProductIdAsync Tests

    [Fact]
    public async Task GetImagesByProductIdAsync_ReturnsImages()
    {
        // Arrange
        var images = new List<ProductImage>
        {
            CreateTestImage(),
            CreateTestImage(Guid.NewGuid())
        };

        _mockImageRepository.Setup(r => r.GetByProductIdAsync(TestProductId))
            .ReturnsAsync(images);

        // Act
        var result = await _service.GetImagesByProductIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetImagesByProductIdAsync_NoImages_ReturnsEmptyList()
    {
        // Arrange
        _mockImageRepository.Setup(r => r.GetByProductIdAsync(TestProductId))
            .ReturnsAsync(new List<ProductImage>());

        // Act
        var result = await _service.GetImagesByProductIdAsync(TestProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private UploadProductImageCommand CreateValidUploadCommand()
    {
        // Create a minimal valid image file stream (1x1 JPEG)
        var jpegBytes = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAn/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBEQCEAwEPwAB//9k=");
        var stream = new MemoryStream(jpegBytes);

        return new UploadProductImageCommand
        {
            ProductId = TestProductId,
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "test-image.jpg",
            ContentType = "image/jpeg",
            FileSize = jpegBytes.Length,
            FileStream = stream,
            SetAsMain = false
        };
    }

    private static DeleteProductImageCommand CreateValidDeleteCommand()
    {
        return new DeleteProductImageCommand
        {
            ImageId = Guid.NewGuid(),
            ProductId = TestProductId,
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };
    }

    private static SetMainProductImageCommand CreateValidSetMainCommand()
    {
        return new SetMainProductImageCommand
        {
            ImageId = Guid.NewGuid(),
            ProductId = TestProductId,
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };
    }

    private static Mercato.Product.Domain.Entities.Product CreateTestProduct()
    {
        return new Mercato.Product.Domain.Entities.Product
        {
            Id = TestProductId,
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

    private static ProductImage CreateTestImage(Guid? id = null)
    {
        return new ProductImage
        {
            Id = id ?? Guid.NewGuid(),
            ProductId = TestProductId,
            FileName = "test-image.jpg",
            StoragePath = "uploads/products/test/test-image.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            IsMain = false,
            DisplayOrder = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
