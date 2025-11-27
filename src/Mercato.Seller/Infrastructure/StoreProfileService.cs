using System.ComponentModel.DataAnnotations;
using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Implementation of store profile management service.
/// </summary>
public class StoreProfileService : IStoreProfileService
{
    private readonly IStoreRepository _repository;
    private readonly ILogger<StoreProfileService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreProfileService"/> class.
    /// </summary>
    /// <param name="repository">The store repository.</param>
    /// <param name="logger">The logger.</param>
    public StoreProfileService(
        IStoreRepository repository,
        ILogger<StoreProfileService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Store?> GetStoreBySellerIdAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);
        return await _repository.GetBySellerIdAsync(sellerId);
    }

    /// <inheritdoc />
    public async Task<Store?> GetStoreByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<UpdateStoreProfileResult> UpdateStoreProfileAsync(UpdateStoreProfileCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var store = await _repository.GetBySellerIdAsync(command.SellerId);
        if (store == null)
        {
            return UpdateStoreProfileResult.Failure("Store not found. Please create a store first.");
        }

        var validationErrors = ValidateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateStoreProfileResult.Failure(validationErrors);
        }

        // Check store name uniqueness (excluding current seller)
        if (!await _repository.IsStoreNameUniqueAsync(command.Name, command.SellerId))
        {
            return UpdateStoreProfileResult.Failure("A store with this name already exists.");
        }

        // Update the store
        UpdateStoreProperties(store, command);

        await _repository.UpdateAsync(store);
        _logger.LogInformation("Updated store profile for seller {SellerId}", command.SellerId);

        return UpdateStoreProfileResult.Success();
    }

    /// <inheritdoc />
    public async Task<UpdateStoreProfileResult> CreateOrUpdateStoreProfileAsync(UpdateStoreProfileCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateStoreProfileResult.Failure(validationErrors);
        }

        var existingStore = await _repository.GetBySellerIdAsync(command.SellerId);
        
        if (existingStore != null)
        {
            // Check store name uniqueness (excluding current seller)
            if (!await _repository.IsStoreNameUniqueAsync(command.Name, command.SellerId))
            {
                return UpdateStoreProfileResult.Failure("A store with this name already exists.");
            }

            // Update existing store
            UpdateStoreProperties(existingStore, command);

            await _repository.UpdateAsync(existingStore);
            _logger.LogInformation("Updated store profile for seller {SellerId}", command.SellerId);
        }
        else
        {
            // Check store name uniqueness for new store
            if (!await _repository.IsStoreNameUniqueAsync(command.Name))
            {
                return UpdateStoreProfileResult.Failure("A store with this name already exists.");
            }

            // Create new store
            var newStore = new Store
            {
                Id = Guid.NewGuid(),
                SellerId = command.SellerId,
                Name = command.Name,
                Description = command.Description,
                LogoUrl = command.LogoUrl,
                ContactEmail = command.ContactEmail,
                ContactPhone = command.ContactPhone,
                WebsiteUrl = command.WebsiteUrl,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            await _repository.CreateAsync(newStore);
            _logger.LogInformation("Created new store for seller {SellerId}", command.SellerId);
        }

        return UpdateStoreProfileResult.Success();
    }

    /// <summary>
    /// Updates the store properties from the command.
    /// </summary>
    /// <param name="store">The store to update.</param>
    /// <param name="command">The command containing the new values.</param>
    private static void UpdateStoreProperties(Store store, UpdateStoreProfileCommand command)
    {
        store.Name = command.Name;
        store.Description = command.Description;
        store.LogoUrl = command.LogoUrl;
        store.ContactEmail = command.ContactEmail;
        store.ContactPhone = command.ContactPhone;
        store.WebsiteUrl = command.WebsiteUrl;
        store.LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Validates the update store profile command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateCommand(UpdateStoreProfileCommand command)
    {
        var errors = new List<string>();

        // Validate SellerId
        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        // Validate Name (required)
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Store name is required.");
        }
        else if (command.Name.Length < 2 || command.Name.Length > 200)
        {
            errors.Add("Store name must be between 2 and 200 characters.");
        }

        // Validate Description (optional)
        if (!string.IsNullOrEmpty(command.Description) && command.Description.Length > 2000)
        {
            errors.Add("Store description must be at most 2000 characters.");
        }

        // Validate LogoUrl (optional, URL format)
        if (!string.IsNullOrEmpty(command.LogoUrl))
        {
            if (command.LogoUrl.Length > 500)
            {
                errors.Add("Store logo URL must be at most 500 characters.");
            }
            else if (!IsValidUrl(command.LogoUrl))
            {
                errors.Add("Please enter a valid URL for the logo.");
            }
        }

        // Validate ContactEmail (optional, email format)
        if (!string.IsNullOrEmpty(command.ContactEmail))
        {
            if (command.ContactEmail.Length > 254)
            {
                errors.Add("Contact email must be at most 254 characters.");
            }
            else if (!IsValidEmail(command.ContactEmail))
            {
                errors.Add("Please enter a valid email address.");
            }
        }

        // Validate ContactPhone (optional, basic length check)
        if (!string.IsNullOrEmpty(command.ContactPhone) && command.ContactPhone.Length > 20)
        {
            errors.Add("Contact phone must be at most 20 characters.");
        }

        // Validate WebsiteUrl (optional, URL format)
        if (!string.IsNullOrEmpty(command.WebsiteUrl))
        {
            if (command.WebsiteUrl.Length > 500)
            {
                errors.Add("Website URL must be at most 500 characters.");
            }
            else if (!IsValidUrl(command.WebsiteUrl))
            {
                errors.Add("Please enter a valid website URL.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Validates if the given string is a valid URL.
    /// </summary>
    /// <param name="url">The URL string to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates if the given string is a valid email address using the EmailAddressAttribute.
    /// </summary>
    /// <param name="email">The email string to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidEmail(string email)
    {
        var emailAttribute = new EmailAddressAttribute();
        return emailAttribute.IsValid(email);
    }
}
