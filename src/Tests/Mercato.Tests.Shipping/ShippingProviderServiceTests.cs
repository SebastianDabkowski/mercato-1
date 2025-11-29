using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure;
using Moq;

namespace Mercato.Tests.Shipping;

public class ShippingProviderServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();

    private readonly Mock<IShippingProviderRepository> _mockShippingProviderRepository;
    private readonly Mock<IStoreShippingProviderRepository> _mockStoreShippingProviderRepository;
    private readonly ShippingProviderService _service;

    public ShippingProviderServiceTests()
    {
        _mockShippingProviderRepository = new Mock<IShippingProviderRepository>(MockBehavior.Strict);
        _mockStoreShippingProviderRepository = new Mock<IStoreShippingProviderRepository>(MockBehavior.Strict);
        _service = new ShippingProviderService(
            _mockShippingProviderRepository.Object,
            _mockStoreShippingProviderRepository.Object);
    }

    #region GetProvidersAsync Tests

    [Fact]
    public async Task GetProvidersAsync_ReturnsActiveProviders()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider("DHL"),
            CreateTestProvider("FEDEX")
        };

        _mockShippingProviderRepository.Setup(r => r.GetActiveProvidersAsync())
            .ReturnsAsync(providers);

        // Act
        var result = await _service.GetProvidersAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Providers.Count);
        _mockShippingProviderRepository.Verify(r => r.GetActiveProvidersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetProvidersAsync_NoActiveProviders_ReturnsEmptyList()
    {
        // Arrange
        _mockShippingProviderRepository.Setup(r => r.GetActiveProvidersAsync())
            .ReturnsAsync(new List<ShippingProvider>());

        // Act
        var result = await _service.GetProvidersAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Providers);
    }

    #endregion

    #region GetProviderByIdAsync Tests

    [Fact]
    public async Task GetProviderByIdAsync_ValidId_ReturnsProvider()
    {
        // Arrange
        var provider = CreateTestProvider("DHL");

        _mockShippingProviderRepository.Setup(r => r.GetByIdAsync(TestProviderId))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.GetProviderByIdAsync(TestProviderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Provider);
        Assert.Equal("DHL", result.Provider.Code);
    }

    [Fact]
    public async Task GetProviderByIdAsync_EmptyId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetProviderByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipping provider ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetProviderByIdAsync_ProviderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockShippingProviderRepository.Setup(r => r.GetByIdAsync(TestProviderId))
            .ReturnsAsync((ShippingProvider?)null);

        // Act
        var result = await _service.GetProviderByIdAsync(TestProviderId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipping provider not found.", result.Errors);
    }

    #endregion

    #region GetProvidersForStoreAsync Tests

    [Fact]
    public async Task GetProvidersForStoreAsync_ValidStoreId_ReturnsStoreProviders()
    {
        // Arrange
        var storeProviders = new List<StoreShippingProvider>
        {
            CreateTestStoreProvider()
        };

        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(storeProviders);

        // Act
        var result = await _service.GetProvidersForStoreAsync(TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.StoreProviders);
    }

    [Fact]
    public async Task GetProvidersForStoreAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetProvidersForStoreAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region EnableProviderForStoreAsync Tests

    [Fact]
    public async Task EnableProviderForStoreAsync_ValidCommand_CreatesStoreProvider()
    {
        // Arrange
        var provider = CreateTestProvider("DHL");
        var command = new EnableProviderForStoreCommand
        {
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId,
            AccountNumber = "12345",
            CredentialIdentifier = "cred-123"
        };

        _mockShippingProviderRepository.Setup(r => r.GetByIdAsync(TestProviderId))
            .ReturnsAsync(provider);

        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreAndProviderAsync(TestStoreId, TestProviderId))
            .ReturnsAsync((StoreShippingProvider?)null);

        _mockStoreShippingProviderRepository.Setup(r => r.AddAsync(It.IsAny<StoreShippingProvider>()))
            .ReturnsAsync((StoreShippingProvider sp) => sp);

        // Act
        var result = await _service.EnableProviderForStoreAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.StoreProvider);
        Assert.True(result.StoreProvider.IsEnabled);
        Assert.Equal("12345", result.StoreProvider.AccountNumber);
        _mockStoreShippingProviderRepository.Verify(r => r.AddAsync(It.Is<StoreShippingProvider>(sp =>
            sp.StoreId == TestStoreId &&
            sp.ShippingProviderId == TestProviderId &&
            sp.IsEnabled)), Times.Once);
    }

    [Fact]
    public async Task EnableProviderForStoreAsync_ExistingDisabled_ReenablesProvider()
    {
        // Arrange
        var provider = CreateTestProvider("DHL");
        var existingStoreProvider = CreateTestStoreProvider();
        existingStoreProvider.IsEnabled = false;

        var command = new EnableProviderForStoreCommand
        {
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId,
            AccountNumber = "67890"
        };

        _mockShippingProviderRepository.Setup(r => r.GetByIdAsync(TestProviderId))
            .ReturnsAsync(provider);

        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreAndProviderAsync(TestStoreId, TestProviderId))
            .ReturnsAsync(existingStoreProvider);

        _mockStoreShippingProviderRepository.Setup(r => r.UpdateAsync(It.IsAny<StoreShippingProvider>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EnableProviderForStoreAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.StoreProvider);
        Assert.True(result.StoreProvider.IsEnabled);
        _mockStoreShippingProviderRepository.Verify(r => r.UpdateAsync(It.Is<StoreShippingProvider>(sp =>
            sp.IsEnabled == true && sp.AccountNumber == "67890")), Times.Once);
    }

    [Fact]
    public async Task EnableProviderForStoreAsync_ProviderNotActive_ReturnsFailure()
    {
        // Arrange
        var provider = CreateTestProvider("DHL");
        provider.IsActive = false;

        var command = new EnableProviderForStoreCommand
        {
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId
        };

        _mockShippingProviderRepository.Setup(r => r.GetByIdAsync(TestProviderId))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.EnableProviderForStoreAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipping provider is not active.", result.Errors);
    }

    [Fact]
    public async Task EnableProviderForStoreAsync_ProviderNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new EnableProviderForStoreCommand
        {
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId
        };

        _mockShippingProviderRepository.Setup(r => r.GetByIdAsync(TestProviderId))
            .ReturnsAsync((ShippingProvider?)null);

        // Act
        var result = await _service.EnableProviderForStoreAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipping provider not found.", result.Errors);
    }

    [Fact]
    public async Task EnableProviderForStoreAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = new EnableProviderForStoreCommand
        {
            StoreId = Guid.Empty,
            ShippingProviderId = TestProviderId
        };

        // Act
        var result = await _service.EnableProviderForStoreAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region DisableProviderForStoreAsync Tests

    [Fact]
    public async Task DisableProviderForStoreAsync_ValidCommand_DisablesProvider()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreAndProviderAsync(TestStoreId, TestProviderId))
            .ReturnsAsync(storeProvider);

        _mockStoreShippingProviderRepository.Setup(r => r.UpdateAsync(It.IsAny<StoreShippingProvider>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DisableProviderForStoreAsync(TestStoreId, TestProviderId);

        // Assert
        Assert.True(result.Succeeded);
        _mockStoreShippingProviderRepository.Verify(r => r.UpdateAsync(It.Is<StoreShippingProvider>(sp =>
            sp.IsEnabled == false)), Times.Once);
    }

    [Fact]
    public async Task DisableProviderForStoreAsync_ConfigNotFound_ReturnsFailure()
    {
        // Arrange
        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreAndProviderAsync(TestStoreId, TestProviderId))
            .ReturnsAsync((StoreShippingProvider?)null);

        // Act
        var result = await _service.DisableProviderForStoreAsync(TestStoreId, TestProviderId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store shipping provider configuration not found.", result.Errors);
    }

    [Fact]
    public async Task DisableProviderForStoreAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.DisableProviderForStoreAsync(Guid.Empty, TestProviderId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region UpdateStoreProviderConfigAsync Tests

    [Fact]
    public async Task UpdateStoreProviderConfigAsync_ValidCommand_UpdatesConfig()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();
        var command = new UpdateStoreProviderConfigCommand
        {
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId,
            AccountNumber = "NEW-ACCOUNT",
            CredentialIdentifier = "NEW-CRED",
            IsEnabled = false
        };

        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreAndProviderAsync(TestStoreId, TestProviderId))
            .ReturnsAsync(storeProvider);

        _mockStoreShippingProviderRepository.Setup(r => r.UpdateAsync(It.IsAny<StoreShippingProvider>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateStoreProviderConfigAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.StoreProvider);
        _mockStoreShippingProviderRepository.Verify(r => r.UpdateAsync(It.Is<StoreShippingProvider>(sp =>
            sp.AccountNumber == "NEW-ACCOUNT" &&
            sp.CredentialIdentifier == "NEW-CRED" &&
            sp.IsEnabled == false)), Times.Once);
    }

    [Fact]
    public async Task UpdateStoreProviderConfigAsync_ConfigNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateStoreProviderConfigCommand
        {
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId,
            AccountNumber = "TEST"
        };

        _mockStoreShippingProviderRepository.Setup(r => r.GetByStoreAndProviderAsync(TestStoreId, TestProviderId))
            .ReturnsAsync((StoreShippingProvider?)null);

        // Act
        var result = await _service.UpdateStoreProviderConfigAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store shipping provider configuration not found.", result.Errors);
    }

    #endregion

    #region Helper Methods

    private static ShippingProvider CreateTestProvider(string code)
    {
        return new ShippingProvider
        {
            Id = TestProviderId,
            Name = code,
            Code = code,
            IsActive = true,
            Status = ShippingProviderStatus.Active,
            ApiEndpointUrl = "https://api.example.com",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private StoreShippingProvider CreateTestStoreProvider()
    {
        return new StoreShippingProvider
        {
            Id = Guid.NewGuid(),
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId,
            IsEnabled = true,
            AccountNumber = "12345",
            CredentialIdentifier = "cred-123",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            ShippingProvider = CreateTestProvider("DHL")
        };
    }

    #endregion
}
