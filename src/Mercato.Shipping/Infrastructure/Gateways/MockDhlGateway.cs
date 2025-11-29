using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Infrastructure.Gateways;

/// <summary>
/// Mock implementation of the DHL shipping provider gateway for testing and development.
/// </summary>
public class MockDhlGateway : IShippingProviderGateway
{
    private static readonly Random Random = new();

    /// <inheritdoc />
    public string ProviderCode => "DHL";

    /// <inheritdoc />
    public async Task<CreateShipmentGatewayResult> CreateShipmentAsync(CreateShipmentGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(100, 500));

        // Simulate occasional failures (5% failure rate)
        if (Random.Next(100) < 5)
        {
            return CreateShipmentGatewayResult.Failure("DHL API temporarily unavailable. Please try again.");
        }

        // Generate mock tracking number
        var trackingNumber = $"DHL{DateTime.UtcNow:yyyyMMdd}{Random.Next(100000, 999999)}";
        var externalId = $"DHL-EXT-{Guid.NewGuid():N}";
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(Random.Next(3, 7));
        var labelUrl = $"https://mock-dhl-labels.example.com/labels/{externalId}.pdf";

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
        await Task.Delay(Random.Next(50, 200));

        // Simulate occasional failures (2% failure rate)
        if (Random.Next(100) < 2)
        {
            return GetShipmentStatusGatewayResult.Failure("Failed to retrieve shipment status from DHL.");
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
            DateTimeOffset.UtcNow.AddHours(-Random.Next(1, 24)),
            DateTimeOffset.UtcNow.AddDays(Random.Next(1, 3)),
            trackingEvents);
    }

    /// <inheritdoc />
    public async Task<CancelShipmentGatewayResult> CancelShipmentAsync(CancelShipmentGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(100, 300));

        // Simulate that only recently created shipments can be cancelled (90% success)
        if (Random.Next(100) < 10)
        {
            return CancelShipmentGatewayResult.Failure("Shipment has already been picked up and cannot be cancelled.");
        }

        return CancelShipmentGatewayResult.Success();
    }

    /// <inheritdoc />
    public async Task<GetShippingLabelGatewayResult> GetLabelAsync(GetShippingLabelGatewayRequest request)
    {
        // Simulate API call delay
        await Task.Delay(Random.Next(100, 400));

        // Simulate occasional failures (3% failure rate)
        if (Random.Next(100) < 3)
        {
            return GetShippingLabelGatewayResult.Failure("DHL label service temporarily unavailable. Please try again.");
        }

        // Validate tracking number
        if (string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            return GetShippingLabelGatewayResult.Failure("Tracking number is required to retrieve label.");
        }

        // Generate mock PDF label data
        var labelData = MockLabelGenerator.GenerateMockPdfLabel(request.TrackingNumber, "DHL Express");
        var fileName = $"DHL-Label-{request.TrackingNumber}.pdf";

        return GetShippingLabelGatewayResult.Success(labelData, "application/pdf", fileName);
    }

    private static ShipmentStatus GetMockStatus(string trackingNumber)
    {
        // Use tracking number hash to generate consistent but varied statuses
        var hash = trackingNumber.GetHashCode();
        var index = Math.Abs(hash % 5);

        return index switch
        {
            0 => ShipmentStatus.Created,
            1 => ShipmentStatus.PickedUp,
            2 => ShipmentStatus.InTransit,
            3 => ShipmentStatus.OutForDelivery,
            _ => ShipmentStatus.InTransit
        };
    }

    private static string GetStatusMessage(ShipmentStatus status)
    {
        return status switch
        {
            ShipmentStatus.Created => "Shipment label created, awaiting pickup",
            ShipmentStatus.PickedUp => "Package picked up from sender",
            ShipmentStatus.InTransit => "Package in transit to destination facility",
            ShipmentStatus.OutForDelivery => "Package is out for delivery",
            ShipmentStatus.Delivered => "Package delivered successfully",
            ShipmentStatus.Exception => "Delivery exception - address issue",
            ShipmentStatus.Returned => "Package returned to sender",
            _ => "Status update available"
        };
    }

    private static string GetMockLocation(ShipmentStatus status)
    {
        return status switch
        {
            ShipmentStatus.Created => "Origin Facility, Frankfurt, DE",
            ShipmentStatus.PickedUp => "DHL Service Point, Frankfurt, DE",
            ShipmentStatus.InTransit => "DHL Distribution Center, Leipzig, DE",
            ShipmentStatus.OutForDelivery => "Local Delivery Facility",
            ShipmentStatus.Delivered => "Delivered to recipient",
            _ => "DHL Facility"
        };
    }

    private static List<ShipmentTrackingEvent> GenerateMockTrackingEvents(ShipmentStatus currentStatus)
    {
        var events = new List<ShipmentTrackingEvent>
        {
            new()
            {
                Status = ShipmentStatus.Created,
                StatusMessage = "Shipment information received",
                Location = "Origin Facility, Frankfurt, DE",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            }
        };

        if (currentStatus >= ShipmentStatus.PickedUp)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.PickedUp,
                StatusMessage = "Package picked up",
                Location = "DHL Service Point, Frankfurt, DE",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2).AddHours(4)
            });
        }

        if (currentStatus >= ShipmentStatus.InTransit)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.InTransit,
                StatusMessage = "In transit",
                Location = "DHL Distribution Center, Leipzig, DE",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            });
        }

        if (currentStatus >= ShipmentStatus.OutForDelivery)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.OutForDelivery,
                StatusMessage = "Out for delivery",
                Location = "Local Delivery Facility",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2)
            });
        }

        if (currentStatus == ShipmentStatus.Delivered)
        {
            events.Add(new ShipmentTrackingEvent
            {
                Status = ShipmentStatus.Delivered,
                StatusMessage = "Delivered",
                Location = "Delivered to recipient",
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        return events;
    }
}
