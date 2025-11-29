using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;

namespace Mercato.Shipping.Infrastructure;

/// <summary>
/// Service implementation for managing shipping providers.
/// </summary>
public class ShippingProviderService : IShippingProviderService
{
    private readonly IShippingProviderRepository _shippingProviderRepository;
    private readonly IStoreShippingProviderRepository _storeShippingProviderRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingProviderService"/> class.
    /// </summary>
    /// <param name="shippingProviderRepository">The shipping provider repository.</param>
    /// <param name="storeShippingProviderRepository">The store shipping provider repository.</param>
    public ShippingProviderService(
        IShippingProviderRepository shippingProviderRepository,
        IStoreShippingProviderRepository storeShippingProviderRepository)
    {
        _shippingProviderRepository = shippingProviderRepository;
        _storeShippingProviderRepository = storeShippingProviderRepository;
    }

    /// <inheritdoc />
    public async Task<GetShippingProvidersResult> GetProvidersAsync()
    {
        var providers = await _shippingProviderRepository.GetActiveProvidersAsync();
        return GetShippingProvidersResult.Success(providers);
    }

    /// <inheritdoc />
    public async Task<GetShippingProviderResult> GetProviderByIdAsync(Guid providerId)
    {
        var errors = ValidateProviderId(providerId);
        if (errors.Count > 0)
        {
            return GetShippingProviderResult.Failure(errors);
        }

        var provider = await _shippingProviderRepository.GetByIdAsync(providerId);
        if (provider == null)
        {
            return GetShippingProviderResult.Failure("Shipping provider not found.");
        }

        return GetShippingProviderResult.Success(provider);
    }

    /// <inheritdoc />
    public async Task<GetStoreShippingProvidersResult> GetProvidersForStoreAsync(Guid storeId)
    {
        var errors = ValidateStoreId(storeId);
        if (errors.Count > 0)
        {
            return GetStoreShippingProvidersResult.Failure(errors);
        }

        var storeProviders = await _storeShippingProviderRepository.GetByStoreIdAsync(storeId);
        return GetStoreShippingProvidersResult.Success(storeProviders);
    }

    /// <inheritdoc />
    public async Task<EnableProviderForStoreResult> EnableProviderForStoreAsync(EnableProviderForStoreCommand command)
    {
        var errors = ValidateEnableProviderCommand(command);
        if (errors.Count > 0)
        {
            return EnableProviderForStoreResult.Failure(errors);
        }

        // Check if provider exists and is active
        var provider = await _shippingProviderRepository.GetByIdAsync(command.ShippingProviderId);
        if (provider == null)
        {
            return EnableProviderForStoreResult.Failure("Shipping provider not found.");
        }

        if (!provider.IsActive)
        {
            return EnableProviderForStoreResult.Failure("Shipping provider is not active.");
        }

        // Check if already exists
        var existing = await _storeShippingProviderRepository.GetByStoreAndProviderAsync(
            command.StoreId, command.ShippingProviderId);

        if (existing != null)
        {
            // Re-enable if disabled
            existing.IsEnabled = true;
            existing.CredentialIdentifier = command.CredentialIdentifier;
            existing.AccountNumber = command.AccountNumber;
            existing.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _storeShippingProviderRepository.UpdateAsync(existing);
            return EnableProviderForStoreResult.Success(existing);
        }

        // Create new store shipping provider
        var storeProvider = new StoreShippingProvider
        {
            Id = Guid.NewGuid(),
            StoreId = command.StoreId,
            ShippingProviderId = command.ShippingProviderId,
            IsEnabled = true,
            CredentialIdentifier = command.CredentialIdentifier,
            AccountNumber = command.AccountNumber,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            ShippingProvider = provider
        };

        await _storeShippingProviderRepository.AddAsync(storeProvider);
        return EnableProviderForStoreResult.Success(storeProvider);
    }

    /// <inheritdoc />
    public async Task<DisableProviderForStoreResult> DisableProviderForStoreAsync(Guid storeId, Guid shippingProviderId)
    {
        var errors = new List<string>();
        errors.AddRange(ValidateStoreId(storeId));
        errors.AddRange(ValidateProviderId(shippingProviderId));

        if (errors.Count > 0)
        {
            return DisableProviderForStoreResult.Failure(errors);
        }

        var storeProvider = await _storeShippingProviderRepository.GetByStoreAndProviderAsync(storeId, shippingProviderId);
        if (storeProvider == null)
        {
            return DisableProviderForStoreResult.Failure("Store shipping provider configuration not found.");
        }

        storeProvider.IsEnabled = false;
        storeProvider.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _storeShippingProviderRepository.UpdateAsync(storeProvider);
        return DisableProviderForStoreResult.Success();
    }

    /// <inheritdoc />
    public async Task<UpdateStoreProviderConfigResult> UpdateStoreProviderConfigAsync(UpdateStoreProviderConfigCommand command)
    {
        var errors = ValidateUpdateConfigCommand(command);
        if (errors.Count > 0)
        {
            return UpdateStoreProviderConfigResult.Failure(errors);
        }

        var storeProvider = await _storeShippingProviderRepository.GetByStoreAndProviderAsync(
            command.StoreId, command.ShippingProviderId);

        if (storeProvider == null)
        {
            return UpdateStoreProviderConfigResult.Failure("Store shipping provider configuration not found.");
        }

        // Update fields if provided
        if (command.CredentialIdentifier != null)
        {
            storeProvider.CredentialIdentifier = command.CredentialIdentifier;
        }

        if (command.AccountNumber != null)
        {
            storeProvider.AccountNumber = command.AccountNumber;
        }

        if (command.IsEnabled.HasValue)
        {
            storeProvider.IsEnabled = command.IsEnabled.Value;
        }

        storeProvider.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _storeShippingProviderRepository.UpdateAsync(storeProvider);
        return UpdateStoreProviderConfigResult.Success(storeProvider);
    }

    private static List<string> ValidateStoreId(Guid storeId)
    {
        var errors = new List<string>();
        if (storeId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }
        return errors;
    }

    private static List<string> ValidateProviderId(Guid providerId)
    {
        var errors = new List<string>();
        if (providerId == Guid.Empty)
        {
            errors.Add("Shipping provider ID is required.");
        }
        return errors;
    }

    private static List<string> ValidateEnableProviderCommand(EnableProviderForStoreCommand command)
    {
        var errors = new List<string>();
        errors.AddRange(ValidateStoreId(command.StoreId));
        errors.AddRange(ValidateProviderId(command.ShippingProviderId));
        return errors;
    }

    private static List<string> ValidateUpdateConfigCommand(UpdateStoreProviderConfigCommand command)
    {
        var errors = new List<string>();
        errors.AddRange(ValidateStoreId(command.StoreId));
        errors.AddRange(ValidateProviderId(command.ShippingProviderId));
        return errors;
    }
}
