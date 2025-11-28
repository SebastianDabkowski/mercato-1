using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Domain.Interfaces;
using Mercato.Buyer.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Buyer;

public class DeliveryAddressServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestAddressId = Guid.NewGuid();

    private readonly Mock<IDeliveryAddressRepository> _mockRepository;
    private readonly Mock<ILogger<DeliveryAddressService>> _mockLogger;
    private readonly DeliveryAddressService _service;

    public DeliveryAddressServiceTests()
    {
        _mockRepository = new Mock<IDeliveryAddressRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<DeliveryAddressService>>();
        _service = new DeliveryAddressService(_mockRepository.Object, _mockLogger.Object);
    }

    #region SaveAddressAsync Tests - Validation

    [Fact]
    public async Task SaveAddressAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.BuyerId = string.Empty;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_EmptyFullName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.FullName = string.Empty;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Full name is required.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_FullNameTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.FullName = new string('A', 201);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Full name must not exceed 200 characters.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_EmptyAddressLine1_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.AddressLine1 = string.Empty;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Address line 1 is required.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_EmptyCity_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.City = string.Empty;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("City is required.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_EmptyPostalCode_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.PostalCode = string.Empty;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Postal code is required.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_EmptyCountry_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.Country = string.Empty;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Country is required.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_InvalidCountryCode_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.Country = "USA"; // Invalid - should be 2 chars

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Country must be a valid 2-letter ISO country code.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new SaveDeliveryAddressCommand
        {
            BuyerId = string.Empty,
            FullName = string.Empty,
            AddressLine1 = string.Empty,
            City = string.Empty,
            PostalCode = string.Empty,
            Country = string.Empty
        };

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count >= 6);
    }

    #endregion

    #region SaveAddressAsync Tests - Region Validation

    [Theory]
    [InlineData("US")]
    [InlineData("CA")]
    [InlineData("GB")]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("IT")]
    [InlineData("ES")]
    [InlineData("NL")]
    [InlineData("PL")]
    public async Task SaveAddressAsync_AllowedCountry_DoesNotReturnRegionError(string countryCode)
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.Country = countryCode;

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<DeliveryAddress>());
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryAddress>()))
            .ReturnsAsync((DeliveryAddress a) => a);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsRegionNotAllowed);
    }

    [Theory]
    [InlineData("JP")]
    [InlineData("CN")]
    [InlineData("AU")]
    [InlineData("BR")]
    [InlineData("IN")]
    public async Task SaveAddressAsync_DisallowedCountry_ReturnsRegionNotAllowed(string countryCode)
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.Country = countryCode;

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsRegionNotAllowed);
        Assert.Contains(countryCode, result.Errors[0]);
    }

    #endregion

    #region SaveAddressAsync Tests - Success

    [Fact]
    public async Task SaveAddressAsync_NewAddress_FirstAddress_SetsAsDefault()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.SetAsDefault = false; // Even without setting, first address should be default

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<DeliveryAddress>());

        DeliveryAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryAddress>()))
            .Callback<DeliveryAddress>(a => capturedAddress = a)
            .ReturnsAsync((DeliveryAddress a) => a);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedAddress);
        Assert.True(capturedAddress.IsDefault);
    }

    [Fact]
    public async Task SaveAddressAsync_NewAddress_NotFirstAddress_DoesNotSetAsDefaultUnlessRequested()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.SetAsDefault = false;

        var existingAddresses = new List<DeliveryAddress>
        {
            CreateTestAddress(isDefault: true)
        };

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(existingAddresses);

        DeliveryAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryAddress>()))
            .Callback<DeliveryAddress>(a => capturedAddress = a)
            .ReturnsAsync((DeliveryAddress a) => a);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedAddress);
        Assert.False(capturedAddress.IsDefault);
    }

    [Fact]
    public async Task SaveAddressAsync_NewAddress_SetAsDefault_ClearsExistingDefault()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.SetAsDefault = true;

        var existingDefault = CreateTestAddress(isDefault: true);
        var existingAddresses = new List<DeliveryAddress> { existingDefault };

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(existingAddresses);

        _mockRepository.Setup(r => r.GetDefaultByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(existingDefault);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DeliveryAddress>()))
            .Returns(Task.CompletedTask);

        DeliveryAddress? capturedNewAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryAddress>()))
            .Callback<DeliveryAddress>(a => capturedNewAddress = a)
            .ReturnsAsync((DeliveryAddress a) => a);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(existingDefault.IsDefault);
        Assert.NotNull(capturedNewAddress);
        Assert.True(capturedNewAddress.IsDefault);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<DeliveryAddress>(a => a.Id == existingDefault.Id && !a.IsDefault)), Times.Once);
    }

    [Fact]
    public async Task SaveAddressAsync_NewAddress_ConvertsCountryToUppercase()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.Country = "us"; // lowercase

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<DeliveryAddress>());

        DeliveryAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryAddress>()))
            .Callback<DeliveryAddress>(a => capturedAddress = a)
            .ReturnsAsync((DeliveryAddress a) => a);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedAddress);
        Assert.Equal("US", capturedAddress.Country);
    }

    [Fact]
    public async Task SaveAddressAsync_UpdateAddress_NotAuthorized_ReturnsNotAuthorized()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.AddressId = TestAddressId;

        var existingAddress = CreateTestAddress(isDefault: false);
        existingAddress.BuyerId = "other-buyer-id"; // Different buyer

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(existingAddress);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task SaveAddressAsync_UpdateAddress_NotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.AddressId = TestAddressId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync((DeliveryAddress?)null);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Address not found.", result.Errors);
    }

    [Fact]
    public async Task SaveAddressAsync_UpdateAddress_Success()
    {
        // Arrange
        var command = CreateValidSaveCommand();
        command.AddressId = TestAddressId;
        command.FullName = "Updated Name";
        command.City = "Updated City";

        var existingAddress = CreateTestAddress(isDefault: false);
        existingAddress.Id = TestAddressId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(existingAddress);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DeliveryAddress>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SaveAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(TestAddressId, result.AddressId);
        Assert.Equal("Updated Name", existingAddress.FullName);
        Assert.Equal("Updated City", existingAddress.City);
    }

    #endregion

    #region GetAddressesAsync Tests

    [Fact]
    public async Task GetAddressesAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var query = new GetDeliveryAddressesQuery { BuyerId = string.Empty };

        // Act
        var result = await _service.GetAddressesAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetAddressesAsync_ValidBuyerId_ReturnsAddresses()
    {
        // Arrange
        var query = new GetDeliveryAddressesQuery { BuyerId = TestBuyerId };
        var addresses = new List<DeliveryAddress>
        {
            CreateTestAddress(isDefault: true, label: "Home"),
            CreateTestAddress(isDefault: false, label: "Work")
        };

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(addresses);

        // Act
        var result = await _service.GetAddressesAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Addresses.Count);
        Assert.Equal("Home", result.Addresses[0].Label);
        Assert.Equal("Work", result.Addresses[1].Label);
    }

    [Fact]
    public async Task GetAddressesAsync_NoAddresses_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetDeliveryAddressesQuery { BuyerId = TestBuyerId };

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<DeliveryAddress>());

        // Act
        var result = await _service.GetAddressesAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Addresses);
    }

    [Fact]
    public async Task GetAddressesAsync_FormatsAddressCorrectly()
    {
        // Arrange
        var query = new GetDeliveryAddressesQuery { BuyerId = TestBuyerId };
        var address = new DeliveryAddress
        {
            Id = TestAddressId,
            BuyerId = TestBuyerId,
            FullName = "John Doe",
            AddressLine1 = "123 Main St",
            AddressLine2 = "Apt 4",
            City = "New York",
            State = "NY",
            PostalCode = "10001",
            Country = "US",
            IsDefault = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _mockRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<DeliveryAddress> { address });

        // Act
        var result = await _service.GetAddressesAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Addresses);
        var dto = result.Addresses[0];
        Assert.Contains("123 Main St", dto.FormattedAddress);
        Assert.Contains("Apt 4", dto.FormattedAddress);
        Assert.Contains("New York", dto.FormattedAddress);
        Assert.Contains("NY", dto.FormattedAddress);
        Assert.Contains("10001", dto.FormattedAddress);
        Assert.Contains("US", dto.FormattedAddress);
    }

    #endregion

    #region DeleteAddressAsync Tests

    [Fact]
    public async Task DeleteAddressAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = string.Empty
        };

        // Act
        var result = await _service.DeleteAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task DeleteAddressAsync_EmptyAddressId_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteDeliveryAddressCommand
        {
            AddressId = Guid.Empty,
            BuyerId = TestBuyerId
        };

        // Act
        var result = await _service.DeleteAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Address ID is required.", result.Errors);
    }

    [Fact]
    public async Task DeleteAddressAsync_AddressNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync((DeliveryAddress?)null);

        // Act
        var result = await _service.DeleteAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Address not found.", result.Errors);
    }

    [Fact]
    public async Task DeleteAddressAsync_NotAuthorized_ReturnsNotAuthorized()
    {
        // Arrange
        var command = new DeleteDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        var address = CreateTestAddress(isDefault: false);
        address.BuyerId = "other-buyer-id";

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(address);

        // Act
        var result = await _service.DeleteAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task DeleteAddressAsync_Success()
    {
        // Arrange
        var command = new DeleteDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        var address = CreateTestAddress(isDefault: false);
        address.Id = TestAddressId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(address);

        _mockRepository.Setup(r => r.DeleteAsync(It.Is<DeliveryAddress>(a => a.Id == TestAddressId)))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<DeliveryAddress>(a => a.Id == TestAddressId)), Times.Once);
    }

    #endregion

    #region SetDefaultAddressAsync Tests

    [Fact]
    public async Task SetDefaultAddressAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = string.Empty
        };

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_EmptyAddressId_ReturnsFailure()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = Guid.Empty,
            BuyerId = TestBuyerId
        };

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Address ID is required.", result.Errors);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_AddressNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync((DeliveryAddress?)null);

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Address not found.", result.Errors);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_NotAuthorized_ReturnsNotAuthorized()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        var address = CreateTestAddress(isDefault: false);
        address.BuyerId = "other-buyer-id";

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(address);

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_AlreadyDefault_ReturnsSuccess()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        var address = CreateTestAddress(isDefault: true);
        address.Id = TestAddressId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(address);

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliveryAddress>()), Times.Never);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_ClearsExistingDefault_SetsNewDefault()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        var currentDefault = CreateTestAddress(isDefault: true);
        var newDefault = CreateTestAddress(isDefault: false);
        newDefault.Id = TestAddressId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(newDefault);

        _mockRepository.Setup(r => r.GetDefaultByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(currentDefault);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DeliveryAddress>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(currentDefault.IsDefault);
        Assert.True(newDefault.IsDefault);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<DeliveryAddress>(a => a.Id == currentDefault.Id && !a.IsDefault)), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<DeliveryAddress>(a => a.Id == TestAddressId && a.IsDefault)), Times.Once);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_NoExistingDefault_SetsNewDefault()
    {
        // Arrange
        var command = new SetDefaultDeliveryAddressCommand
        {
            AddressId = TestAddressId,
            BuyerId = TestBuyerId
        };

        var newDefault = CreateTestAddress(isDefault: false);
        newDefault.Id = TestAddressId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestAddressId))
            .ReturnsAsync(newDefault);

        _mockRepository.Setup(r => r.GetDefaultByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync((DeliveryAddress?)null);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DeliveryAddress>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetDefaultAddressAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(newDefault.IsDefault);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<DeliveryAddress>(a => a.Id == TestAddressId && a.IsDefault)), Times.Once);
    }

    #endregion

    #region IsShippingAllowedToRegion Tests

    [Theory]
    [InlineData("US", true)]
    [InlineData("us", true)]
    [InlineData("Us", true)]
    [InlineData("CA", true)]
    [InlineData("GB", true)]
    [InlineData("DE", true)]
    [InlineData("FR", true)]
    [InlineData("IT", true)]
    [InlineData("ES", true)]
    [InlineData("NL", true)]
    [InlineData("PL", true)]
    [InlineData("JP", false)]
    [InlineData("CN", false)]
    [InlineData("AU", false)]
    [InlineData("BR", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsShippingAllowedToRegion_ReturnsExpectedResult(string countryCode, bool expected)
    {
        // Act
        var result = _service.IsShippingAllowedToRegion(countryCode);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Helper Methods

    private static SaveDeliveryAddressCommand CreateValidSaveCommand()
    {
        return new SaveDeliveryAddressCommand
        {
            BuyerId = TestBuyerId,
            FullName = "John Doe",
            AddressLine1 = "123 Main Street",
            City = "New York",
            PostalCode = "10001",
            Country = "US",
            SetAsDefault = false
        };
    }

    private static DeliveryAddress CreateTestAddress(bool isDefault, string? label = null)
    {
        return new DeliveryAddress
        {
            Id = Guid.NewGuid(),
            BuyerId = TestBuyerId,
            Label = label,
            FullName = "Test User",
            AddressLine1 = "Test Street 123",
            City = "Test City",
            PostalCode = "12345",
            Country = "US",
            IsDefault = isDefault,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
