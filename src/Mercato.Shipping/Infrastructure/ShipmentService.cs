using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Gateways;

namespace Mercato.Shipping.Infrastructure;

/// <summary>
/// Service implementation for managing shipments.
/// </summary>
public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IStoreShippingProviderRepository _storeShippingProviderRepository;
    private readonly IShipmentStatusUpdateRepository _shipmentStatusUpdateRepository;
    private readonly IShippingProviderGatewayFactory _gatewayFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShipmentService"/> class.
    /// </summary>
    /// <param name="shipmentRepository">The shipment repository.</param>
    /// <param name="storeShippingProviderRepository">The store shipping provider repository.</param>
    /// <param name="shipmentStatusUpdateRepository">The shipment status update repository.</param>
    /// <param name="gatewayFactory">The shipping provider gateway factory.</param>
    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IStoreShippingProviderRepository storeShippingProviderRepository,
        IShipmentStatusUpdateRepository shipmentStatusUpdateRepository,
        IShippingProviderGatewayFactory gatewayFactory)
    {
        _shipmentRepository = shipmentRepository;
        _storeShippingProviderRepository = storeShippingProviderRepository;
        _shipmentStatusUpdateRepository = shipmentStatusUpdateRepository;
        _gatewayFactory = gatewayFactory;
    }

    /// <inheritdoc />
    public async Task<CreateShipmentResult> CreateShipmentAsync(CreateShipmentCommand command)
    {
        var errors = ValidateCreateShipmentCommand(command);
        if (errors.Count > 0)
        {
            return CreateShipmentResult.Failure(errors);
        }

        // Get store shipping provider configuration
        var storeProvider = await _storeShippingProviderRepository.GetByIdAsync(command.StoreShippingProviderId);
        if (storeProvider == null)
        {
            return CreateShipmentResult.Failure("Store shipping provider configuration not found.");
        }

        // Validate store authorization
        if (storeProvider.StoreId != command.StoreId)
        {
            return CreateShipmentResult.NotAuthorized();
        }

        if (!storeProvider.IsEnabled)
        {
            return CreateShipmentResult.Failure("Shipping provider is not enabled for this store.");
        }

        // Get the gateway
        var gateway = _gatewayFactory.GetGateway(storeProvider.ShippingProvider.Code);
        if (gateway == null)
        {
            return CreateShipmentResult.Failure($"Gateway not available for provider: {storeProvider.ShippingProvider.Code}");
        }

        // Create shipment via gateway
        var gatewayRequest = new CreateShipmentGatewayRequest
        {
            AccountNumber = storeProvider.AccountNumber,
            CredentialIdentifier = storeProvider.CredentialIdentifier,
            SenderAddress = command.SenderAddress,
            RecipientAddress = command.RecipientAddress,
            WeightKg = command.WeightKg,
            Dimensions = command.Dimensions,
            ReferenceNumber = command.ReferenceNumber,
            ServiceType = command.ServiceType
        };

        var gatewayResult = await gateway.CreateShipmentAsync(gatewayRequest);
        if (!gatewayResult.Succeeded)
        {
            return CreateShipmentResult.Failure(gatewayResult.Errors);
        }

        // Create shipment entity
        var now = DateTimeOffset.UtcNow;
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            SellerSubOrderId = command.SellerSubOrderId,
            StoreShippingProviderId = command.StoreShippingProviderId,
            TrackingNumber = gatewayResult.TrackingNumber!,
            ExternalShipmentId = gatewayResult.ExternalShipmentId,
            Status = ShipmentStatus.Created,
            StatusMessage = "Shipment created successfully",
            EstimatedDeliveryDate = gatewayResult.EstimatedDeliveryDate,
            LabelUrl = gatewayResult.LabelUrl,
            CreatedAt = now,
            LastUpdatedAt = now,
            StoreShippingProvider = storeProvider
        };

        await _shipmentRepository.AddAsync(shipment);

        // Record initial status update
        var statusUpdate = new ShipmentStatusUpdate
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Status = ShipmentStatus.Created,
            StatusMessage = "Shipment created",
            Timestamp = now,
            CreatedAt = now
        };

        await _shipmentStatusUpdateRepository.AddAsync(statusUpdate);

        return CreateShipmentResult.Success(shipment);
    }

    /// <inheritdoc />
    public async Task<GetShipmentResult> GetShipmentAsync(Guid shipmentId, Guid storeId)
    {
        if (shipmentId == Guid.Empty)
        {
            return GetShipmentResult.Failure("Shipment ID is required.");
        }

        if (storeId == Guid.Empty)
        {
            return GetShipmentResult.Failure("Store ID is required.");
        }

        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            return GetShipmentResult.Failure("Shipment not found.");
        }

        // Check authorization
        if (shipment.StoreShippingProvider.StoreId != storeId)
        {
            return GetShipmentResult.NotAuthorized();
        }

        return GetShipmentResult.Success(shipment);
    }

    /// <inheritdoc />
    public async Task<GetShipmentsResult> GetShipmentsForSubOrderAsync(Guid sellerSubOrderId, Guid storeId)
    {
        if (sellerSubOrderId == Guid.Empty)
        {
            return GetShipmentsResult.Failure("Seller sub-order ID is required.");
        }

        if (storeId == Guid.Empty)
        {
            return GetShipmentsResult.Failure("Store ID is required.");
        }

        var shipments = await _shipmentRepository.GetBySellerSubOrderIdAsync(sellerSubOrderId);

        // Filter shipments to only those belonging to this store
        var authorizedShipments = shipments
            .Where(s => s.StoreShippingProvider.StoreId == storeId)
            .ToList();

        return GetShipmentsResult.Success(authorizedShipments);
    }

    /// <inheritdoc />
    public async Task<UpdateShipmentStatusResult> UpdateShipmentStatusAsync(UpdateShipmentStatusCommand command)
    {
        var errors = ValidateUpdateStatusCommand(command);
        if (errors.Count > 0)
        {
            return UpdateShipmentStatusResult.Failure(errors);
        }

        // Find shipment by ID, tracking number, or external ID
        Shipment? shipment = null;

        if (command.ShipmentId.HasValue)
        {
            shipment = await _shipmentRepository.GetByIdAsync(command.ShipmentId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(command.TrackingNumber))
        {
            shipment = await _shipmentRepository.GetByTrackingNumberAsync(command.TrackingNumber);
        }
        else if (!string.IsNullOrWhiteSpace(command.ExternalShipmentId))
        {
            shipment = await _shipmentRepository.GetByExternalShipmentIdAsync(command.ExternalShipmentId);
        }

        if (shipment == null)
        {
            return UpdateShipmentStatusResult.Failure("Shipment not found.");
        }

        var previousStatus = shipment.Status;
        var now = DateTimeOffset.UtcNow;

        // Update shipment status
        shipment.Status = command.Status;
        shipment.StatusMessage = command.StatusMessage;
        shipment.LastUpdatedAt = now;

        // Set timestamp for specific status transitions
        if (command.Status == ShipmentStatus.PickedUp && !shipment.ShippedAt.HasValue)
        {
            shipment.ShippedAt = command.Timestamp;
        }

        if (command.Status == ShipmentStatus.Delivered && !shipment.DeliveredAt.HasValue)
        {
            shipment.DeliveredAt = command.Timestamp;
        }

        await _shipmentRepository.UpdateAsync(shipment);

        // Record status update
        var statusUpdate = new ShipmentStatusUpdate
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Status = command.Status,
            StatusMessage = command.StatusMessage,
            Location = command.Location,
            Timestamp = command.Timestamp,
            CreatedAt = now
        };

        await _shipmentStatusUpdateRepository.AddAsync(statusUpdate);

        return UpdateShipmentStatusResult.Success(shipment, previousStatus);
    }

    /// <inheritdoc />
    public async Task<PollShipmentStatusResult> PollShipmentStatusAsync(Guid shipmentId)
    {
        if (shipmentId == Guid.Empty)
        {
            return PollShipmentStatusResult.Failure("Shipment ID is required.");
        }

        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            return PollShipmentStatusResult.Failure("Shipment not found.");
        }

        // Get the gateway
        var gateway = _gatewayFactory.GetGateway(shipment.StoreShippingProvider.ShippingProvider.Code);
        if (gateway == null)
        {
            return PollShipmentStatusResult.Failure("Gateway not available for this provider.");
        }

        // Poll the provider for status
        var gatewayRequest = new GetShipmentStatusGatewayRequest
        {
            TrackingNumber = shipment.TrackingNumber,
            ExternalShipmentId = shipment.ExternalShipmentId,
            CredentialIdentifier = shipment.StoreShippingProvider.CredentialIdentifier
        };

        var gatewayResult = await gateway.GetShipmentStatusAsync(gatewayRequest);
        if (!gatewayResult.Succeeded)
        {
            return PollShipmentStatusResult.Failure(gatewayResult.Errors);
        }

        // Check if status has changed
        if (gatewayResult.Status == shipment.Status)
        {
            return PollShipmentStatusResult.SuccessNoChange(shipment);
        }

        var previousStatus = shipment.Status;
        var now = DateTimeOffset.UtcNow;

        // Update shipment
        shipment.Status = gatewayResult.Status!.Value;
        shipment.StatusMessage = gatewayResult.StatusMessage;
        shipment.LastUpdatedAt = now;

        if (gatewayResult.EstimatedDeliveryDate.HasValue)
        {
            shipment.EstimatedDeliveryDate = gatewayResult.EstimatedDeliveryDate;
        }

        if (gatewayResult.Status == ShipmentStatus.PickedUp && !shipment.ShippedAt.HasValue)
        {
            shipment.ShippedAt = gatewayResult.Timestamp ?? now;
        }

        if (gatewayResult.Status == ShipmentStatus.Delivered && !shipment.DeliveredAt.HasValue)
        {
            shipment.DeliveredAt = gatewayResult.Timestamp ?? now;
        }

        await _shipmentRepository.UpdateAsync(shipment);

        // Record new tracking events
        if (gatewayResult.TrackingEvents.Count > 0)
        {
            var existingUpdates = await _shipmentStatusUpdateRepository.GetByShipmentIdAsync(shipment.Id);
            var existingTimestamps = existingUpdates.Select(u => u.Timestamp).ToHashSet();

            var newUpdates = gatewayResult.TrackingEvents
                .Where(e => !existingTimestamps.Contains(e.Timestamp))
                .Select(e => new ShipmentStatusUpdate
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    Status = e.Status,
                    StatusMessage = e.StatusMessage,
                    Location = e.Location,
                    Timestamp = e.Timestamp,
                    CreatedAt = now
                })
                .ToList();

            if (newUpdates.Count > 0)
            {
                await _shipmentStatusUpdateRepository.AddRangeAsync(newUpdates);
            }
        }

        return PollShipmentStatusResult.SuccessWithChange(shipment, previousStatus);
    }

    /// <inheritdoc />
    public async Task<CancelShipmentResult> CancelShipmentAsync(Guid shipmentId, Guid storeId, string? reason)
    {
        if (shipmentId == Guid.Empty)
        {
            return CancelShipmentResult.Failure("Shipment ID is required.");
        }

        if (storeId == Guid.Empty)
        {
            return CancelShipmentResult.Failure("Store ID is required.");
        }

        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            return CancelShipmentResult.Failure("Shipment not found.");
        }

        // Check authorization
        if (shipment.StoreShippingProvider.StoreId != storeId)
        {
            return CancelShipmentResult.NotAuthorized();
        }

        // Check if cancellation is possible
        if (shipment.Status == ShipmentStatus.Delivered || shipment.Status == ShipmentStatus.Returned)
        {
            return CancelShipmentResult.Failure("Cannot cancel a delivered or returned shipment.");
        }

        // Get the gateway
        var gateway = _gatewayFactory.GetGateway(shipment.StoreShippingProvider.ShippingProvider.Code);
        if (gateway == null)
        {
            return CancelShipmentResult.Failure("Gateway not available for this provider.");
        }

        // Cancel with provider
        var gatewayRequest = new CancelShipmentGatewayRequest
        {
            TrackingNumber = shipment.TrackingNumber,
            ExternalShipmentId = shipment.ExternalShipmentId,
            CredentialIdentifier = shipment.StoreShippingProvider.CredentialIdentifier,
            Reason = reason
        };

        var gatewayResult = await gateway.CancelShipmentAsync(gatewayRequest);
        if (!gatewayResult.Succeeded)
        {
            return CancelShipmentResult.Failure(gatewayResult.Errors);
        }

        // Update shipment status
        var now = DateTimeOffset.UtcNow;
        shipment.Status = ShipmentStatus.Returned;
        shipment.StatusMessage = reason ?? "Shipment cancelled by seller";
        shipment.LastUpdatedAt = now;

        await _shipmentRepository.UpdateAsync(shipment);

        // Record cancellation
        var statusUpdate = new ShipmentStatusUpdate
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Status = ShipmentStatus.Returned,
            StatusMessage = reason ?? "Shipment cancelled",
            Timestamp = now,
            CreatedAt = now
        };

        await _shipmentStatusUpdateRepository.AddAsync(statusUpdate);

        return CancelShipmentResult.Success();
    }

    /// <inheritdoc />
    public async Task<GetTrackingHistoryResult> GetTrackingHistoryAsync(Guid shipmentId)
    {
        if (shipmentId == Guid.Empty)
        {
            return GetTrackingHistoryResult.Failure("Shipment ID is required.");
        }

        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            return GetTrackingHistoryResult.Failure("Shipment not found.");
        }

        var statusUpdates = await _shipmentStatusUpdateRepository.GetByShipmentIdAsync(shipmentId);

        return GetTrackingHistoryResult.Success(shipment, statusUpdates);
    }

    private static List<string> ValidateCreateShipmentCommand(CreateShipmentCommand command)
    {
        var errors = new List<string>();

        if (command.SellerSubOrderId == Guid.Empty)
        {
            errors.Add("Seller sub-order ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (command.StoreShippingProviderId == Guid.Empty)
        {
            errors.Add("Store shipping provider ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RecipientAddress.Name))
        {
            errors.Add("Recipient name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RecipientAddress.AddressLine1))
        {
            errors.Add("Recipient address line 1 is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RecipientAddress.City))
        {
            errors.Add("Recipient city is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RecipientAddress.PostalCode))
        {
            errors.Add("Recipient postal code is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RecipientAddress.CountryCode))
        {
            errors.Add("Recipient country code is required.");
        }

        if (command.WeightKg <= 0)
        {
            errors.Add("Package weight must be greater than zero.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateStatusCommand(UpdateShipmentStatusCommand command)
    {
        var errors = new List<string>();

        if (!command.ShipmentId.HasValue &&
            string.IsNullOrWhiteSpace(command.TrackingNumber) &&
            string.IsNullOrWhiteSpace(command.ExternalShipmentId))
        {
            errors.Add("Either shipment ID, tracking number, or external shipment ID is required.");
        }

        return errors;
    }
}
