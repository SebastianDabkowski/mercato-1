using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Infrastructure;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Buyer;

public class RecentlyViewedServiceTests
{
    private static readonly Guid TestProductId1 = Guid.NewGuid();
    private static readonly Guid TestProductId2 = Guid.NewGuid();
    private static readonly Guid TestProductId3 = Guid.NewGuid();

    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<ILogger<RecentlyViewedService>> _mockLogger;
    private readonly RecentlyViewedService _service;

    public RecentlyViewedServiceTests()
    {
        _mockRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<RecentlyViewedService>>();
        _service = new RecentlyViewedService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetRecentlyViewedProductsAsync Tests

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_EmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var productIds = new List<Guid>();

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_NullList_ReturnsEmptyResult()
    {
        // Arrange
        IEnumerable<Guid>? productIds = null;

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_ValidIds_ReturnsActiveProductsInOrder()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1, TestProductId2 };
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateActiveProduct(TestProductId1, "Product 1", 10.99m),
            CreateActiveProduct(TestProductId2, "Product 2", 20.99m)
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(TestProductId1, result.Products[0].Id);
        Assert.Equal(TestProductId2, result.Products[1].Id);
        Assert.Equal("Product 1", result.Products[0].Title);
        Assert.Equal("Product 2", result.Products[1].Title);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_FiltersOutInactiveProducts()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1, TestProductId2, TestProductId3 };
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateActiveProduct(TestProductId1, "Active Product 1", 10.99m),
            CreateProduct(TestProductId2, "Draft Product", 20.99m, ProductStatus.Draft),
            CreateActiveProduct(TestProductId3, "Active Product 2", 30.99m)
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(TestProductId1, result.Products[0].Id);
        Assert.Equal(TestProductId3, result.Products[1].Id);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_FiltersOutArchivedProducts()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1, TestProductId2 };
        var archivedProduct = CreateActiveProduct(TestProductId2, "Archived Product", 20.99m);
        archivedProduct.ArchivedAt = DateTimeOffset.UtcNow;

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateActiveProduct(TestProductId1, "Active Product", 10.99m),
            archivedProduct
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Products);
        Assert.Equal(TestProductId1, result.Products[0].Id);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_PreservesOrderFromInput()
    {
        // Arrange - IDs in specific order (most recent first)
        var productIds = new List<Guid> { TestProductId2, TestProductId1 };
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateActiveProduct(TestProductId1, "Product 1", 10.99m),
            CreateActiveProduct(TestProductId2, "Product 2", 20.99m)
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(TestProductId2, result.Products[0].Id);
        Assert.Equal(TestProductId1, result.Products[1].Id);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_RespectsMaxItemsLimit()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1, TestProductId2, TestProductId3 };
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateActiveProduct(TestProductId1, "Product 1", 10.99m),
            CreateActiveProduct(TestProductId2, "Product 2", 20.99m),
            CreateActiveProduct(TestProductId3, "Product 3", 30.99m)
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 2)))
            .ReturnsAsync(products.Take(2).ToList());

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds, maxItems: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Products.Count);
        _mockRepository.Verify(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 2)), Times.Once);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_MapsProductFieldsCorrectly()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1 };
        var product = CreateActiveProduct(TestProductId1, "Test Product", 25.99m);
        product.Stock = 10;
        product.Images = "[\"/uploads/test.jpg\"]";

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product> { product });

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Products);
        var dto = result.Products[0];
        Assert.Equal(TestProductId1, dto.Id);
        Assert.Equal("Test Product", dto.Title);
        Assert.Equal(25.99m, dto.Price);
        Assert.Equal("/uploads/test.jpg", dto.ImageUrl);
        Assert.True(dto.IsInStock);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_OutOfStockProduct_ReturnsIsInStockFalse()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1 };
        var product = CreateActiveProduct(TestProductId1, "Out of Stock Product", 15.99m);
        product.Stock = 0;

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product> { product });

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Products);
        Assert.False(result.Products[0].IsInStock);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_NoImages_ReturnsNullImageUrl()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1 };
        var product = CreateActiveProduct(TestProductId1, "No Image Product", 15.99m);
        product.Images = null;

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product> { product });

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Products);
        Assert.Null(result.Products[0].ImageUrl);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_InvalidImageUrl_ReturnsNullImageUrl()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1 };
        var product = CreateActiveProduct(TestProductId1, "Invalid Image Product", 15.99m);
        product.Images = "[\"https://external.com/image.jpg\"]";

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product> { product });

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Products);
        Assert.Null(result.Products[0].ImageUrl);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_RepositoryThrows_ReturnsEmptyResult()
    {
        // Arrange
        var productIds = new List<Guid> { TestProductId1 };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task GetRecentlyViewedProductsAsync_ProductNotFound_ExcludedFromResults()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var productIds = new List<Guid> { TestProductId1, nonExistentId };
        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateActiveProduct(TestProductId1, "Found Product", 10.99m)
        };

        _mockRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetRecentlyViewedProductsAsync(productIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Products);
        Assert.Equal(TestProductId1, result.Products[0].Id);
    }

    #endregion

    #region Helper Methods

    private static Mercato.Product.Domain.Entities.Product CreateActiveProduct(Guid id, string title, decimal price)
    {
        return CreateProduct(id, title, price, ProductStatus.Active);
    }

    private static Mercato.Product.Domain.Entities.Product CreateProduct(Guid id, string title, decimal price, ProductStatus status)
    {
        return new Mercato.Product.Domain.Entities.Product
        {
            Id = id,
            Title = title,
            Price = price,
            Status = status,
            Stock = 10,
            StoreId = Guid.NewGuid(),
            Category = "Test Category",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
