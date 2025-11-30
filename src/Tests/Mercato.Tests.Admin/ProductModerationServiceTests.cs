using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

/// <summary>
/// Unit tests for the ProductModerationService.
/// </summary>
public class ProductModerationServiceTests
{
    private readonly Mock<IProductModerationRepository> _mockProductModerationRepository;
    private readonly Mock<IStoreRepository> _mockStoreRepository;
    private readonly Mock<IAdminAuditRepository> _mockAdminAuditRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<ProductModerationService>> _mockLogger;
    private readonly ProductModerationService _service;

    public ProductModerationServiceTests()
    {
        _mockProductModerationRepository = new Mock<IProductModerationRepository>(MockBehavior.Strict);
        _mockStoreRepository = new Mock<IStoreRepository>(MockBehavior.Strict);
        _mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        _mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ProductModerationService>>();

        _service = new ProductModerationService(
            _mockProductModerationRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProductModerationRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductModerationService(
            null!,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullStoreRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductModerationService(
            _mockProductModerationRepository.Object,
            null!,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAdminAuditRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductModerationService(
            _mockProductModerationRepository.Object,
            _mockStoreRepository.Object,
            null!,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductModerationService(
            _mockProductModerationRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductModerationService(
            _mockProductModerationRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            null!));
    }

    #endregion

    #region GetProductsForModerationAsync Tests

    [Fact]
    public async Task GetProductsForModerationAsync_ReturnsSuccessWithProducts()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var products = new List<Product.Domain.Entities.Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Title = "Test Product",
                Description = "A great product",
                Price = 29.99m,
                Category = "Electronics",
                Status = ProductStatus.Draft,
                ModerationStatus = ProductModerationStatus.PendingReview,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var stores = new List<Store>
        {
            new()
            {
                Id = storeId,
                Name = "Test Store",
                SellerId = "seller-123"
            }
        };

        var categories = new List<string> { "Electronics", "Clothing" };

        _mockProductModerationRepository
            .Setup(r => r.GetProductsForModerationAsync(
                It.IsAny<IReadOnlyList<ProductModerationStatus>?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((products, 1));

        _mockStoreRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(stores);

        _mockProductModerationRepository
            .Setup(r => r.GetDistinctCategoriesAsync())
            .ReturnsAsync(categories);

        var query = new ProductModerationFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetProductsForModerationAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Products);
        Assert.Equal("Test Product", result.Products[0].Title);
        Assert.Equal("Test Store", result.Products[0].StoreName);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsForModerationAsync_TruncatesLongDescription()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var longDescription = new string('A', 150);
        var products = new List<Product.Domain.Entities.Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Title = "Test Product",
                Description = longDescription,
                Price = 29.99m,
                Category = "Electronics",
                Status = ProductStatus.Active,
                ModerationStatus = ProductModerationStatus.Approved,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockProductModerationRepository
            .Setup(r => r.GetProductsForModerationAsync(
                It.IsAny<IReadOnlyList<ProductModerationStatus>?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((products, 1));

        _mockStoreRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Store>());

        _mockProductModerationRepository
            .Setup(r => r.GetDistinctCategoriesAsync())
            .ReturnsAsync(new List<string>());

        var query = new ProductModerationFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetProductsForModerationAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.EndsWith("...", result.Products[0].DescriptionPreview);
        Assert.True(result.Products[0].DescriptionPreview.Length <= 103);
    }

    #endregion

    #region GetProductDetailsAsync Tests

    [Fact]
    public async Task GetProductDetailsAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product",
            Description = "Product description",
            Price = 49.99m,
            Stock = 10,
            Category = "Electronics",
            Status = ProductStatus.Draft,
            ModerationStatus = ProductModerationStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            SellerId = "seller-123"
        };

        var history = new List<ProductModerationDecision>();

        _mockProductModerationRepository
            .Setup(r => r.GetProductForModerationAsync(productId))
            .ReturnsAsync(product);

        _mockStoreRepository
            .Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        _mockProductModerationRepository
            .Setup(r => r.GetModerationHistoryAsync(productId))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetProductDetailsAsync(productId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ProductDetails);
        Assert.Equal(productId, result.ProductDetails.Id);
        Assert.Equal("Test Product", result.ProductDetails.Title);
        Assert.Equal("Test Store", result.ProductDetails.StoreName);
        Assert.Equal("seller-123", result.ProductDetails.SellerId);
    }

    [Fact]
    public async Task GetProductDetailsAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductModerationRepository
            .Setup(r => r.GetProductForModerationAsync(productId))
            .ReturnsAsync((Product.Domain.Entities.Product?)null);

        // Act
        var result = await _service.GetProductDetailsAsync(productId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    #endregion

    #region ApproveProductAsync Tests

    [Fact]
    public async Task ApproveProductAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product",
            Status = ProductStatus.Draft,
            ModerationStatus = ProductModerationStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            SellerId = "seller-123"
        };

        _mockProductModerationRepository
            .Setup(r => r.GetProductForModerationAsync(productId))
            .ReturnsAsync(product);

        _mockProductModerationRepository
            .Setup(r => r.UpdateModerationStatusAsync(It.IsAny<Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        _mockProductModerationRepository
            .Setup(r => r.AddModerationDecisionAsync(It.IsAny<ProductModerationDecision>()))
            .ReturnsAsync((ProductModerationDecision d) => d);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        _mockStoreRepository
            .Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        _mockNotificationService
            .Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(new CreateNotificationResult { Succeeded = true });

        var command = new ApproveProductCommand
        {
            ProductId = productId,
            AdminUserId = "admin-user",
            Reason = "Product meets guidelines"
        };

        // Act
        var result = await _service.ApproveProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ProductModerationStatus.Approved, product.ModerationStatus);
        Assert.Equal(ProductStatus.Active, product.Status);
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()), Times.Once);
    }

    [Fact]
    public async Task ApproveProductAsync_WithEmptyProductId_ReturnsValidationError()
    {
        // Arrange
        var command = new ApproveProductCommand
        {
            ProductId = Guid.Empty,
            AdminUserId = "admin-user"
        };

        // Act
        var result = await _service.ApproveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task ApproveProductAsync_WithEmptyAdminUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new ApproveProductCommand
        {
            ProductId = Guid.NewGuid(),
            AdminUserId = ""
        };

        // Act
        var result = await _service.ApproveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Admin user ID is required.", result.Errors);
    }

    [Fact]
    public async Task ApproveProductAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductModerationRepository
            .Setup(r => r.GetProductForModerationAsync(productId))
            .ReturnsAsync((Product.Domain.Entities.Product?)null);

        var command = new ApproveProductCommand
        {
            ProductId = productId,
            AdminUserId = "admin-user"
        };

        // Act
        var result = await _service.ApproveProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    #endregion

    #region RejectProductAsync Tests

    [Fact]
    public async Task RejectProductAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product",
            Status = ProductStatus.Active,
            ModerationStatus = ProductModerationStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            SellerId = "seller-123"
        };

