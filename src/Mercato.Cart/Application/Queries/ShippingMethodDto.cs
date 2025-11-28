namespace Mercato.Cart.Application.Queries;

/// <summary>
/// Represents a shipping method option available for a store.
/// </summary>
public class ShippingMethodDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping method.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID this shipping method belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the shipping method.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shipping method.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shipping cost for this method.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery time description.
    /// </summary>
    public string EstimatedDelivery { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is the default method.
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Result of getting available shipping methods.
/// </summary>
public class GetShippingMethodsResult
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
    /// Gets the available shipping methods grouped by store.
    /// </summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<ShippingMethodDto>> MethodsByStore { get; private init; } =
        new Dictionary<Guid, IReadOnlyList<ShippingMethodDto>>();

    /// <summary>
    /// Creates a successful result with shipping methods.
    /// </summary>
    /// <param name="methodsByStore">The shipping methods grouped by store.</param>
    /// <returns>A successful result.</returns>
    public static GetShippingMethodsResult Success(
        IReadOnlyDictionary<Guid, IReadOnlyList<ShippingMethodDto>> methodsByStore) => new()
    {
        Succeeded = true,
        Errors = [],
        MethodsByStore = methodsByStore
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingMethodsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingMethodsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetShippingMethodsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
