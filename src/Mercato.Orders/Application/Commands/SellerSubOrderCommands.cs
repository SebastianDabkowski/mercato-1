using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Command for updating the status of a seller sub-order.
/// </summary>
public class UpdateSellerSubOrderStatusCommand
{
    /// <summary>
    /// Gets or sets the new status for the sub-order.
    /// </summary>
    public SellerSubOrderStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the tracking number (optional, for shipped status).
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping carrier (optional, for shipped status).
    /// </summary>
    public string? ShippingCarrier { get; set; }
}

/// <summary>
/// Result of getting seller sub-orders.
/// </summary>
public class GetSellerSubOrdersResult
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
    /// Gets the list of seller sub-orders.
    /// </summary>
    public IReadOnlyList<SellerSubOrder> SellerSubOrders { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="sellerSubOrders">The seller sub-orders.</param>
    /// <returns>A successful result.</returns>
    public static GetSellerSubOrdersResult Success(IReadOnlyList<SellerSubOrder> sellerSubOrders) => new()
    {
        Succeeded = true,
        Errors = [],
        SellerSubOrders = sellerSubOrders
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerSubOrdersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerSubOrdersResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetSellerSubOrdersResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access these sub-orders."]
    };
}

/// <summary>
/// Result of getting a single seller sub-order.
/// </summary>
public class GetSellerSubOrderResult
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
    /// Gets the seller sub-order if found.
    /// </summary>
    public SellerSubOrder? SellerSubOrder { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="sellerSubOrder">The seller sub-order.</param>
    /// <returns>A successful result.</returns>
    public static GetSellerSubOrderResult Success(SellerSubOrder sellerSubOrder) => new()
    {
        Succeeded = true,
        Errors = [],
        SellerSubOrder = sellerSubOrder
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerSubOrderResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerSubOrderResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetSellerSubOrderResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this sub-order."]
    };
}

/// <summary>
/// Command for updating the tracking information of a seller sub-order.
/// </summary>
public class UpdateTrackingInfoCommand
{
    /// <summary>
    /// Gets or sets the tracking number.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping carrier.
    /// </summary>
    public string? ShippingCarrier { get; set; }
}

/// <summary>
/// Result of updating tracking information.
/// </summary>
public class UpdateTrackingInfoResult
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
    public static UpdateTrackingInfoResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateTrackingInfoResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateTrackingInfoResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateTrackingInfoResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to update this sub-order."]
    };
}

/// <summary>
/// Result of updating seller sub-order status.
/// </summary>
public class UpdateSellerSubOrderStatusResult
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
    public static UpdateSellerSubOrderStatusResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateSellerSubOrderStatusResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateSellerSubOrderStatusResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateSellerSubOrderStatusResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to update this sub-order."]
    };
}
