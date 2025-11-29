using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Infrastructure.Gateways;

/// <summary>
/// Mock implementation of the FedEx shipping provider gateway for testing and development.
/// </summary>
public class MockFedExGateway : IShippingProviderGateway
{
    private static readonly Random Random = new();

    /// <inheritdoc />
    public string ProviderCode => "FEDEX";

    /// <inheritdoc />
    public async Task<CreateShipmentGatewayResult> CreateShipmentAsync(CreateShipmentGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(150, 600));

        // Simulate occasional failures (3% failure rate)
        if (Random.Next(100) < 3)
        {
            return CreateShipmentGatewayResult.Failure("FedEx service temporarily unavailable.");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.RecipientAddress.PostalCode))
        {
            return CreateShipmentGatewayResult.Failure("Recipient postal code is required.");
        }

        // Generate mock tracking number
        var trackingNumber = $"FX{Random.Next(10000000, 99999999)}{Random.Next(1000, 9999)}";
        var externalId = $"FEDEX-{Guid.NewGuid():N}";
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(Random.Next(2, 5));
        var labelUrl = $"https://mock-fedex-labels.example.com/labels/{externalId}.pdf";

        return CreateShipmentGatewayResult.Success(
            trackingNumber,
            externalId,
            estimatedDelivery,
            labelUrl);
    }

    /// <inheritdoc />
    public async Task<GetShipmentStatusGatewayResult> GetShipmentStatusAsync(GetShipmentStatusGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(50, 250));

        // Simulate occasional failures (2% failure rate)
        if (Random.Next(100) < 2)
        {
            return GetShipmentStatusGatewayResult.Failure("Failed to retrieve shipment status from FedEx.");
        }

        // Generate mock status based on tracking number pattern
        var status = GetMockStatus(request.TrackingNumber);
        var statusMessage = GetStatusMessage(status);
        var location = GetMockLocation(status);

        var trackingEvents = GenerateMockTrackingEvents(status);

        return GetShipmentStatusGatewayResult.Success(
            status,
            statusMessage,
            location,
            DateTimeOffset.UtcNow.AddHours(-Random.Next(1, 12)),
            DateTimeOffset.UtcNow.AddDays(Random.Next(1, 2)),
            trackingEvents);
    }

    /// <inheritdoc />
    public async Task<CancelShipmentGatewayResult> CancelShipmentAsync(CancelShipmentGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(100, 400));

        // Simulate that only recently created shipments can be cancelled (85% success)
        if (Random.Next(100) < 15)
        {
            return CancelShipmentGatewayResult.Failure("Shipment is already in transit and cannot be cancelled.");
        }

        return CancelShipmentGatewayResult.Success();
    }

    /// <inheritdoc />
    public async Task<GetShippingLabelGatewayResult> GetLabelAsync(GetShippingLabelGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(100, 500));

        // Simulate occasional failures (4% failure rate)
        if (Random.Next(100) < 4)
        {
            return GetShippingLabelGatewayResult.Failure("FedEx label service temporarily unavailable. Please try again.");
        }

        // Validate tracking number
        if (string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            return GetShippingLabelGatewayResult.Failure("Tracking number is required to retrieve label.");
        }

        // Generate mock PDF label data
        var labelData = MockLabelGenerator.GenerateMockPdfLabel(request.TrackingNumber, "FedEx");
        var fileName = $"FedEx-Label-{request.TrackingNumber}.pdf";

        return GetShippingLabelGatewayResult.Success(labelData, "application/pdf", fileName);
    }

    private static ShipmentStatus GetMockStatus(string trackingNumber)
    {
        // Use tracking number hash to generate consistent but varied statuses
        var hash = trackingNumber.GetHashCode();
        var index = Math.Abs(hash % 6);

        return index switch
        {
            0 => ShipmentStatus.Created,
            1 => ShipmentStatus.PickedUp,
            2 => ShipmentStatus.InTransit,
            3 => ShipmentStatus.InTransit,
            4 => ShipmentStatus.OutForDelivery,
            _ => ShipmentStatus.InTransit
        };
    }

    private static string GetStatusMessage(ShipmentStatus status)
    {
        return status switch
        {
            ShipmentStatus.Created => "Shipment information sent to FedEx",
            ShipmentStatus.PickedUp => "Picked up",
            ShipmentStatus.InTransit => "In transit - on schedule",
            ShipmentStatus.OutForDelivery => "On FedEx vehicle for delivery",
            ShipmentStatus.Delivered => "Delivered - Left at front door",
            ShipmentStatus.Exception => "Delivery exception",
            ShipmentStatus.Returned => "Package being returned",
            _ => "Status available"
        };
    }

    private static string GetMockLocation(ShipmentStatus status)
    {
        return status switch
        {
            ShipmentStatus.Created => "MEMPHIS, TN",
            ShipmentStatus.PickedUp => "Local FedEx Facility",
            ShipmentStatus.InTransit => "INDIANAPOLIS, IN",
            ShipmentStatus.OutForDelivery => "Local Delivery Station",
            ShipmentStatus.Delivered => "Delivered",
            _ => "FedEx Facility"
        };
    }

    private static List<ShipmentTrackingEvent> GenerateMockTrackingEvents(ShipmentStatus currentStatus)
    {
        var events = new List<ShipmentTrackingEvent>
        {
            new()
            {
                Status = ShipmentStatus.Created,
                StatusMessage = "Shipment information sent to FedEx",
                Location = "MEMPHIS, TN",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-3)
            }
        };

        if (currentStatus >= ShipmentStatus.PickedUp)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.PickedUp,
                StatusMessage = "Picked up",
                Location = "Local FedEx Facility",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2).AddHours(8)
            });
        }

        if (currentStatus >= ShipmentStatus.InTransit)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.InTransit,
                StatusMessage = "Departed FedEx location",
                Location = "MEMPHIS, TN",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2).AddHours(12)
            });

            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.InTransit,
                StatusMessage = "Arrived at FedEx location",
                Location = "INDIANAPOLIS, IN",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            });
        }

        if (currentStatus >= ShipmentStatus.OutForDelivery)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.OutForDelivery,
                StatusMessage = "On FedEx vehicle for delivery",
                Location = "Local Delivery Station",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3)
            });
        }

        if (currentStatus == ShipmentStatus.Delivered)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.Delivered,
                StatusMessage = "Delivered - Left at front door",
                Location = "Delivered",
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        return events;
    }
}