        _mockProductModerationRepository
            .Setup(r => r.GetProductForModerationAsync(productId))
            .ReturnsAsync(product);

        _mockProductModerationRepository
            .Setup(r => r.UpdateModerationStatusAsync(It.IsAny<Product.Domain.Entities.Product>()))
            .Returns(Task.CompletedTask);

        _mockProductModerationRepository
            .Setup(r => r.AddModerationDecisionAsync(It.IsAny<ProductModerationDecision>()))
            .ReturnsAsync((ProductModerationDecision d) => d);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        _mockStoreRepository
            .Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        _mockNotificationService
            .Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(new CreateNotificationResult { Succeeded = true });

        var command = new RejectProductCommand
        {
            ProductId = productId,
            AdminUserId = "admin-user",
            Reason = "Violates content guidelines"
        };

        // Act
        var result = await _service.RejectProductAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ProductModerationStatus.Rejected, product.ModerationStatus);
        Assert.Equal(ProductStatus.Inactive, product.Status);
        Assert.Equal("Violates content guidelines", product.ModerationReason);
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()), Times.Once);
    }

    [Fact]
    public async Task RejectProductAsync_WithEmptyProductId_ReturnsValidationError()
    {
        // Arrange
        var command = new RejectProductCommand
        {
            ProductId = Guid.Empty,
            AdminUserId = "admin-user",
            Reason = "Test reason"
        };

        // Act
        var result = await _service.RejectProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product ID is required.", result.Errors);
    }

    [Fact]
    public async Task RejectProductAsync_WithEmptyAdminUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new RejectProductCommand
        {
            ProductId = Guid.NewGuid(),
            AdminUserId = "",
            Reason = "Test reason"
        };

        // Act
        var result = await _service.RejectProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Admin user ID is required.", result.Errors);
    }

    [Fact]
    public async Task RejectProductAsync_WithEmptyReason_ReturnsValidationError()
    {
        // Arrange
        var command = new RejectProductCommand
        {
            ProductId = Guid.NewGuid(),
            AdminUserId = "admin-user",
            Reason = ""
        };

        // Act
        var result = await _service.RejectProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Rejection reason is required.", result.Errors);
    }

    [Fact]
    public async Task RejectProductAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductModerationRepository
            .Setup(r => r.GetProductForModerationAsync(productId))
            .ReturnsAsync((Product.Domain.Entities.Product?)null);

        var command = new RejectProductCommand
        {
            ProductId = productId,
            AdminUserId = "admin-user",
            Reason = "Test reason"
        };

        // Act
        var result = await _service.RejectProductAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    #endregion

    #region BulkModerateProductsAsync Tests

    [Fact]
    public async Task BulkModerateProductsAsync_ApproveMultiple_ReturnsSuccess()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var productIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var products = productIds.Select(id => new Product.Domain.Entities.Product
        {
            Id = id,
            StoreId = storeId,
            Title = $"Product {id}",
            Status = ProductStatus.Draft,
            ModerationStatus = ProductModerationStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        }).ToList();

        var stores = new List<Store>
        {
            new() { Id = storeId, Name = "Test Store", SellerId = "seller-123" }
        };

        _mockProductModerationRepository
            .Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        _mockProductModerationRepository
            .Setup(r => r.AddModerationDecisionAsync(It.IsAny<ProductModerationDecision>()))
            .ReturnsAsync((ProductModerationDecision d) => d);

        _mockProductModerationRepository
            .Setup(r => r.UpdateModerationStatusBulkAsync(It.IsAny<IEnumerable<Product.Domain.Entities.Product>>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        _mockStoreRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(stores);

        _mockNotificationService
            .Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(new CreateNotificationResult { Succeeded = true });

        var command = new BulkModerateProductsCommand
        {
            ProductIds = productIds,
            AdminUserId = "admin-user",
            Approve = true,
            Reason = "Bulk approved"
        };

        // Act
        var result = await _service.BulkModerateProductsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.SuccessCount);
    }

    [Fact]
    public async Task BulkModerateProductsAsync_WithEmptyProductIds_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkModerateProductsCommand
        {
            ProductIds = new List<Guid>(),
            AdminUserId = "admin-user",
            Approve = true
        };

        // Act
        var result = await _service.BulkModerateProductsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("At least one product ID is required.", result.Errors);
    }

    [Fact]
    public async Task BulkModerateProductsAsync_RejectWithoutReason_ReturnsValidationError()
    {
        // Arrange
        var command = new BulkModerateProductsCommand
        {
            ProductIds = new List<Guid> { Guid.NewGuid() },
            AdminUserId = "admin-user",
            Approve = false,
            Reason = ""
        };

        // Act
        var result = await _service.BulkModerateProductsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Rejection reason is required for bulk rejection.", result.Errors);
    }

    #endregion
}
