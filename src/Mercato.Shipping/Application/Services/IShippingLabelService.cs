using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Application.Services;

/// <summary>
/// Service interface for shipping label operations.
/// </summary>
public interface IShippingLabelService
{
    /// <summary>
    /// Generates a shipping label for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <param name="storeId">The store identifier for authorization.</param>
    /// <returns>The result containing the generated label.</returns>
    Task<GenerateLabelResult> GenerateLabelAsync(Guid shipmentId, Guid storeId);

    /// <summary>
    /// Gets an existing shipping label for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <param name="storeId">The store identifier for authorization.</param>
    /// <returns>The result containing the label.</returns>
    Task<GetLabelResult> GetLabelAsync(Guid shipmentId, Guid storeId);
}

/// <summary>
/// Result of generating a shipping label.
/// </summary>
public class GenerateLabelResult
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
    /// Gets a value indicating whether the operation failed due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the generated shipping label.
    /// </summary>
    public ShippingLabel? Label { get; private init; }

    /// <summary>
    /// Creates a successful result with the generated label.
    /// </summary>
    /// <param name="label">The generated shipping label.</param>
    /// <returns>A successful result.</returns>
    public static GenerateLabelResult Success(ShippingLabel label) => new()
    {
        Succeeded = true,
        Errors = [],
        Label = label
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GenerateLabelResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GenerateLabelResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GenerateLabelResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a shipping label.
/// </summary>
public class GetLabelResult
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
    /// Gets a value indicating whether the operation failed due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the shipping label.
    /// </summary>
    public ShippingLabel? Label { get; private init; }

    /// <summary>
    /// Creates a successful result with the label.
    /// </summary>
    /// <param name="label">The shipping label.</param>
    /// <returns>A successful result.</returns>
    public static GetLabelResult Success(ShippingLabel label) => new()
    {
        Succeeded = true,
        Errors = [],
        Label = label
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetLabelResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetLabelResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetLabelResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
