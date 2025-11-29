using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure;
using Mercato.Shipping.Infrastructure.Gateways;
using Moq;

namespace Mercato.Tests.Shipping;

public class ShipmentServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();
    private static readonly Guid TestStoreProviderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();
    private static readonly Guid TestShipmentId = Guid.NewGuid();

    private readonly Mock<IShipmentRepository> _mockShipmentRepository;
    private readonly Mock<IStoreShippingProviderRepository> _mockStoreShippingProviderRepository;
    private readonly Mock<IShipmentStatusUpdateRepository> _mockShipmentStatusUpdateRepository;
    private readonly Mock<IShippingProviderGatewayFactory> _mockGatewayFactory;
    private readonly Mock<IShippingProviderGateway> _mockGateway;
    private readonly ShipmentService _service;

    public ShipmentServiceTests()
    {
        _mockShipmentRepository = new Mock<IShipmentRepository>(MockBehavior.Strict);
        _mockStoreShippingProviderRepository = new Mock<IStoreShippingProviderRepository>(MockBehavior.Strict);
        _mockShipmentStatusUpdateRepository = new Mock<IShipmentStatusUpdateRepository>(MockBehavior.Strict);
        _mockGatewayFactory = new Mock<IShippingProviderGatewayFactory>(MockBehavior.Strict);
        _mockGateway = new Mock<IShippingProviderGateway>(MockBehavior.Strict);

        _service = new ShipmentService(
            _mockShipmentRepository.Object,
            _mockStoreShippingProviderRepository.Object,
            _mockShipmentStatusUpdateRepository.Object,
            _mockGatewayFactory.Object);
    }

    #region CreateShipmentAsync Tests

    [Fact]
    public async Task CreateShipmentAsync_ValidCommand_CreatesShipment()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();
        var command = CreateTestCreateShipmentCommand();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByIdAsync(TestStoreProviderId))
            .ReturnsAsync(storeProvider);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.CreateShipmentAsync(It.IsAny<CreateShipmentGatewayRequest>()))
            .ReturnsAsync(CreateShipmentGatewayResult.Success(
                "DHL123456789",
                "EXT-123",
                DateTimeOffset.UtcNow.AddDays(3),
                "https://labels.example.com/label.pdf"));

        _mockShipmentRepository.Setup(r => r.AddAsync(It.IsAny<Shipment>()))
            .ReturnsAsync((Shipment s) => s);

        _mockShipmentStatusUpdateRepository.Setup(r => r.AddAsync(It.IsAny<ShipmentStatusUpdate>()))
            .ReturnsAsync((ShipmentStatusUpdate u) => u);

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Shipment);
        Assert.Equal("DHL123456789", result.Shipment.TrackingNumber);
        Assert.Equal("EXT-123", result.Shipment.ExternalShipmentId);
        Assert.Equal(ShipmentStatus.Created, result.Shipment.Status);
        _mockShipmentRepository.Verify(r => r.AddAsync(It.IsAny<Shipment>()), Times.Once);
        _mockShipmentStatusUpdateRepository.Verify(r => r.AddAsync(It.IsAny<ShipmentStatusUpdate>()), Times.Once);
    }

    [Fact]
    public async Task CreateShipmentAsync_StoreProviderNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestCreateShipmentCommand();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByIdAsync(TestStoreProviderId))
            .ReturnsAsync((StoreShippingProvider?)null);

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store shipping provider configuration not found.", result.Errors);
    }

    [Fact]
    public async Task CreateShipmentAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();
        storeProvider.StoreId = Guid.NewGuid(); // Different store

        var command = CreateTestCreateShipmentCommand();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByIdAsync(TestStoreProviderId))
            .ReturnsAsync(storeProvider);

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CreateShipmentAsync_ProviderDisabled_ReturnsFailure()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();
        storeProvider.IsEnabled = false;

        var command = CreateTestCreateShipmentCommand();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByIdAsync(TestStoreProviderId))
            .ReturnsAsync(storeProvider);

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipping provider is not enabled for this store.", result.Errors);
    }

    [Fact]
    public async Task CreateShipmentAsync_GatewayNotAvailable_ReturnsFailure()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();
        var command = CreateTestCreateShipmentCommand();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByIdAsync(TestStoreProviderId))
            .ReturnsAsync(storeProvider);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns((IShippingProviderGateway?)null);

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Gateway not available for provider: DHL", result.Errors);
    }

    [Fact]
    public async Task CreateShipmentAsync_GatewayFails_ReturnsFailure()
    {
        // Arrange
        var storeProvider = CreateTestStoreProvider();
        var command = CreateTestCreateShipmentCommand();

        _mockStoreShippingProviderRepository.Setup(r => r.GetByIdAsync(TestStoreProviderId))
            .ReturnsAsync(storeProvider);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.CreateShipmentAsync(It.IsAny<CreateShipmentGatewayRequest>()))
            .ReturnsAsync(CreateShipmentGatewayResult.Failure("Provider API error"));

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Provider API error", result.Errors);
    }

    [Fact]
    public async Task CreateShipmentAsync_InvalidCommand_ReturnsValidationErrors()
    {
        // Arrange
        var command = new CreateShipmentCommand
        {
            SellerSubOrderId = Guid.Empty,
            StoreId = Guid.Empty,
            StoreShippingProviderId = Guid.Empty,
            WeightKg = 0,
            RecipientAddress = new ShippingAddress()
        };

        // Act
        var result = await _service.CreateShipmentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller sub-order ID is required.", result.Errors);
        Assert.Contains("Store ID is required.", result.Errors);
        Assert.Contains("Store shipping provider ID is required.", result.Errors);
        Assert.Contains("Recipient name is required.", result.Errors);
        Assert.Contains("Package weight must be greater than zero.", result.Errors);
    }

    #endregion

    #region GetShipmentAsync Tests

    [Fact]
    public async Task GetShipmentAsync_ValidShipment_ReturnsShipment()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        // Act
        var result = await _service.GetShipmentAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Shipment);
        Assert.Equal(TestShipmentId, result.Shipment.Id);
    }

    [Fact]
    public async Task GetShipmentAsync_ShipmentNotFound_ReturnsFailure()
    {
        // Arrange
        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _service.GetShipmentAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment not found.", result.Errors);
    }

    [Fact]
    public async Task GetShipmentAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.StoreShippingProvider.StoreId = Guid.NewGuid(); // Different store

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        // Act
        var result = await _service.GetShipmentAsync(TestShipmentId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetShipmentAsync_EmptyShipmentId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetShipmentAsync(Guid.Empty, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment ID is required.", result.Errors);
    }

    #endregion

    #region GetShipmentsForSubOrderAsync Tests

    [Fact]
    public async Task GetShipmentsForSubOrderAsync_ValidSubOrder_ReturnsShipments()
    {
        // Arrange
        var shipments = new List<Shipment> { CreateTestShipment() };

        _mockShipmentRepository.Setup(r => r.GetBySellerSubOrderIdAsync(TestSubOrderId))
            .ReturnsAsync(shipments);

        // Act
        var result = await _service.GetShipmentsForSubOrderAsync(TestSubOrderId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Shipments);
    }

    [Fact]
    public async Task GetShipmentsForSubOrderAsync_FiltersByStore_ReturnsOnlyAuthorizedShipments()
    {
        // Arrange
        var authorizedShipment = CreateTestShipment();
        var unauthorizedShipment = CreateTestShipment();
        unauthorizedShipment.StoreShippingProvider.StoreId = Guid.NewGuid();

        var shipments = new List<Shipment> { authorizedShipment, unauthorizedShipment };

        _mockShipmentRepository.Setup(r => r.GetBySellerSubOrderIdAsync(TestSubOrderId))
            .ReturnsAsync(shipments);

        // Act
        var result = await _service.GetShipmentsForSubOrderAsync(TestSubOrderId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Shipments);
        Assert.Equal(TestStoreId, result.Shipments[0].StoreShippingProvider.StoreId);
    }

    [Fact]
    public async Task GetShipmentsForSubOrderAsync_EmptySubOrderId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetShipmentsForSubOrderAsync(Guid.Empty, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller sub-order ID is required.", result.Errors);
    }

    #endregion

    #region UpdateShipmentStatusAsync Tests

    [Fact]
    public async Task UpdateShipmentStatusAsync_ValidCommand_UpdatesStatus()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var command = new UpdateShipmentStatusCommand
        {
            ShipmentId = TestShipmentId,
            Status = ShipmentStatus.InTransit,
            StatusMessage = "In transit to destination",
            Location = "Distribution Center, Chicago",
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShipmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Shipment>()))
            .Returns(Task.CompletedTask);

        _mockShipmentStatusUpdateRepository.Setup(r => r.AddAsync(It.IsAny<ShipmentStatusUpdate>()))
            .ReturnsAsync((ShipmentStatusUpdate u) => u);

        // Act
        var result = await _service.UpdateShipmentStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Shipment);
        Assert.Equal(ShipmentStatus.InTransit, result.Shipment.Status);
        Assert.Equal(ShipmentStatus.Created, result.PreviousStatus);
        _mockShipmentRepository.Verify(r => r.UpdateAsync(It.Is<Shipment>(s => s.Status == ShipmentStatus.InTransit)), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_ByTrackingNumber_FindsShipment()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var command = new UpdateShipmentStatusCommand
        {
            TrackingNumber = "DHL123456",
            Status = ShipmentStatus.InTransit,
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockShipmentRepository.Setup(r => r.GetByTrackingNumberAsync("DHL123456"))
            .ReturnsAsync(shipment);

        _mockShipmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Shipment>()))
            .Returns(Task.CompletedTask);

        _mockShipmentStatusUpdateRepository.Setup(r => r.AddAsync(It.IsAny<ShipmentStatusUpdate>()))
            .ReturnsAsync((ShipmentStatusUpdate u) => u);

        // Act
        var result = await _service.UpdateShipmentStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockShipmentRepository.Verify(r => r.GetByTrackingNumberAsync("DHL123456"), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_DeliveredStatus_SetsDeliveredAt()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var deliveryTime = DateTimeOffset.UtcNow;
        var command = new UpdateShipmentStatusCommand
        {
            ShipmentId = TestShipmentId,
            Status = ShipmentStatus.Delivered,
            StatusMessage = "Delivered to recipient",
            Timestamp = deliveryTime
        };

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShipmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Shipment>()))
            .Returns(Task.CompletedTask);

        _mockShipmentStatusUpdateRepository.Setup(r => r.AddAsync(It.IsAny<ShipmentStatusUpdate>()))
            .ReturnsAsync((ShipmentStatusUpdate u) => u);

        // Act
        var result = await _service.UpdateShipmentStatusAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockShipmentRepository.Verify(r => r.UpdateAsync(It.Is<Shipment>(s =>
            s.Status == ShipmentStatus.Delivered &&
            s.DeliveredAt == deliveryTime)), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_NoIdentifier_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateShipmentStatusCommand
        {
            Status = ShipmentStatus.InTransit,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _service.UpdateShipmentStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Either shipment ID, tracking number, or external shipment ID is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_ShipmentNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateShipmentStatusCommand
        {
            ShipmentId = TestShipmentId,
            Status = ShipmentStatus.InTransit,
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _service.UpdateShipmentStatusAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment not found.", result.Errors);
    }

    #endregion

    #region PollShipmentStatusAsync Tests

    [Fact]
    public async Task PollShipmentStatusAsync_StatusChanged_ReturnsSuccessWithChange()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.GetShipmentStatusAsync(It.IsAny<GetShipmentStatusGatewayRequest>()))
            .ReturnsAsync(GetShipmentStatusGatewayResult.Success(
                ShipmentStatus.InTransit,
                "In transit",
                "Chicago Hub",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(2),
                new List<ShipmentTrackingEvent>()));

        _mockShipmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Shipment>()))
            .Returns(Task.CompletedTask);

        _mockShipmentStatusUpdateRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync(new List<ShipmentStatusUpdate>());

        // Act
        var result = await _service.PollShipmentStatusAsync(TestShipmentId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.StatusChanged);
        Assert.Equal(ShipmentStatus.Created, result.PreviousStatus);
        Assert.Equal(ShipmentStatus.InTransit, result.Shipment?.Status);
    }

    [Fact]
    public async Task PollShipmentStatusAsync_NoStatusChange_ReturnsSuccessNoChange()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.Status = ShipmentStatus.InTransit;

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.GetShipmentStatusAsync(It.IsAny<GetShipmentStatusGatewayRequest>()))
            .ReturnsAsync(GetShipmentStatusGatewayResult.Success(
                ShipmentStatus.InTransit,
                "Still in transit"));

        // Act
        var result = await _service.PollShipmentStatusAsync(TestShipmentId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.StatusChanged);
    }

    [Fact]
    public async Task PollShipmentStatusAsync_GatewayFails_ReturnsFailure()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.GetShipmentStatusAsync(It.IsAny<GetShipmentStatusGatewayRequest>()))
            .ReturnsAsync(GetShipmentStatusGatewayResult.Failure("Provider API error"));

        // Act
        var result = await _service.PollShipmentStatusAsync(TestShipmentId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Provider API error", result.Errors);
    }

    #endregion

    #region CancelShipmentAsync Tests

    [Fact]
    public async Task CancelShipmentAsync_ValidRequest_CancelsShipment()
    {
        // Arrange
        var shipment = CreateTestShipment();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockGatewayFactory.Setup(f => f.GetGateway("DHL"))
            .Returns(_mockGateway.Object);

        _mockGateway.Setup(g => g.CancelShipmentAsync(It.IsAny<CancelShipmentGatewayRequest>()))
            .ReturnsAsync(CancelShipmentGatewayResult.Success());

        _mockShipmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Shipment>()))
            .Returns(Task.CompletedTask);

        _mockShipmentStatusUpdateRepository.Setup(r => r.AddAsync(It.IsAny<ShipmentStatusUpdate>()))
            .ReturnsAsync((ShipmentStatusUpdate u) => u);

        // Act
        var result = await _service.CancelShipmentAsync(TestShipmentId, TestStoreId, "Order cancelled");

        // Assert
        Assert.True(result.Succeeded);
        _mockShipmentRepository.Verify(r => r.UpdateAsync(It.Is<Shipment>(s =>
            s.Status == ShipmentStatus.Returned)), Times.Once);
    }

    [Fact]
    public async Task CancelShipmentAsync_AlreadyDelivered_ReturnsFailure()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.Status = ShipmentStatus.Delivered;

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        // Act
        var result = await _service.CancelShipmentAsync(TestShipmentId, TestStoreId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot cancel a delivered or returned shipment.", result.Errors);
    }

    [Fact]
    public async Task CancelShipmentAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.StoreShippingProvider.StoreId = Guid.NewGuid();

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        // Act
        var result = await _service.CancelShipmentAsync(TestShipmentId, TestStoreId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    #endregion

    #region GetTrackingHistoryAsync Tests

    [Fact]
    public async Task GetTrackingHistoryAsync_ValidShipment_ReturnsHistory()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var statusUpdates = new List<ShipmentStatusUpdate>
        {
            new ShipmentStatusUpdate
            {
                Id = Guid.NewGuid(),
                ShipmentId = TestShipmentId,
                Status = ShipmentStatus.Created,
                StatusMessage = "Created",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new ShipmentStatusUpdate
            {
                Id = Guid.NewGuid(),
                ShipmentId = TestShipmentId,
                Status = ShipmentStatus.InTransit,
                StatusMessage = "In transit",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync(shipment);

        _mockShipmentStatusUpdateRepository.Setup(r => r.GetByShipmentIdAsync(TestShipmentId))
            .ReturnsAsync(statusUpdates);

        // Act
        var result = await _service.GetTrackingHistoryAsync(TestShipmentId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Shipment);
        Assert.Equal(2, result.StatusUpdates.Count);
    }

    [Fact]
    public async Task GetTrackingHistoryAsync_ShipmentNotFound_ReturnsFailure()
    {
        // Arrange
        _mockShipmentRepository.Setup(r => r.GetByIdAsync(TestShipmentId))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _service.GetTrackingHistoryAsync(TestShipmentId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment not found.", result.Errors);
    }

    [Fact]
    public async Task GetTrackingHistoryAsync_EmptyShipmentId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetTrackingHistoryAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Shipment ID is required.", result.Errors);
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

    private CreateShipmentCommand CreateTestCreateShipmentCommand()
    {
        return new CreateShipmentCommand
        {
            SellerSubOrderId = TestSubOrderId,
            StoreId = TestStoreId,
            StoreShippingProviderId = TestStoreProviderId,
            WeightKg = 2.5m,
            ServiceType = "EXPRESS",
            ReferenceNumber = "ORD-12345",
            SenderAddress = new ShippingAddress
            {
                Name = "Seller Store",
                AddressLine1 = "123 Warehouse St",
                City = "Chicago",
                PostalCode = "60601",
                CountryCode = "US"
            },
            RecipientAddress = new ShippingAddress
            {
                Name = "John Doe",
                AddressLine1 = "456 Main St",
                City = "New York",
                PostalCode = "10001",
                CountryCode = "US",
                PhoneNumber = "+1-555-123-4567"
            },
            Dimensions = new PackageDimensions
            {
                LengthCm = 30,
                WidthCm = 20,
                HeightCm = 15
            }
        };
    }

    #endregion
}
