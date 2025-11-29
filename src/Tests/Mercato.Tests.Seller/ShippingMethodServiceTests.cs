using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

/// <summary>
/// Unit tests for the ShippingMethodService.
/// </summary>
public class ShippingMethodServiceTests
{
    private static readonly Guid TestStoreId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherStoreId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestShippingMethodId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingMethod_ReturnsMethod()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetByIdAsync(TestShippingMethodId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingMethod.Id, result.Id);
        Assert.Equal(existingMethod.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingMethod_ReturnsNull()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync((ShippingMethod?)null);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetByIdAsync(TestShippingMethodId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByStoreIdAsync Tests

    [Fact]
    public async Task GetByStoreIdAsync_WithExistingMethods_ReturnsMethods()
    {
        // Arrange
        var methods = new List<ShippingMethod>
        {
            CreateShippingMethod(name: "Courier"),
            CreateShippingMethod(name: "Parcel Locker")
        };
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(methods);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetByStoreIdAsync(TestStoreId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByStoreIdAsync_WithNoMethods_ReturnsEmptyList()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(new List<ShippingMethod>());

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetByStoreIdAsync(TestStoreId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetActiveByStoreIdAsync Tests

    [Fact]
    public async Task GetActiveByStoreIdAsync_ReturnsOnlyActiveMethods()
    {
        // Arrange
        var activeMethods = new List<ShippingMethod>
        {
            CreateShippingMethod(name: "Active Courier", isActive: true)
        };
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetActiveByStoreIdAsync(TestStoreId))
            .ReturnsAsync(activeMethods);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetActiveByStoreIdAsync(TestStoreId);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidCommand_CreatesMethodAndReturnsSuccess()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingMethod>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "Standard Courier",
            Description = "Delivery in 3-5 business days",
            AvailableCountries = "US, CA, GB"
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ShippingMethodId);
        Assert.NotEqual(Guid.Empty, result.ShippingMethodId.Value);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.AddAsync(It.Is<ShippingMethod>(m =>
            m.StoreId == TestStoreId &&
            m.Name == "Standard Courier" &&
            m.Description == "Delivery in 3-5 business days" &&
            m.AvailableCountries == "CA,GB,US" && // Normalized and sorted
            m.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithBaseCostAndDeliveryTime_SavesValues()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingMethod>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "Express Courier",
            BaseCost = 15.99m,
            EstimatedDeliveryMinDays = 2,
            EstimatedDeliveryMaxDays = 3
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ShippingMethodId);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.AddAsync(It.Is<ShippingMethod>(m =>
            m.StoreId == TestStoreId &&
            m.Name == "Express Courier" &&
            m.BaseCost == 15.99m &&
            m.EstimatedDeliveryMinDays == 2 &&
            m.EstimatedDeliveryMaxDays == 3)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDeliveryMinGreaterThanMax_ReturnsValidationError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "Express Courier",
            BaseCost = 15.99m,
            EstimatedDeliveryMinDays = 5,
            EstimatedDeliveryMaxDays = 2
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ShippingMethodId);
        Assert.Contains(result.Errors, e => e.Contains("Minimum delivery days cannot be greater than maximum delivery days"));
    }

    [Fact]
    public async Task CreateAsync_WithMissingName_ReturnsValidationError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "",
            Description = "Some description"
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ShippingMethodId);
        Assert.Contains(result.Errors, e => e.Contains("name is required"));
    }

    [Fact]
    public async Task CreateAsync_WithNameTooShort_ReturnsValidationError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "A",
            Description = "Some description"
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("between 2 and 100 characters"));
    }

