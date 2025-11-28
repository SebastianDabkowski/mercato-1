namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Result of adding an item to the cart.
/// </summary>
public class AddToCartResult
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
    /// Gets a value indicating whether the item already exists in the cart.
    /// When true, the quantity was updated instead of adding a new item.
    /// </summary>
    public bool ItemAlreadyExists { get; private init; }

    /// <summary>
    /// Gets the ID of the cart item that was added or updated.
    /// </summary>
    public Guid? CartItemId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="cartItemId">The ID of the cart item.</param>
    /// <param name="itemAlreadyExists">Whether the item already existed and quantity was updated.</param>
    /// <returns>A successful result.</returns>
    public static AddToCartResult Success(Guid cartItemId, bool itemAlreadyExists = false) => new()
    {
        Succeeded = true,
        Errors = [],
        CartItemId = cartItemId,
        ItemAlreadyExists = itemAlreadyExists
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static AddToCartResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static AddToCartResult Failure(string error) => Failure([error]);
}
