namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Result of updating a cart item quantity.
/// </summary>
public class UpdateCartItemQuantityResult
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
    /// Gets a value indicating whether the request was not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the available stock for the product when validation fails due to insufficient stock.
    /// </summary>
    public int? AvailableStock { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static UpdateCartItemQuantityResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateCartItemQuantityResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateCartItemQuantityResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a failed result for insufficient stock.
    /// </summary>
    /// <param name="availableStock">The current available stock.</param>
    /// <returns>A failed result with stock information.</returns>
    public static UpdateCartItemQuantityResult InsufficientStock(int availableStock) => new()
    {
        Succeeded = false,
        AvailableStock = availableStock,
        Errors = [$"Requested quantity exceeds available stock. Only {availableStock} item(s) available."]
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateCartItemQuantityResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["You are not authorized to modify this cart item."]
    };
}