    [Fact]
    public async Task CreateAsync_WithNameTooLong_ReturnsValidationError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = new string('A', 101),
            Description = "Some description"
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("between 2 and 100 characters"));
    }

    [Fact]
    public async Task CreateAsync_WithDescriptionTooLong_ReturnsValidationError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "Valid Name",
            Description = new string('A', 501)
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Description must be at most 500 characters"));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyStoreId_ReturnsValidationError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = Guid.Empty,
            Name = "Valid Name"
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Store ID is required"));
    }

    [Fact]
    public async Task CreateAsync_WithNullAvailableCountries_SetsNullInResult()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingMethod>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "Standard Courier",
            AvailableCountries = null
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.AddAsync(It.Is<ShippingMethod>(m =>
            m.AvailableCountries == null)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithWhitespaceAvailableCountries_SetsNullInResult()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingMethod>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new CreateShippingMethodCommand
        {
            StoreId = TestStoreId,
            Name = "Standard Courier",
            AvailableCountries = "   "
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.AddAsync(It.Is<ShippingMethod>(m =>
            m.AvailableCountries == null)), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidCommand_UpdatesMethodAndReturnsSuccess()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ShippingMethod>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId,
            Name = "Updated Courier",
            Description = "Updated description",
            AvailableCountries = "DE, FR",
            IsActive = false
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.False(result.IsNotAuthorized);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<ShippingMethod>(m =>
            m.Name == "Updated Courier" &&
            m.Description == "Updated description" &&
            m.AvailableCountries == "DE,FR" &&
            m.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingMethod_ReturnsNotFoundError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync((ShippingMethod?)null);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId,
            Name = "Updated Courier"
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateAsync_WithDifferentStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var existingMethod = CreateShippingMethod(); // Belongs to TestStoreId
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = OtherStoreId, // Different store
            Name = "Updated Courier"
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains(result.Errors, e => e.Contains("not authorized"));
    }

    [Fact]
    public async Task UpdateAsync_WithMissingName_ReturnsValidationError()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId,
            Name = ""
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("name is required"));
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyShippingMethodId_ReturnsValidationError()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(Guid.Empty))
            .ReturnsAsync((ShippingMethod?)null);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = Guid.Empty,
            StoreId = TestStoreId,
            Name = "Valid Name"
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        // Will fail with "not found" since Guid.Empty won't find anything
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateAsync_WithBaseCostAndDeliveryTime_UpdatesValues()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ShippingMethod>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId,
            Name = "Updated Courier",
            BaseCost = 25.50m,
            EstimatedDeliveryMinDays = 1,
            EstimatedDeliveryMaxDays = 2
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<ShippingMethod>(m =>
            m.Name == "Updated Courier" &&
            m.BaseCost == 25.50m &&
            m.EstimatedDeliveryMinDays == 1 &&
            m.EstimatedDeliveryMaxDays == 2)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithDeliveryMinGreaterThanMax_ReturnsValidationError()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);

        var service = CreateService(mockRepository.Object);
        var command = new UpdateShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId,
            Name = "Updated Courier",
            BaseCost = 25.50m,
            EstimatedDeliveryMinDays = 10,
            EstimatedDeliveryMaxDays = 5
        };

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Minimum delivery days cannot be greater than maximum delivery days"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingMethod_DeletesAndReturnsSuccess()
    {
        // Arrange
        var existingMethod = CreateShippingMethod();
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);
        mockRepository.Setup(r => r.DeleteAsync(TestShippingMethodId))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);
        var command = new DeleteShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId
        };

        // Act
        var result = await service.DeleteAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.False(result.IsNotAuthorized);
        mockRepository.Verify(r => r.DeleteAsync(TestShippingMethodId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingMethod_ReturnsNotFoundError()
    {
        // Arrange
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync((ShippingMethod?)null);

        var service = CreateService(mockRepository.Object);
        var command = new DeleteShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = TestStoreId
        };

        // Act
        var result = await service.DeleteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeleteAsync_WithDifferentStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var existingMethod = CreateShippingMethod(); // Belongs to TestStoreId
        var mockRepository = new Mock<IShippingMethodRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(TestShippingMethodId))
            .ReturnsAsync(existingMethod);

        var service = CreateService(mockRepository.Object);
        var command = new DeleteShippingMethodCommand
        {
            Id = TestShippingMethodId,
            StoreId = OtherStoreId // Different store
        };

        // Act
        var result = await service.DeleteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains(result.Errors, e => e.Contains("not authorized"));
    }

    #endregion

    #region Helper Methods

    private static ShippingMethodService CreateService(IShippingMethodRepository repository)
    {
        var mockLogger = new Mock<ILogger<ShippingMethodService>>();
        return new ShippingMethodService(repository, mockLogger.Object);
    }

    private static ShippingMethod CreateShippingMethod(
        Guid? id = null,
        Guid? storeId = null,
        string name = "Standard Courier",
        string? description = "Delivery in 3-5 business days",
        string? availableCountries = "US,CA,GB",
        bool isActive = true)
    {
        return new ShippingMethod
        {
            Id = id ?? TestShippingMethodId,
            StoreId = storeId ?? TestStoreId,
            Name = name,
            Description = description,
            AvailableCountries = availableCountries,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
