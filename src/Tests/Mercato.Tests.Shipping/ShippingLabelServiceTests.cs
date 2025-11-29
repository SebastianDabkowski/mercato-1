using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure;
using Mercato.Shipping.Infrastructure.Gateways;
using Moq;

namespace Mercato.Tests.Shipping;

public class ShippingLabelServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();
    private static readonly Guid TestStoreProviderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();
    private static readonly Guid TestShipmentId = Guid.NewGuid();
    private static readonly Guid TestLabelId = Guid.NewGuid();

    private readonly Mock<IShipmentRepository> _mockShipmentRepository;
    private readonly Mock<IShippingLabelRepository> _mockShippingLabelRepository;
    private readonly Mock<IShippingProviderGatewayFactory> _mockGatewayFactory;
    private readonly Mock<IShippingProviderGateway> _mockGateway;
    private readonly ShippingLabelService _service;

    public ShippingLabelServiceTests()
    {
        _mockShipmentRepository = new Mock<IShipmentRepository>(MockBehavior.Strict);
        _mockShippingLabelRepository = new Mock<IShippingLabelRepository>(MockBehavior.Strict);
        _mockGatewayFactory = new Mock<IShippingProviderGatewayFactory>(MockBehavior.Strict);
        _mockGateway = new Mock<IShippingProviderGateway>(MockBehavior.Strict);

        _service = new ShippingLabelService(
            _mockShipmentRepository.Object,
            _mockShippingLabelRepository.Object,
            _mockGatewayFactory.Object);
    }

    #region GenerateLabelAsync Tests

    [Fact]
    public async Task GenerateLabelAsync_ValidShipment_GeneratesLabel()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var labelData = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 Mock PDF Content");

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShippingLabelRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync((ShippingLabel?)null);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.GetLabelAsync(It.IsAny<GetShippingLabelGatewayRequest>()))
            .ReturnsAsync(GetShippingLabelGatewayResult.Success(
                labelData,
                "application/pdf",
                "DHL-Label-DHL123456.pdf"));

        _mockShippingLabelRepository.Setup(r => r.AddAsync(It.IsAny<ShippingLabel>()))
            .ReturnsAsync((ShippingLabel l) => l);

        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Label);
        Assert.Equal(TestShipmentId, result.Label.ShipmentId);
        Assert.Equal("application/pdf", result.Label.ContentType);
        Assert.True(result.Label.LabelData.Length > 0);
        _mockShippingLabelRepository.Verify(r => r.AddAsync(It.IsAny<ShippingLabel>()), Times.Once);
    }

    [Fact]
    public async Task GenerateLabelAsync_ExistingLabel_ReturnsExistingLabel()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var existingLabel = CreateTestShippingLabel();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShippingLabelRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync(existingLabel);

        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Label);
        Assert.Equal(TestLabelId, result.Label.Id);
        _mockGatewayFactory.Verify(f => f.GetGateway(It.IsAny<string>()), Times.Never);
        _mockShippingLabelRepository.Verify(r => r.AddAsync(It.IsAny<ShippingLabel>()), Times.Never);
    }

    [Fact]
    public async Task GenerateLabelAsync_ShipmentNotFound_ReturnsFailure()
    {
        // Arrange
        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment not found.", result.Errors);
    }

    [Fact]
    public async Task GenerateLabelAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.StoreShippingProvider.StoreId = Guid.NewGuid(); // Different store

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GenerateLabelAsync_GatewayNotAvailable_ReturnsFailure()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShippingLabelRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync((ShippingLabel?)null);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns((IShippingProviderGateway?)null);

        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Gateway not available for provider: DHL", result.Errors);
    }

    [Fact]
    public async Task GenerateLabelAsync_GatewayFails_ReturnsFailure()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShippingLabelRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync((ShippingLabel?)null);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.GetLabelAsync(It.IsAny<GetShippingLabelGatewayRequest>()))
            .ReturnsAsync(GetShippingLabelGatewayResult.Failure("Label service unavailable"));

        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Label service unavailable", result.Errors);
    }

    [Fact]
    public async Task GenerateLabelAsync_EmptyShipmentId_ReturnsValidationError()
    {
        // Act
        var result = await _service.GenerateLabelAsync(Guid.Empty, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment ID is required.", result.Errors);
    }

    [Fact]
    public async Task GenerateLabelAsync_EmptyStoreId_ReturnsValidationError()
    {
        // Act
        var result = await _service.GenerateLabelAsync(TestShipmentId, Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region GetLabelAsync Tests

    [Fact]
    public async Task GetLabelAsync_ValidShipmentWithLabel_ReturnsLabel()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var label = CreateTestShippingLabel();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShippingLabelRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync(label);

        // Act
        var result = await _service.GetLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Label);
        Assert.Equal(TestLabelId, result.Label.Id);
    }

    [Fact]
    public async Task GetLabelAsync_ShipmentNotFound_ReturnsFailure()
    {
        // Arrange
        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _service.GetLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment not found.", result.Errors);
    }

    [Fact]
    public async Task GetLabelAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.StoreShippingProvider.StoreId = Guid.NewGuid(); // Different store

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        // Act
        var result = await _service.GetLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetLabelAsync_LabelNotFound_ReturnsFailure()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShippingLabelRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync((ShippingLabel?)null);

        // Act
        var result = await _service.GetLabelAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipping label not found for this shipment.", result.Errors);
    }

    [Fact]
    public async Task GetLabelAsync_EmptyShipmentId_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetLabelAsync(Guid.Empty, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetLabelAsync_EmptyStoreId_ReturnsValidationError()
    {
        // Act
        var result = await _service.GetLabelAsync(TestShipmentId, Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region Helper Methods

    private StoreShippingProvider CreateTestStoreProvider()
    {
        return new StoreShippingProvider
        {
            Id = TestStoreProviderId,
            StoreId = TestStoreId,
            ShippingProviderId = TestProviderId,
            IsEnabled = true,
            AccountNumber = "12345",
            CredentialIdentifier = "cred-123",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            ShippingProvider = new ShippingProvider
            {
                Id = TestProviderId,
                Name = "DHL Express",
                Code = "DHL",
                IsActive = true,
                Status = ShippingProviderStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            }
        };
    }

    private Shipment CreateTestShipment()
    {
        return new Shipment
        {
            Id = TestShipmentId,
            SellerSubOrderId = TestSubOrderId,
            StoreShippingProviderId = TestStoreProviderId,
            TrackingNumber = "DHL123456",
            ExternalShipmentId = "EXT-123",
            Status = ShipmentStatus.Created,
            StatusMessage = "Created",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3),
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            StoreShippingProvider = CreateTestStoreProvider()
        };
    }

    private ShippingLabel CreateTestShippingLabel()
    {
        return new ShippingLabel
        {
            Id = TestLabelId,
            ShipmentId = TestShipmentId,
            LabelData = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 Mock PDF Content"),
            ContentType = "application/pdf",
            FileName = "DHL-Label-DHL123456.pdf",
            LabelFormat = "PDF",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            Shipment = CreateTestShipment()
        };
    }

    #endregion
}
