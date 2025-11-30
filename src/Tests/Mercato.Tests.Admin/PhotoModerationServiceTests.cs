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
/// Unit tests for the PhotoModerationService.
/// </summary>
public class PhotoModerationServiceTests
{
    private readonly Mock<IPhotoModerationRepository> _mockPhotoModerationRepository;
    private readonly Mock<IProductImageRepository> _mockProductImageRepository;
    private readonly Mock<IStoreRepository> _mockStoreRepository;
    private readonly Mock<IAdminAuditRepository> _mockAdminAuditRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<PhotoModerationService>> _mockLogger;
    private readonly PhotoModerationService _service;

    public PhotoModerationServiceTests()
    {
        _mockPhotoModerationRepository = new Mock<IPhotoModerationRepository>(MockBehavior.Strict);
        _mockProductImageRepository = new Mock<IProductImageRepository>(MockBehavior.Strict);
        _mockStoreRepository = new Mock<IStoreRepository>(MockBehavior.Strict);
        _mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        _mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<PhotoModerationService>>();

        _service = new PhotoModerationService(
            _mockPhotoModerationRepository.Object,
            _mockProductImageRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPhotoModerationRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PhotoModerationService(
            null!,
            _mockProductImageRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullProductImageRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PhotoModerationService(
            _mockPhotoModerationRepository.Object,
            null!,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullStoreRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PhotoModerationService(
            _mockPhotoModerationRepository.Object,
            _mockProductImageRepository.Object,
            null!,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAdminAuditRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PhotoModerationService(
            _mockPhotoModerationRepository.Object,
            _mockProductImageRepository.Object,
            _mockStoreRepository.Object,
            null!,
            _mockNotificationService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PhotoModerationService(
            _mockPhotoModerationRepository.Object,
            _mockProductImageRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PhotoModerationService(
            _mockPhotoModerationRepository.Object,
            _mockProductImageRepository.Object,
            _mockStoreRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockNotificationService.Object,
            null!));
    }

    #endregion

    #region GetPhotosForModerationAsync Tests

    [Fact]
    public async Task GetPhotosForModerationAsync_ReturnsSuccessWithPhotos()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var photos = new List<ProductImage>
        {
            new()
            {
                Id = imageId,
                ProductId = productId,
                FileName = "test.jpg",
                StoragePath = "/uploads/test.jpg",
                ContentType = "image/jpeg",
                FileSize = 1024,
                ModerationStatus = PhotoModerationStatus.PendingReview,
                IsFlagged = false,
                CreatedAt = DateTimeOffset.UtcNow,
                Product = product
            }
        };

        var stores = new List<Store>
        {
            new() { Id = storeId, Name = "Test Store", SellerId = "seller-123" }
        };

        _mockPhotoModerationRepository
            .Setup(r => r.GetPendingPhotosAsync(null, false, 0, 20))
            .ReturnsAsync((photos, 1));

        _mockStoreRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(stores);

        var query = new PhotoModerationFilterQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _service.GetPhotosForModerationAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Photos);
        Assert.Equal(imageId, result.Photos[0].Id);
        Assert.Equal("Test Product", result.Photos[0].ProductTitle);
        Assert.Equal("Test Store", result.Photos[0].StoreName);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPhotosForModerationAsync_WithFlaggedOnly_FiltersCorrectly()
    {
        // Arrange
        _mockPhotoModerationRepository
            .Setup(r => r.GetPendingPhotosAsync(null, true, 0, 20))
            .ReturnsAsync((new List<ProductImage>(), 0));

        _mockStoreRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Store>());

        var query = new PhotoModerationFilterQuery { Page = 1, PageSize = 20, FlaggedOnly = true };

        // Act
        var result = await _service.GetPhotosForModerationAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Photos);
        _mockPhotoModerationRepository.Verify(r => r.GetPendingPhotosAsync(null, true, 0, 20), Times.Once);
    }

    #endregion

    #region GetPhotoDetailsAsync Tests

    [Fact]
    public async Task GetPhotoDetailsAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product"
        };

        var photo = new ProductImage
        {
            Id = imageId,
            ProductId = productId,
            FileName = "test.jpg",
            StoragePath = "/uploads/test.jpg",
            ContentType = "image/jpeg",
            FileSize = 2048,
            IsMain = true,
            ModerationStatus = PhotoModerationStatus.PendingReview,
            IsFlagged = true,
            FlagReason = "Reported by user",
            CreatedAt = DateTimeOffset.UtcNow,
            Product = product
        };

        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            SellerId = "seller-123"
        };

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync(photo);

