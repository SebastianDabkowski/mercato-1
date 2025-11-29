using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Gateways;

namespace Mercato.Shipping.Infrastructure;

/// <summary>
/// Service implementation for shipping label operations.
/// </summary>
public class ShippingLabelService : IShippingLabelService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShippingLabelRepository _shippingLabelRepository;
    private readonly IShippingProviderGatewayFactory _gatewayFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingLabelService"/> class.
    /// </summary>
    /// <param name="shipmentRepository">The shipment repository.</param>
    /// <param name="shippingLabelRepository">The shipping label repository.</param>
    /// <param name="gatewayFactory">The shipping provider gateway factory.</param>
    public ShippingLabelService(
        IShipmentRepository shipmentRepository,
        IShippingLabelRepository shippingLabelRepository,
        IShippingProviderGatewayFactory gatewayFactory)
    {
        _shipmentRepository = shipmentRepository;
        _shippingLabelRepository = shippingLabelRepository;
        _gatewayFactory = gatewayFactory;
    }

    /// <inheritdoc />
    public async Task<GenerateLabelResult> GenerateLabelAsync(Guid shipmentId, Guid storeId)
    {
        var errors = ValidateLabelRequest(shipmentId, storeId);
        if (errors.Count > 0)
        {
            return GenerateLabelResult.Failure(errors);
        }

        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            return GenerateLabelResult.Failure("Shipment not found.");
        }

        // Check authorization
        if (shipment.StoreShippingProvider.StoreId != storeId)
        {
            return GenerateLabelResult.NotAuthorized();
        }

        // Check if label already exists
        var existingLabel = await _shippingLabelRepository.GetByShipmentIdAsync(shipmentId);
        if (existingLabel != null)
        {
            return GenerateLabelResult.Success(existingLabel);
        }

        // Get the gateway
        var gateway = _gatewayFactory.GetGateway(shipment.StoreShippingProvider.ShippingProvider.Code);
        if (gateway == null)
        {
            return GenerateLabelResult.Failure($"Gateway not available for provider: {shipment.StoreShippingProvider.ShippingProvider.Code}");
        }

        // Request label from provider
        var labelRequest = new GetShippingLabelGatewayRequest
        {
            TrackingNumber = shipment.TrackingNumber,
            ExternalShipmentId = shipment.ExternalShipmentId,
            CredentialIdentifier = shipment.StoreShippingProvider.CredentialIdentifier,
            LabelFormat = "PDF"
        };

        var gatewayResult = await gateway.GetLabelAsync(labelRequest);
        if (!gatewayResult.Succeeded)
        {
            return GenerateLabelResult.Failure(gatewayResult.Errors);
        }

        // Create and store the label
        var now = DateTimeOffset.UtcNow;
        var label = new ShippingLabel
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            LabelData = gatewayResult.LabelData!,
            ContentType = gatewayResult.ContentType ?? "application/pdf",
            FileName = gatewayResult.FileName ?? $"label-{shipment.TrackingNumber}.pdf",
            LabelFormat = "PDF",
            CreatedAt = now,
            ExpiresAt = now.AddDays(30), // Labels typically expire after 30 days
            Shipment = shipment
        };

        await _shippingLabelRepository.AddAsync(label);

        return GenerateLabelResult.Success(label);
    }

    /// <inheritdoc />
    public async Task<GetLabelResult> GetLabelAsync(Guid shipmentId, Guid storeId)
    {
        var errors = ValidateLabelRequest(shipmentId, storeId);
        if (errors.Count > 0)
        {
            return GetLabelResult.Failure(errors);
        }

        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            return GetLabelResult.Failure("Shipment not found.");
        }

        // Check authorization
        if (shipment.StoreShippingProvider.StoreId != storeId)
        {
            return GetLabelResult.NotAuthorized();
        }

        var label = await _shippingLabelRepository.GetByShipmentIdAsync(shipmentId);
        if (label == null)
        {
            return GetLabelResult.Failure("Shipping label not found for this shipment.");
        }

        return GetLabelResult.Success(label);
    }

    private static List<string> ValidateLabelRequest(Guid shipmentId, Guid storeId)
    {
        var errors = new List<string>();

        if (shipmentId == Guid.Empty)
        {
            errors.Add("Shipment ID is required.");
        }

        if (storeId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        return errors;
    }
}
