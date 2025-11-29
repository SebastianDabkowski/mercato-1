namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of creating a shipping method.
/// </summary>
public class CreateShippingMethodResult
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
    /// Gets the ID of the created shipping method if the operation succeeded.
    /// </summary>
    public Guid? ShippingMethodId { get; private init; }

    /// <summary>
    /// Creates a successful result with the created shipping method ID.
    /// </summary>
    /// <param name="shippingMethodId">The ID of the created shipping method.</param>
    /// <returns>A successful result.</returns>
    public static CreateShippingMethodResult Success(Guid shippingMethodId) => new()
    {
        Succeeded = true,
        Errors = [],
        ShippingMethodId = shippingMethodId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateShippingMethodResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateShippingMethodResult Failure(string error) => Failure([error]);
}
