using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Application.Services;

/// <summary>
/// Service interface for managing shipments.
/// </summary>
public interface IShipmentService
{
    /// <summary>
    /// Creates a shipment for a seller sub-order via the configured shipping provider.
    /// </summary>
    /// <param name="command">The create shipment command.</param>
    /// <returns>The result of the shipment creation.</returns>
    Task<CreateShipmentResult> CreateShipmentAsync(CreateShipmentCommand command);

    /// <summary>
    /// Gets a shipment by its identifier.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <param name="storeId">The store identifier for authorization.</param>
    /// <returns>The result containing the shipment.</returns>
    Task<GetShipmentResult> GetShipmentAsync(Guid shipmentId, Guid storeId);

    /// <summary>
    /// Gets all shipments for a seller sub-order.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order identifier.</param>
    /// <param name="storeId">The store identifier for authorization.</param>
    /// <returns>The result containing the shipments.</returns>
    Task<GetShipmentsResult> GetShipmentsForSubOrderAsync(Guid sellerSubOrderId, Guid storeId);

    /// <summary>
    /// Updates the status of a shipment from a webhook or poll.
    /// </summary>
    /// <param name="command">The update status command.</param>
    /// <returns>The result of the status update.</returns>
    Task<UpdateShipmentStatusResult> UpdateShipmentStatusAsync(UpdateShipmentStatusCommand command);

    /// <summary>
    /// Polls the shipping provider for updated status on a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <returns>The result of the status poll.</returns>
    Task<PollShipmentStatusResult> PollShipmentStatusAsync(Guid shipmentId);

    /// <summary>
    /// Cancels a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <param name="storeId">The store identifier for authorization.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <returns>The result of the cancellation.</returns>
    Task<CancelShipmentResult> CancelShipmentAsync(Guid shipmentId, Guid storeId, string? reason);

    /// <summary>
    /// Gets the tracking history for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <returns>The result containing the tracking history.</returns>
    Task<GetTrackingHistoryResult> GetTrackingHistoryAsync(Guid shipmentId);
}

/// <summary>
/// Command to create a shipment.
/// </summary>
public class CreateShipmentCommand
{
    /// <summary>
    /// Gets or sets the seller sub-order identifier.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the store identifier.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store shipping provider identifier.
    /// </summary>
    public Guid StoreShippingProviderId { get; set; }

    /// <summary>
    /// Gets or sets the sender's address.
    /// </summary>
    public ShippingAddress SenderAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets the recipient's address.
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
    /// Gets or sets the reference number for the shipment.
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the service type.
    /// </summary>
    public string ServiceType { get; set; } = "STANDARD";
}

/// <summary>
/// Result of creating a shipment.
/// </summary>
public class CreateShipmentResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the created shipment.
    /// </summary>
    public Shipment? Shipment { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="shipment">The created shipment.</param>
    /// <returns>A successful result.</returns>
    public static CreateShipmentResult Success(Shipment shipment) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipment = shipment
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateShipmentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateShipmentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateShipmentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a shipment.
/// </summary>
public class GetShipmentResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the shipment.
    /// </summary>
    public Shipment? Shipment { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="shipment">The shipment.</param>
    /// <returns>A successful result.</returns>
    public static GetShipmentResult Success(Shipment shipment) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipment = shipment
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShipmentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShipmentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetShipmentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting shipments.
/// </summary>
public class GetShipmentsResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the shipments.
    /// </summary>
    public IReadOnlyList<Shipment> Shipments { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="shipments">The shipments.</param>
    /// <returns>A successful result.</returns>
    public static GetShipmentsResult Success(IReadOnlyList<Shipment> shipments) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipments = shipments
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShipmentsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShipmentsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetShipmentsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update a shipment status from webhook or poll.
/// </summary>
public class UpdateShipmentStatusCommand
{
    /// <summary>
    /// Gets or sets the shipment identifier.
    /// </summary>
    public Guid? ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number to identify the shipment.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the external shipment ID from the provider.
    /// </summary>
    public string? ExternalShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the new status.
    /// </summary>
    public ShipmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the location at the time of the status update.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the status update.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Result of updating a shipment status.
/// </summary>
public class UpdateShipmentStatusResult
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
    /// Gets the updated shipment.
    /// </summary>
    public Shipment? Shipment { get; private init; }

    /// <summary>
    /// Gets the previous status before the update.
    /// </summary>
    public ShipmentStatus? PreviousStatus { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="shipment">The updated shipment.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <returns>A successful result.</returns>
    public static UpdateShipmentStatusResult Success(Shipment shipment, ShipmentStatus previousStatus) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipment = shipment,
        PreviousStatus = previousStatus
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateShipmentStatusResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateShipmentStatusResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of polling a shipment status.
/// </summary>
public class PollShipmentStatusResult
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
    /// Gets the shipment.
    /// </summary>
    public Shipment? Shipment { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the status changed.
    /// </summary>
    public bool StatusChanged { get; private init; }

    /// <summary>
    /// Gets the previous status if changed.
    /// </summary>
    public ShipmentStatus? PreviousStatus { get; private init; }

    /// <summary>
    /// Creates a successful result with no status change.
    /// </summary>
    /// <param name="shipment">The shipment.</param>
    /// <returns>A successful result.</returns>
    public static PollShipmentStatusResult SuccessNoChange(Shipment shipment) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipment = shipment,
        StatusChanged = false
    };

    /// <summary>
    /// Creates a successful result with a status change.
    /// </summary>
    /// <param name="shipment">The updated shipment.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <returns>A successful result.</returns>
    public static PollShipmentStatusResult SuccessWithChange(Shipment shipment, ShipmentStatus previousStatus) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipment = shipment,
        StatusChanged = true,
        PreviousStatus = previousStatus
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static PollShipmentStatusResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static PollShipmentStatusResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of cancelling a shipment.
/// </summary>
public class CancelShipmentResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static CancelShipmentResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static CancelShipmentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CancelShipmentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CancelShipmentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting tracking history.
/// </summary>
public class GetTrackingHistoryResult
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
    /// Gets the shipment.
    /// </summary>
    public Shipment? Shipment { get; private init; }

    /// <summary>
    /// Gets the status updates.
    /// </summary>
    public IReadOnlyList<ShipmentStatusUpdate> StatusUpdates { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="shipment">The shipment.</param>
    /// <param name="statusUpdates">The status updates.</param>
    /// <returns>A successful result.</returns>
    public static GetTrackingHistoryResult Success(Shipment shipment, IReadOnlyList<ShipmentStatusUpdate> statusUpdates) => new()
    {
        Succeeded = true,
        Errors = [],
        Shipment = shipment,
        StatusUpdates = statusUpdates
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetTrackingHistoryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetTrackingHistoryResult Failure(string error) => Failure([error]);
}
