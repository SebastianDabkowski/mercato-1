using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Buyer.Infrastructure;

/// <summary>
/// Service implementation for delivery address operations.
/// </summary>
public class DeliveryAddressService : IDeliveryAddressService
{
    private readonly IDeliveryAddressRepository _repository;
    private readonly ILogger<DeliveryAddressService> _logger;

    /// <summary>
    /// Allowed shipping countries (ISO codes).
    /// </summary>
    private static readonly HashSet<string> AllowedCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", // United States
        "CA", // Canada
        "GB", // United Kingdom
        "DE", // Germany
        "FR", // France
        "IT", // Italy
        "ES", // Spain
        "NL", // Netherlands
        "PL"  // Poland
    };

    /// <summary>
    /// Cached read-only list of allowed shipping countries.
    /// </summary>
    private static readonly IReadOnlyList<string> AllowedCountriesList = ["US", "CA", "GB", "DE", "FR", "IT", "ES", "NL", "PL"];

    /// <inheritdoc />
    public IReadOnlyList<string> AllowedShippingCountries => AllowedCountriesList;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryAddressService"/> class.
    /// </summary>
    /// <param name="repository">The delivery address repository.</param>
    /// <param name="logger">The logger.</param>
    public DeliveryAddressService(
        IDeliveryAddressRepository repository,
        ILogger<DeliveryAddressService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetDeliveryAddressesResult> GetAddressesAsync(GetDeliveryAddressesQuery query)
    {
        var validationErrors = ValidateGetAddressesQuery(query);
        if (validationErrors.Count > 0)
        {
            return GetDeliveryAddressesResult.Failure(validationErrors);
        }

        try
        {
            var addresses = await _repository.GetByBuyerIdAsync(query.BuyerId);
            var addressDtos = addresses.Select(MapToDto).ToList();

            _logger.LogDebug("Retrieved {Count} addresses for buyer {BuyerId}", addressDtos.Count, query.BuyerId);

            return GetDeliveryAddressesResult.Success(addressDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses for buyer {BuyerId}", query.BuyerId);
            return GetDeliveryAddressesResult.Failure("An error occurred while retrieving addresses.");
        }
    }

    /// <inheritdoc />
    public async Task<SaveDeliveryAddressResult> SaveAddressAsync(SaveDeliveryAddressCommand command)
    {
        var validationErrors = ValidateSaveAddressCommand(command);
        if (validationErrors.Count > 0)
        {
            return SaveDeliveryAddressResult.Failure(validationErrors);
        }

        // Check if country is allowed for shipping
        if (!IsShippingAllowedToRegion(command.Country))
        {
            return SaveDeliveryAddressResult.RegionNotAllowed(command.Country);
        }

        try
        {
            if (command.AddressId.HasValue)
            {
                return await UpdateExistingAddressAsync(command);
            }
            else
            {
                return await CreateNewAddressAsync(command);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving address for buyer {BuyerId}", command.BuyerId);
            return SaveDeliveryAddressResult.Failure("An error occurred while saving the address.");
        }
    }

    /// <inheritdoc />
    public async Task<DeleteDeliveryAddressResult> DeleteAddressAsync(DeleteDeliveryAddressCommand command)
    {
        var validationErrors = ValidateDeleteAddressCommand(command);
        if (validationErrors.Count > 0)
        {
            return DeleteDeliveryAddressResult.Failure(validationErrors);
        }

        try
        {
            var address = await _repository.GetByIdAsync(command.AddressId);
            if (address == null)
            {
                return DeleteDeliveryAddressResult.Failure("Address not found.");
            }

            // Check authorization
            if (address.BuyerId != command.BuyerId)
            {
                return DeleteDeliveryAddressResult.NotAuthorized();
            }

            await _repository.DeleteAsync(address);

            _logger.LogInformation("Deleted address {AddressId} for buyer {BuyerId}", command.AddressId, command.BuyerId);

            return DeleteDeliveryAddressResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId} for buyer {BuyerId}", command.AddressId, command.BuyerId);
            return DeleteDeliveryAddressResult.Failure("An error occurred while deleting the address.");
        }
    }

    /// <inheritdoc />
    public async Task<SetDefaultDeliveryAddressResult> SetDefaultAddressAsync(SetDefaultDeliveryAddressCommand command)
    {
        var validationErrors = ValidateSetDefaultAddressCommand(command);
        if (validationErrors.Count > 0)
        {
            return SetDefaultDeliveryAddressResult.Failure(validationErrors);
        }

        try
        {
            var address = await _repository.GetByIdAsync(command.AddressId);
            if (address == null)
            {
                return SetDefaultDeliveryAddressResult.Failure("Address not found.");
            }

            // Check authorization
            if (address.BuyerId != command.BuyerId)
            {
                return SetDefaultDeliveryAddressResult.NotAuthorized();
            }

            // If already default, nothing to do
            if (address.IsDefault)
            {
                return SetDefaultDeliveryAddressResult.Success();
            }

            // Clear current default
            var currentDefault = await _repository.GetDefaultByBuyerIdAsync(command.BuyerId);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
                currentDefault.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _repository.UpdateAsync(currentDefault);
            }

            // Set new default
            address.IsDefault = true;
            address.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(address);

            _logger.LogInformation("Set address {AddressId} as default for buyer {BuyerId}", command.AddressId, command.BuyerId);

            return SetDefaultDeliveryAddressResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default address {AddressId} for buyer {BuyerId}", command.AddressId, command.BuyerId);
            return SetDefaultDeliveryAddressResult.Failure("An error occurred while setting the default address.");
        }
    }

    /// <inheritdoc />
    public bool IsShippingAllowedToRegion(string countryCode)
    {
        var normalizedCode = NormalizeCountryCode(countryCode);
        if (string.IsNullOrEmpty(normalizedCode))
        {
            return false;
        }

        return AllowedCountries.Contains(normalizedCode);
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return string.Empty;
        }

        return countryCode.Trim().ToUpperInvariant();
    }

    private async Task<SaveDeliveryAddressResult> UpdateExistingAddressAsync(SaveDeliveryAddressCommand command)
    {
        var address = await _repository.GetByIdAsync(command.AddressId!.Value);
        if (address == null)
        {
            return SaveDeliveryAddressResult.Failure("Address not found.");
        }

        // Check authorization
        if (address.BuyerId != command.BuyerId)
        {
            return SaveDeliveryAddressResult.NotAuthorized();
        }

        // Update address properties
        address.Label = command.Label;
        address.FullName = command.FullName;
        address.AddressLine1 = command.AddressLine1;
        address.AddressLine2 = command.AddressLine2;
        address.City = command.City;
        address.State = command.State;
        address.PostalCode = command.PostalCode;
        address.Country = NormalizeCountryCode(command.Country);
        address.PhoneNumber = command.PhoneNumber;
        address.LastUpdatedAt = DateTimeOffset.UtcNow;

        // Handle default setting
        if (command.SetAsDefault && !address.IsDefault)
        {
            await ClearCurrentDefaultAsync(command.BuyerId);
            address.IsDefault = true;
        }

        await _repository.UpdateAsync(address);

        _logger.LogInformation("Updated address {AddressId} for buyer {BuyerId}", address.Id, command.BuyerId);

        return SaveDeliveryAddressResult.Success(address.Id);
    }

    private async Task<SaveDeliveryAddressResult> CreateNewAddressAsync(SaveDeliveryAddressCommand command)
    {
        // Check if this is the first address (should be default)
        var existingAddresses = await _repository.GetByBuyerIdAsync(command.BuyerId);
        var isFirstAddress = existingAddresses.Count == 0;
        var shouldBeDefault = isFirstAddress || command.SetAsDefault;

        // Clear current default if setting this as default
        if (shouldBeDefault && !isFirstAddress)
        {
            await ClearCurrentDefaultAsync(command.BuyerId);
        }

        var address = new DeliveryAddress
        {
            Id = Guid.NewGuid(),
            BuyerId = command.BuyerId,
            Label = command.Label,
            FullName = command.FullName,
            AddressLine1 = command.AddressLine1,
            AddressLine2 = command.AddressLine2,
            City = command.City,
            State = command.State,
            PostalCode = command.PostalCode,
            Country = NormalizeCountryCode(command.Country),
            PhoneNumber = command.PhoneNumber,
            IsDefault = shouldBeDefault,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(address);

        _logger.LogInformation("Created address {AddressId} for buyer {BuyerId}", address.Id, command.BuyerId);

        return SaveDeliveryAddressResult.Success(address.Id);
    }

    private async Task ClearCurrentDefaultAsync(string buyerId)
    {
        var currentDefault = await _repository.GetDefaultByBuyerIdAsync(buyerId);
        if (currentDefault != null)
        {
            currentDefault.IsDefault = false;
            currentDefault.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(currentDefault);
        }
    }

    private static DeliveryAddressDto MapToDto(DeliveryAddress address)
    {
        var formattedAddress = FormatAddress(address);

        return new DeliveryAddressDto
        {
            Id = address.Id,
            Label = address.Label,
            FullName = address.FullName,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            PhoneNumber = address.PhoneNumber,
            IsDefault = address.IsDefault,
            FormattedAddress = formattedAddress
        };
    }

    private static string FormatAddress(DeliveryAddress address)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(address.AddressLine1))
        {
            parts.Add(address.AddressLine1);
        }

        if (!string.IsNullOrWhiteSpace(address.AddressLine2))
        {
            parts.Add(address.AddressLine2);
        }

        var cityStateParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.City))
        {
            cityStateParts.Add(address.City);
        }
        if (!string.IsNullOrWhiteSpace(address.State))
        {
            cityStateParts.Add(address.State);
        }
        if (!string.IsNullOrWhiteSpace(address.PostalCode))
        {
            cityStateParts.Add(address.PostalCode);
        }

        if (cityStateParts.Count > 0)
        {
            parts.Add(string.Join(", ", cityStateParts));
        }

        if (!string.IsNullOrWhiteSpace(address.Country))
        {
            parts.Add(address.Country);
        }

        return string.Join(", ", parts);
    }

    private static List<string> ValidateGetAddressesQuery(GetDeliveryAddressesQuery query)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(query.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateSaveAddressCommand(SaveDeliveryAddressCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add("Full name is required.");
        }
        else if (command.FullName.Length > 200)
        {
            errors.Add("Full name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.AddressLine1))
        {
            errors.Add("Address line 1 is required.");
        }
        else if (command.AddressLine1.Length > 500)
        {
            errors.Add("Address line 1 must not exceed 500 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.AddressLine2) && command.AddressLine2.Length > 500)
        {
            errors.Add("Address line 2 must not exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.City))
        {
            errors.Add("City is required.");
        }
        else if (command.City.Length > 100)
        {
            errors.Add("City must not exceed 100 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.State) && command.State.Length > 100)
        {
            errors.Add("State must not exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.PostalCode))
        {
            errors.Add("Postal code is required.");
        }
        else if (command.PostalCode.Length > 20)
        {
            errors.Add("Postal code must not exceed 20 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Country))
        {
            errors.Add("Country is required.");
        }
        else if (command.Country.Length != 2)
        {
            errors.Add("Country must be a valid 2-letter ISO country code.");
        }

        if (!string.IsNullOrWhiteSpace(command.Label) && command.Label.Length > 50)
        {
            errors.Add("Label must not exceed 50 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.PhoneNumber) && command.PhoneNumber.Length > 30)
        {
            errors.Add("Phone number must not exceed 30 characters.");
        }

        return errors;
    }

    private static List<string> ValidateDeleteAddressCommand(DeleteDeliveryAddressCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.AddressId == Guid.Empty)
        {
            errors.Add("Address ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateSetDefaultAddressCommand(SetDefaultDeliveryAddressCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.AddressId == Guid.Empty)
        {
            errors.Add("Address ID is required.");
        }

        return errors;
    }
}
