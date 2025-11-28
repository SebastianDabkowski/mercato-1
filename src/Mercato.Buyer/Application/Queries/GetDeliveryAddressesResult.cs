namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Result containing delivery addresses for a buyer.
/// </summary>
public class GetDeliveryAddressesResult
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
    /// Gets the list of delivery addresses.
    /// </summary>
    public IReadOnlyList<DeliveryAddressDto> Addresses { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with the provided addresses.
    /// </summary>
    /// <param name="addresses">The list of delivery addresses.</param>
    /// <returns>A successful result.</returns>
    public static GetDeliveryAddressesResult Success(IReadOnlyList<DeliveryAddressDto> addresses) => new()
    {
        Succeeded = true,
        Errors = [],
        Addresses = addresses
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetDeliveryAddressesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetDeliveryAddressesResult Failure(string error) => Failure([error]);
}

/// <summary>
/// DTO representing a delivery address.
/// </summary>
public class DeliveryAddressDto
{
    /// <summary>
    /// Gets or sets the delivery address ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the label for the address.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary address line.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary address line.
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
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO country code.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default delivery address.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the formatted address as a single string.
    /// </summary>
    public string FormattedAddress { get; set; } = string.Empty;
}
