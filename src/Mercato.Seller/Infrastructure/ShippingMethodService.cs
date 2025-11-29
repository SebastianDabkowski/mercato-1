using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Implementation of shipping method management service.
/// </summary>
public class ShippingMethodService : IShippingMethodService
{
    private readonly IShippingMethodRepository _repository;
    private readonly ILogger<ShippingMethodService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodService"/> class.
    /// </summary>
    /// <param name="repository">The shipping method repository.</param>
    /// <param name="logger">The logger.</param>
    public ShippingMethodService(
        IShippingMethodRepository repository,
        ILogger<ShippingMethodService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ShippingMethod?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingMethod>> GetByStoreIdAsync(Guid storeId)
    {
        return await _repository.GetByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingMethod>> GetActiveByStoreIdAsync(Guid storeId)
    {
        return await _repository.GetActiveByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<CreateShippingMethodResult> CreateAsync(CreateShippingMethodCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = ValidateCreateCommand(command);
        if (errors.Count > 0)
        {
            return CreateShippingMethodResult.Failure(errors);
        }

        var shippingMethod = new ShippingMethod
        {
            Id = Guid.NewGuid(),
            StoreId = command.StoreId,
            Name = command.Name,
            Description = command.Description,
            AvailableCountries = NormalizeAvailableCountries(command.AvailableCountries),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(shippingMethod);
        _logger.LogInformation(
            "Created shipping method {ShippingMethodId} '{ShippingMethodName}' for store {StoreId}",
            shippingMethod.Id, shippingMethod.Name, shippingMethod.StoreId);

        return CreateShippingMethodResult.Success(shippingMethod.Id);
    }

    /// <inheritdoc />
    public async Task<UpdateShippingMethodResult> UpdateAsync(UpdateShippingMethodCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingMethod = await _repository.GetByIdAsync(command.Id);
        if (shippingMethod == null)
        {
            return UpdateShippingMethodResult.Failure("Shipping method not found.");
        }

        // Check authorization - verify the shipping method belongs to the specified store
        if (!IsAuthorizedForShippingMethod(shippingMethod, command.StoreId, command.Id, "update"))
        {
            return UpdateShippingMethodResult.NotAuthorized();
        }

        var errors = ValidateUpdateCommand(command);
        if (errors.Count > 0)
        {
            return UpdateShippingMethodResult.Failure(errors);
        }

        shippingMethod.Name = command.Name;
        shippingMethod.Description = command.Description;
        shippingMethod.AvailableCountries = NormalizeAvailableCountries(command.AvailableCountries);
        shippingMethod.IsActive = command.IsActive;
        shippingMethod.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(shippingMethod);
        _logger.LogInformation(
            "Updated shipping method {ShippingMethodId} '{ShippingMethodName}' for store {StoreId}",
            shippingMethod.Id, shippingMethod.Name, shippingMethod.StoreId);

        return UpdateShippingMethodResult.Success();
    }

    /// <inheritdoc />
    public async Task<DeleteShippingMethodResult> DeleteAsync(DeleteShippingMethodCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingMethod = await _repository.GetByIdAsync(command.Id);
        if (shippingMethod == null)
        {
            return DeleteShippingMethodResult.Failure("Shipping method not found.");
        }

        // Check authorization - verify the shipping method belongs to the specified store
        if (!IsAuthorizedForShippingMethod(shippingMethod, command.StoreId, command.Id, "delete"))
        {
            return DeleteShippingMethodResult.NotAuthorized();
        }

        await _repository.DeleteAsync(command.Id);
        _logger.LogInformation(
            "Deleted shipping method {ShippingMethodId} from store {StoreId}",
            command.Id, command.StoreId);

        return DeleteShippingMethodResult.Success();
    }

    /// <summary>
    /// Checks if the specified store is authorized to access the shipping method.
    /// </summary>
    /// <param name="shippingMethod">The shipping method to check.</param>
    /// <param name="requestedStoreId">The store ID from the request.</param>
    /// <param name="shippingMethodId">The shipping method ID (for logging).</param>
    /// <param name="operation">The operation being attempted (for logging).</param>
    /// <returns>True if authorized; otherwise, false.</returns>
    private bool IsAuthorizedForShippingMethod(ShippingMethod shippingMethod, Guid requestedStoreId, Guid shippingMethodId, string operation)
    {
        if (shippingMethod.StoreId == requestedStoreId)
        {
            return true;
        }

        _logger.LogWarning(
            "Unauthorized attempt to {Operation} shipping method {ShippingMethodId} by store {StoreId}",
            operation, shippingMethodId, requestedStoreId);
        return false;
    }

    /// <summary>
    /// Validates the create shipping method command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateCreateCommand(CreateShippingMethodCommand command)
    {
        var errors = new List<string>();

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        ValidateNameAndDescription(command.Name, command.Description, command.AvailableCountries, errors);

        return errors;
    }

    /// <summary>
    /// Validates the update shipping method command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateUpdateCommand(UpdateShippingMethodCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Shipping method ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        ValidateNameAndDescription(command.Name, command.Description, command.AvailableCountries, errors);

        return errors;
    }

    /// <summary>
    /// Validates the name, description, and available countries fields.
    /// </summary>
    /// <param name="name">The shipping method name.</param>
    /// <param name="description">The shipping method description.</param>
    /// <param name="availableCountries">The available countries string.</param>
    /// <param name="errors">The list to add errors to.</param>
    private static void ValidateNameAndDescription(string? name, string? description, string? availableCountries, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Shipping method name is required.");
        }
        else if (name.Length < 2 || name.Length > 100)
        {
            errors.Add("Shipping method name must be between 2 and 100 characters.");
        }

        if (!string.IsNullOrEmpty(description) && description.Length > 500)
        {
            errors.Add("Description must be at most 500 characters.");
        }

        if (!string.IsNullOrEmpty(availableCountries) && availableCountries.Length > 1000)
        {
            errors.Add("Available countries must be at most 1000 characters.");
        }
    }

    /// <summary>
    /// Normalizes the available countries string by trimming whitespace and converting to uppercase.
    /// </summary>
    /// <param name="availableCountries">The raw available countries string.</param>
    /// <returns>The normalized string, or null if empty.</returns>
    private static string? NormalizeAvailableCountries(string? availableCountries)
    {
        if (string.IsNullOrWhiteSpace(availableCountries))
        {
            return null;
        }

        // Split by comma, trim each code, convert to uppercase, filter empty, and rejoin
        var codes = availableCountries
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim().ToUpperInvariant())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c);

        var normalized = string.Join(",", codes);
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
