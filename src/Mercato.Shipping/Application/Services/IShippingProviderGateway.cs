using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Application.Services;

/// <summary>
/// Abstraction for shipping provider API interactions.
/// </summary>
public interface IShippingProviderGateway
{
    /// <summary>
    /// Gets the provider code this gateway handles.
    /// </summary>
    string ProviderCode { get; }

    /// <summary>
    /// Creates a shipment with the shipping provider.
    /// </summary>
    /// <param name="request">The shipment creation request.</param>
    /// <returns>The result of the shipment creation.</returns>
    Task<CreateShipmentGatewayResult> CreateShipmentAsync(CreateShipmentGatewayRequest request);

    /// <summary>
    /// Gets the current status of a shipment from the provider.
    /// </summary>
    /// <param name="request">The status request.</param>
    /// <returns>The shipment status from the provider.</returns>
    Task<GetShipmentStatusGatewayResult> GetShipmentStatusAsync(GetShipmentStatusGatewayRequest request);

    /// <summary>
    /// Cancels a shipment with the provider.
    /// </summary>
    /// <param name="request">The cancellation request.</param>
    /// <returns>The result of the cancellation.</returns>
    Task<CancelShipmentGatewayResult> CancelShipmentAsync(CancelShipmentGatewayRequest request);
}

/// <summary>
/// Request to create a shipment via the provider gateway.
/// </summary>
public class CreateShipmentGatewayRequest
{
    /// <summary>
    /// Gets or sets the seller's account number with the provider.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the credential identifier for API authentication.
    /// </summary>
    public string? CredentialIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the sender's address information.
    /// </summary>
    public ShippingAddress SenderAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets the recipient's address information.
    /// </summary>
    public ShippingAddress RecipientAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets the package weight in kilograms.
    /// </summary>
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Gets or sets the package dimensions.
    /// </summary>
    public PackageDimensions? Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the reference number for the shipment (e.g., order number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the service type (e.g., "EXPRESS", "STANDARD").
    /// </summary>
    public string ServiceType { get; set; } = "STANDARD";
}

/// <summary>
/// Result of creating a shipment via the provider gateway.
/// </summary>
public class CreateShipmentGatewayResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the tracking number assigned by the provider.
    /// </summary>
    public string? TrackingNumber { get; private init; }

    /// <summary>
    /// Gets the external shipment ID from the provider.
    /// </summary>
    public string? ExternalShipmentId { get; private init; }

    /// <summary>
    /// Gets the estimated delivery date.
    /// </summary>
    public DateTimeOffset? EstimatedDeliveryDate { get; private init; }

    /// <summary>
    /// Gets the URL for the shipping label.
    /// </summary>
    public string? LabelUrl { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="externalShipmentId">The external shipment ID.</param>
    /// <param name="estimatedDeliveryDate">The estimated delivery date.</param>
    /// <param name="labelUrl">The label URL.</param>
    /// <returns>A successful result.</returns>
    public static CreateShipmentGatewayResult Success(
        string trackingNumber, 
        string? externalShipmentId = null, 
        DateTimeOffset? estimatedDeliveryDate = null,
        string? labelUrl = null) => new()
    {
        Succeeded = true,
        Errors = [],
        TrackingNumber = trackingNumber,
        ExternalShipmentId = externalShipmentId,
        EstimatedDeliveryDate = estimatedDeliveryDate,
        LabelUrl = labelUrl
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateShipmentGatewayResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateShipmentGatewayResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Request to get shipment status from the provider.
/// </summary>
public class GetShipmentStatusGatewayRequest
{
    /// <summary>
    /// Gets or sets the tracking number.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external shipment ID.
    /// </summary>
    public string? ExternalShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the credential identifier for API authentication.
    /// </summary>
    public string? CredentialIdentifier { get; set; }
}

/// <summary>
/// Result of getting shipment status from the provider.
/// </summary>
public class GetShipmentStatusGatewayResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the current status of the shipment.
    /// </summary>
    public ShipmentStatus? Status { get; private init; }

    /// <summary>
    /// Gets the status message from the provider.
    /// </summary>
    public string? StatusMessage { get; private init; }

    /// <summary>
    /// Gets the current location of the shipment.
    /// </summary>
    public string? Location { get; private init; }

    /// <summary>
    /// Gets the timestamp of the status update.
    /// </summary>
    public DateTimeOffset? Timestamp { get; private init; }

    /// <summary>
    /// Gets the updated estimated delivery date.
    /// </summary>
    public DateTimeOffset? EstimatedDeliveryDate { get; private init; }

    /// <summary>
    /// Gets the list of tracking events from the provider.
    /// </summary>
    public IReadOnlyList<ShipmentTrackingEvent> TrackingEvents { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="status">The current status.</param>
    /// <param name="statusMessage">The status message.</param>
    /// <param name="location">The current location.</param>
    /// <param name="timestamp">The status timestamp.</param>
    /// <param name="estimatedDeliveryDate">The estimated delivery date.</param>
    /// <param name="trackingEvents">The tracking events.</param>
    /// <returns>A successful result.</returns>
    public static GetShipmentStatusGatewayResult Success(
        ShipmentStatus status,
        string? statusMessage = null,
        string? location = null,
        DateTimeOffset? timestamp = null,
        DateTimeOffset? estimatedDeliveryDate = null,
        IReadOnlyList<ShipmentTrackingEvent>? trackingEvents = null) => new()
    {
        Succeeded = true,
        Errors = [],
        Status = status,
        StatusMessage = statusMessage,
        Location = location,
        Timestamp = timestamp,
        EstimatedDeliveryDate = estimatedDeliveryDate,
        TrackingEvents = trackingEvents ?? []
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShipmentStatusGatewayResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShipmentStatusGatewayResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Represents a tracking event from the shipping provider.
/// </summary>
public class ShipmentTrackingEvent
{
    /// <summary>
    /// Gets or sets the status at this event.
    /// </summary>
    public ShipmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the location at this event.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of this event.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Request to cancel a shipment via the provider.
/// </summary>
public class CancelShipmentGatewayRequest
{
    /// <summary>
    /// Gets or sets the tracking number.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external shipment ID.
    /// </summary>
    public string? ExternalShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the credential identifier for API authentication.
    /// </summary>
    public string? CredentialIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the reason for cancellation.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Result of cancelling a shipment via the provider.
/// </summary>
public class CancelShipmentGatewayResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the cancellation was confirmed.
    /// </summary>
    public bool IsCancelled { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static CancelShipmentGatewayResult Success() => new()
    {
        Succeeded = true,
        Errors = [],
        IsCancelled = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static CancelShipmentGatewayResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CancelShipmentGatewayResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Represents an address for shipping purposes.
/// </summary>
public class ShippingAddress
{
    /// <summary>
    /// Gets or sets the recipient name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the company name.
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Gets or sets the first address line.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the second address line.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country code (ISO 3166-1 alpha-2).
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// Represents package dimensions.
/// </summary>
public class PackageDimensions
{
    /// <summary>
    /// Gets or sets the length in centimeters.
    /// </summary>
    public decimal LengthCm { get; set; }

    /// <summary>
    /// Gets or sets the width in centimeters.
    /// </summary>
    public decimal WidthCm { get; set; }

    /// <summary>
    /// Gets or sets the height in centimeters.
    /// </summary>
    public decimal HeightCm { get; set; }
}
