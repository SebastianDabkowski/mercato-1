using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Application.Services;

/// <summary>
/// Service interface for managing shipping providers.
/// </summary>
public interface IShippingProviderService
{
    /// <summary>
    /// Gets all available shipping providers on the platform.
    /// </summary>
    /// <returns>The result containing available shipping providers.</returns>
    Task<GetShippingProvidersResult> GetProvidersAsync();

    /// <summary>
    /// Gets a shipping provider by its identifier.
    /// </summary>
    /// <param name="providerId">The shipping provider identifier.</param>
    /// <returns>The result containing the shipping provider.</returns>
    Task<GetShippingProviderResult> GetProviderByIdAsync(Guid providerId);

    /// <summary>
    /// Gets all shipping providers enabled for a specific store.
    /// </summary>
    /// <param name="storeId">The store identifier.</param>
    /// <returns>The result containing the store's enabled shipping providers.</returns>
    Task<GetStoreShippingProvidersResult> GetProvidersForStoreAsync(Guid storeId);

    /// <summary>
    /// Enables a shipping provider for a store.
    /// </summary>
    /// <param name="command">The enable provider command.</param>
    /// <returns>The result of the enable operation.</returns>
    Task<EnableProviderForStoreResult> EnableProviderForStoreAsync(EnableProviderForStoreCommand command);

    /// <summary>
    /// Disables a shipping provider for a store.
    /// </summary>
    /// <param name="storeId">The store identifier.</param>
    /// <param name="shippingProviderId">The shipping provider identifier.</param>
    /// <returns>The result of the disable operation.</returns>
    Task<DisableProviderForStoreResult> DisableProviderForStoreAsync(Guid storeId, Guid shippingProviderId);

    /// <summary>
    /// Updates the configuration for a store's shipping provider.
    /// </summary>
    /// <param name="command">The update configuration command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateStoreProviderConfigResult> UpdateStoreProviderConfigAsync(UpdateStoreProviderConfigCommand command);
}

/// <summary>
/// Result of getting available shipping providers.
/// </summary>
public class GetShippingProvidersResult
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
    /// Gets the available shipping providers.
    /// </summary>
    public IReadOnlyList<ShippingProvider> Providers { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="providers">The shipping providers.</param>
    /// <returns>A successful result.</returns>
    public static GetShippingProvidersResult Success(IReadOnlyList<ShippingProvider> providers) => new()
    {
        Succeeded = true,
        Errors = [],
        Providers = providers
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingProvidersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingProvidersResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of getting a shipping provider by ID.
/// </summary>
public class GetShippingProviderResult
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
    /// Gets the shipping provider.
    /// </summary>
    public ShippingProvider? Provider { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="provider">The shipping provider.</param>
    /// <returns>A successful result.</returns>
    public static GetShippingProviderResult Success(ShippingProvider provider) => new()
    {
        Succeeded = true,
        Errors = [],
        Provider = provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingProviderResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingProviderResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of getting shipping providers for a store.
/// </summary>
public class GetStoreShippingProvidersResult
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
    /// Gets the store's shipping providers.
    /// </summary>
    public IReadOnlyList<StoreShippingProvider> StoreProviders { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="storeProviders">The store's shipping providers.</param>
    /// <returns>A successful result.</returns>
    public static GetStoreShippingProvidersResult Success(IReadOnlyList<StoreShippingProvider> storeProviders) => new()
    {
        Succeeded = true,
        Errors = [],
        StoreProviders = storeProviders
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetStoreShippingProvidersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetStoreShippingProvidersResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetStoreShippingProvidersResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to enable a shipping provider for a store.
/// </summary>
public class EnableProviderForStoreCommand
{
    /// <summary>
    /// Gets or sets the store identifier.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the shipping provider identifier.
    /// </summary>
    public Guid ShippingProviderId { get; set; }

    /// <summary>
    /// Gets or sets the credential identifier for secure credential reference.
    /// </summary>
    public string? CredentialIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the seller's account number with the provider.
    /// </summary>
    public string? AccountNumber { get; set; }
}

/// <summary>
/// Result of enabling a shipping provider for a store.
/// </summary>
public class EnableProviderForStoreResult
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
    /// Gets the store shipping provider configuration.
    /// </summary>
    public StoreShippingProvider? StoreProvider { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="storeProvider">The store shipping provider.</param>
    /// <returns>A successful result.</returns>
    public static EnableProviderForStoreResult Success(StoreShippingProvider storeProvider) => new()
    {
        Succeeded = true,
        Errors = [],
        StoreProvider = storeProvider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static EnableProviderForStoreResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static EnableProviderForStoreResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static EnableProviderForStoreResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of disabling a shipping provider for a store.
/// </summary>
public class DisableProviderForStoreResult
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
    public static DisableProviderForStoreResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static DisableProviderForStoreResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DisableProviderForStoreResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DisableProviderForStoreResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update a store's shipping provider configuration.
/// </summary>
public class UpdateStoreProviderConfigCommand
{
    /// <summary>
    /// Gets or sets the store identifier.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the shipping provider identifier.
    /// </summary>
    public Guid ShippingProviderId { get; set; }

    /// <summary>
    /// Gets or sets the credential identifier for secure credential reference.
    /// </summary>
    public string? CredentialIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the seller's account number with the provider.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Result of updating a store's shipping provider configuration.
/// </summary>
public class UpdateStoreProviderConfigResult
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
    /// Gets the updated store shipping provider configuration.
    /// </summary>
    public StoreShippingProvider? StoreProvider { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="storeProvider">The updated store shipping provider.</param>
    /// <returns>A successful result.</returns>
    public static UpdateStoreProviderConfigResult Success(StoreShippingProvider storeProvider) => new()
    {
        Succeeded = true,
        Errors = [],
        StoreProvider = storeProvider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateStoreProviderConfigResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateStoreProviderConfigResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateStoreProviderConfigResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
