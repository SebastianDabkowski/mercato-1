using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class StoreProfileServiceTests
{
    private const string TestSellerId = "seller-test-123";
    private static readonly Guid TestStoreId = Guid.NewGuid();

    private readonly Mock<IStoreRepository> _mockRepository;
    private readonly Mock<ILogger<StoreProfileService>> _mockLogger;
    private readonly StoreProfileService _service;

    public StoreProfileServiceTests()
    {
        _mockRepository = new Mock<IStoreRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<StoreProfileService>>();
        _service = new StoreProfileService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetStoreBySellerIdAsync Tests

    [Fact]
    public async Task GetStoreBySellerIdAsync_WhenStoreExists_ReturnsStore()
    {
        // Arrange
        var expectedStore = CreateTestStore();
        _mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(expectedStore);

        // Act
        var result = await _service.GetStoreBySellerIdAsync(TestSellerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStore.Id, result.Id);
        Assert.Equal(expectedStore.SellerId, result.SellerId);
        Assert.Equal(expectedStore.Name, result.Name);
        _mockRepository.Verify(r => r.GetBySellerIdAsync(TestSellerId), Times.Once);
    }

    [Fact]
    public async Task GetStoreBySellerIdAsync_WhenStoreNotExists_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetStoreBySellerIdAsync(TestSellerId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetBySellerIdAsync(TestSellerId), Times.Once);
    }

    #endregion

    #region GetStoreByIdAsync Tests

    [Fact]
    public async Task GetStoreByIdAsync_WhenStoreExists_ReturnsStore()
    {
        // Arrange
        var expectedStore = CreateTestStore();
        _mockRepository.Setup(r => r.GetByIdAsync(TestStoreId))
            .ReturnsAsync(expectedStore);

        // Act
        var result = await _service.GetStoreByIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStore.Id, result.Id);
        Assert.Equal(expectedStore.Name, result.Name);
        _mockRepository.Verify(r => r.GetByIdAsync(TestStoreId), Times.Once);
    }

    [Fact]
    public async Task GetStoreByIdAsync_WhenStoreNotExists_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetStoreByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(nonExistentId), Times.Once);
    }

    #endregion

    #region CreateOrUpdateStoreProfileAsync Tests

    [Fact]
    public async Task CreateOrUpdateStoreProfileAsync_WhenNewStore_CreatesStore()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockRepository.Setup(r => r.GetBySellerIdAsync(command.SellerId))
            .ReturnsAsync((Store?)null);
        _mockRepository.Setup(r => r.IsStoreNameUniqueAsync(command.Name, null))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Store>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateOrUpdateStoreProfileAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.GetBySellerIdAsync(command.SellerId), Times.Once);
        _mockRepository.Verify(r => r.IsStoreNameUniqueAsync(command.Name, null), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<Store>(s =>
            s.SellerId == command.SellerId &&
            s.Name == command.Name &&
            s.Description == command.Description)), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateStoreProfileAsync_WhenExistingStore_UpdatesStore()
    {
        // Arrange
        var existingStore = CreateTestStore();
        var command = CreateValidCommand();
        command.Name = "Updated Store Name";

        _mockRepository.Setup(r => r.GetBySellerIdAsync(command.SellerId))
            .ReturnsAsync(existingStore);
        _mockRepository.Setup(r => r.IsStoreNameUniqueAsync(command.Name, command.SellerId))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Store>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateOrUpdateStoreProfileAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Store>(s =>
            s.Name == command.Name)), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Store>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateStoreProfileAsync_WhenStoreNameNotUnique_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockRepository.Setup(r => r.GetBySellerIdAsync(command.SellerId))
            .ReturnsAsync((Store?)null);
        _mockRepository.Setup(r => r.IsStoreNameUniqueAsync(command.Name, null))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateOrUpdateStoreProfileAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Store>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateStoreProfileAsync_WhenStoreNameEmpty_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Name = string.Empty;

        // Act
        var result = await _service.CreateOrUpdateStoreProfileAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Store name is required"));
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Store>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Store>()), Times.Never);
    }

    #endregion

    #region UpdateStoreProfileAsync Tests

    [Fact]
    public async Task UpdateStoreProfileAsync_WhenStoreNotExists_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockRepository.Setup(r => r.GetBySellerIdAsync(command.SellerId))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.UpdateStoreProfileAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Store not found"));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Store>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStoreProfileAsync_WhenStoreNameNotUnique_ReturnsFailure()
    {
        // Arrange
        var existingStore = CreateTestStore();
        var command = CreateValidCommand();
        command.Name = "Duplicate Store Name";

        _mockRepository.Setup(r => r.GetBySellerIdAsync(command.SellerId))
            .ReturnsAsync(existingStore);
        _mockRepository.Setup(r => r.IsStoreNameUniqueAsync(command.Name, command.SellerId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateStoreProfileAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Store>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStoreProfileAsync_WhenValid_UpdatesStore()
    {
        // Arrange
        var existingStore = CreateTestStore();
        var command = CreateValidCommand();
        command.Name = "New Store Name";
        command.Description = "New Description";

        _mockRepository.Setup(r => r.GetBySellerIdAsync(command.SellerId))
            .ReturnsAsync(existingStore);
        _mockRepository.Setup(r => r.IsStoreNameUniqueAsync(command.Name, command.SellerId))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Store>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateStoreProfileAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Store>(s =>
            s.Name == command.Name &&
            s.Description == command.Description)), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Store CreateTestStore()
    {
        return new Store
        {
            Id = TestStoreId,
            SellerId = TestSellerId,
            Name = "Test Store",
            Description = "Test store description",
            LogoUrl = "https://example.com/logo.png",
            ContactEmail = "test@example.com",
            ContactPhone = "123-456-7890",
            WebsiteUrl = "https://example.com",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            LastUpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    private static UpdateStoreProfileCommand CreateValidCommand()
    {
        return new UpdateStoreProfileCommand
        {
            SellerId = TestSellerId,
            Name = "Valid Store Name",
            Description = "Valid store description",
            LogoUrl = "https://example.com/logo.png",
            ContactEmail = "contact@example.com",
            ContactPhone = "123-456-7890",
            WebsiteUrl = "https://example.com"
        };
    }

    #endregion
}