        _mockStoreRepository
            .Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        _mockPhotoModerationRepository
            .Setup(r => r.GetModerationHistoryAsync(imageId))
            .ReturnsAsync(new List<PhotoModerationDecision>());

        _mockProductImageRepository
            .Setup(r => r.GetImageCountByProductIdAsync(productId))
            .ReturnsAsync(3);

        // Act
        var result = await _service.GetPhotoDetailsAsync(imageId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.PhotoDetails);
        Assert.Equal(imageId, result.PhotoDetails.Id);
        Assert.Equal("Test Product", result.PhotoDetails.ProductTitle);
        Assert.Equal("Test Store", result.PhotoDetails.StoreName);
        Assert.Equal("seller-123", result.PhotoDetails.SellerId);
        Assert.True(result.PhotoDetails.IsFlagged);
        Assert.Equal("Reported by user", result.PhotoDetails.FlagReason);
        Assert.Equal(2, result.PhotoDetails.OtherProductImagesCount);
    }

    [Fact]
    public async Task GetPhotoDetailsAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync((ProductImage?)null);

        // Act
        var result = await _service.GetPhotoDetailsAsync(imageId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Photo not found.", result.Errors);
    }

    #endregion

    #region ApprovePhotoAsync Tests

    [Fact]
    public async Task ApprovePhotoAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product"
        };

        var photo = new ProductImage
        {
            Id = imageId,
            ProductId = productId,
            FileName = "test.jpg",
            StoragePath = "/uploads/test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            ModerationStatus = PhotoModerationStatus.PendingReview,
            IsFlagged = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Product = product
        };

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync(photo);

        _mockPhotoModerationRepository
            .Setup(r => r.UpdatePhotoModerationStatusAsync(It.IsAny<ProductImage>()))
            .Returns(Task.CompletedTask);

        _mockPhotoModerationRepository
            .Setup(r => r.AddModerationDecisionAsync(It.IsAny<PhotoModerationDecision>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new ApprovePhotoCommand
        {
            ImageId = imageId,
            AdminUserId = "admin-user",
            Reason = "Photo is appropriate"
        };

        // Act
        var result = await _service.ApprovePhotoAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(PhotoModerationStatus.Approved, photo.ModerationStatus);
        Assert.False(photo.IsFlagged);
        _mockPhotoModerationRepository.Verify(r => r.UpdatePhotoModerationStatusAsync(photo), Times.Once);
        _mockPhotoModerationRepository.Verify(r => r.AddModerationDecisionAsync(It.IsAny<PhotoModerationDecision>()), Times.Once);
    }

    [Fact]
    public async Task ApprovePhotoAsync_WithEmptyImageId_ReturnsValidationError()
    {
        // Arrange
        var command = new ApprovePhotoCommand
        {
            ImageId = Guid.Empty,
            AdminUserId = "admin-user"
        };

        // Act
        var result = await _service.ApprovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image ID is required.", result.Errors);
    }

    [Fact]
    public async Task ApprovePhotoAsync_WithEmptyAdminUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new ApprovePhotoCommand
        {
            ImageId = Guid.NewGuid(),
            AdminUserId = ""
        };

        // Act
        var result = await _service.ApprovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Admin user ID is required.", result.Errors);
    }

    [Fact]
    public async Task ApprovePhotoAsync_PhotoNotFound_ReturnsFailure()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync((ProductImage?)null);

        var command = new ApprovePhotoCommand
        {
            ImageId = imageId,
            AdminUserId = "admin-user"
        };

        // Act
        var result = await _service.ApprovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Photo not found.", result.Errors);
    }

    #endregion

    #region RemovePhotoAsync Tests

    [Fact]
    public async Task RemovePhotoAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product"
        };

        var photo = new ProductImage
        {
            Id = imageId,
            ProductId = productId,
            FileName = "test.jpg",
            StoragePath = "/uploads/test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            ModerationStatus = PhotoModerationStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow,
            Product = product
        };

        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            SellerId = "seller-123"
        };

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync(photo);

        _mockPhotoModerationRepository
            .Setup(r => r.UpdatePhotoModerationStatusAsync(It.IsAny<ProductImage>()))
            .Returns(Task.CompletedTask);

        _mockPhotoModerationRepository
            .Setup(r => r.AddModerationDecisionAsync(It.IsAny<PhotoModerationDecision>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        _mockStoreRepository
            .Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        _mockNotificationService
            .Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(new CreateNotificationResult { Succeeded = true });

        var command = new RemovePhotoCommand
        {
            ImageId = imageId,
            AdminUserId = "admin-user",
            Reason = "Inappropriate content"
        };

        // Act
        var result = await _service.RemovePhotoAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(PhotoModerationStatus.Removed, photo.ModerationStatus);
        Assert.Equal("Inappropriate content", photo.ModerationReason);
        Assert.Equal("seller-123", result.SellerIdToNotify);
        Assert.Equal("Test Product", result.ProductTitle);
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()), Times.Once);
    }

    [Fact]
    public async Task RemovePhotoAsync_WithEmptyImageId_ReturnsValidationError()
    {
        // Arrange
        var command = new RemovePhotoCommand
        {
            ImageId = Guid.Empty,
            AdminUserId = "admin-user",
            Reason = "Test reason"
        };

        // Act
        var result = await _service.RemovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Image ID is required.", result.Errors);
    }

    [Fact]
    public async Task RemovePhotoAsync_WithEmptyAdminUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new RemovePhotoCommand
        {
            ImageId = Guid.NewGuid(),
            AdminUserId = "",
            Reason = "Test reason"
        };

        // Act
        var result = await _service.RemovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Admin user ID is required.", result.Errors);
    }

    [Fact]
    public async Task RemovePhotoAsync_WithEmptyReason_ReturnsValidationError()
    {
        // Arrange
        var command = new RemovePhotoCommand
        {
            ImageId = Guid.NewGuid(),
            AdminUserId = "admin-user",
            Reason = ""
        };

        // Act
        var result = await _service.RemovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Removal reason is required.", result.Errors);
    }

    [Fact]
    public async Task RemovePhotoAsync_PhotoNotFound_ReturnsFailure()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync((ProductImage?)null);

        var command = new RemovePhotoCommand
        {
            ImageId = imageId,
            AdminUserId = "admin-user",
            Reason = "Test reason"
        };

        // Act
        var result = await _service.RemovePhotoAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Photo not found.", result.Errors);
    }

    [Fact]
    public async Task RemovePhotoAsync_WhenPhotoIsOnlyOneForProduct_StillRemoves()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var product = new Product.Domain.Entities.Product
        {
            Id = productId,
            StoreId = storeId,
            Title = "Test Product"
        };

        var photo = new ProductImage
        {
            Id = imageId,
            ProductId = productId,
            FileName = "only-image.jpg",
            StoragePath = "/uploads/only-image.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            IsMain = true,
            ModerationStatus = PhotoModerationStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow,
            Product = product
        };

        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            SellerId = "seller-123"
        };

        _mockPhotoModerationRepository
            .Setup(r => r.GetPhotoByIdAsync(imageId))
            .ReturnsAsync(photo);

        _mockPhotoModerationRepository
            .Setup(r => r.UpdatePhotoModerationStatusAsync(It.IsAny<ProductImage>()))
            .Returns(Task.CompletedTask);

        _mockPhotoModerationRepository
            .Setup(r => r.AddModerationDecisionAsync(It.IsAny<PhotoModerationDecision>()))
            .Returns(Task.CompletedTask);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        _mockStoreRepository
            .Setup(r => r.GetByIdAsync(storeId))
            .ReturnsAsync(store);

        _mockNotificationService
            .Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationCommand>()))
            .ReturnsAsync(new CreateNotificationResult { Succeeded = true });

        var command = new RemovePhotoCommand
        {
            ImageId = imageId,
            AdminUserId = "admin-user",
            Reason = "Violates guidelines"
        };

        // Act
        var result = await _service.RemovePhotoAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(PhotoModerationStatus.Removed, photo.ModerationStatus);
    }

    #endregion

    #region GetPendingPhotoCountAsync Tests

    [Fact]
    public async Task GetPendingPhotoCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _mockPhotoModerationRepository
            .Setup(r => r.GetPendingPhotoCountAsync())
            .ReturnsAsync(5);

        // Act
        var result = await _service.GetPendingPhotoCountAsync();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetPendingPhotoCountAsync_WhenRepositoryThrows_ReturnsZero()
    {
        // Arrange
        _mockPhotoModerationRepository
            .Setup(r => r.GetPendingPhotoCountAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetPendingPhotoCountAsync();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion
}
