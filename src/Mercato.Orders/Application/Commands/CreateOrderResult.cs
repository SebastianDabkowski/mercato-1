using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Result of creating a new order.
/// </summary>
public class CreateOrderResult
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
    /// Gets the created order ID.
    /// </summary>
    public Guid? OrderId { get; private init; }

    /// <summary>
    /// Gets the generated order number.
    /// </summary>
    public string? OrderNumber { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="orderNumber">The order number.</param>
    /// <returns>A successful result.</returns>
    public static CreateOrderResult Success(Guid orderId, string orderNumber) => new()
    {
        Succeeded = true,
        Errors = [],
        OrderId = orderId,
        OrderNumber = orderNumber
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateOrderResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateOrderResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateOrderResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to perform this operation."]
    };
}

/// <summary>
/// Result of getting an order.
/// </summary>
public class GetOrderResult
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
    /// Gets the order if found.
    /// </summary>
    public Order? Order { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns>A successful result.</returns>
    public static GetOrderResult Success(Order order) => new()
    {
        Succeeded = true,
        Errors = [],
        Order = order
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetOrderResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetOrderResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetOrderResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this order."]
    };
}

/// <summary>
/// Result of updating order status.
/// </summary>
public class UpdateOrderStatusResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static UpdateOrderStatusResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateOrderStatusResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateOrderStatusResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of getting orders for a buyer.
/// </summary>
public class GetOrdersResult
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
    /// Gets the list of orders.
    /// </summary>
    public IReadOnlyList<Order> Orders { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="orders">The orders.</param>
    /// <returns>A successful result.</returns>
    public static GetOrdersResult Success(IReadOnlyList<Order> orders) => new()
    {
        Succeeded = true,
        Errors = [],
        Orders = orders
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetOrdersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetOrdersResult Failure(string error) => Failure([error]);
}
