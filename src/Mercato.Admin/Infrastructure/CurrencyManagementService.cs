using System.Text.Json;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing currencies from the admin panel.
/// </summary>
public class CurrencyManagementService : ICurrencyManagementService
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ICurrencyHistoryRepository _historyRepository;
    private readonly ILogger<CurrencyManagementService> _logger;

    private const string ConfirmationCodeValue = "CONFIRM_BASE_CURRENCY_CHANGE";

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyManagementService"/> class.
    /// </summary>
    /// <param name="currencyRepository">The currency repository.</param>
    /// <param name="historyRepository">The currency history repository.</param>
    /// <param name="logger">The logger.</param>
    public CurrencyManagementService(
        ICurrencyRepository currencyRepository,
        ICurrencyHistoryRepository historyRepository,
        ILogger<CurrencyManagementService> logger)
    {
        ArgumentNullException.ThrowIfNull(currencyRepository);
        ArgumentNullException.ThrowIfNull(historyRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _currencyRepository = currencyRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetCurrenciesResult> GetAllCurrenciesAsync()
    {
        var currencies = await _currencyRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} currencies", currencies.Count);

        return GetCurrenciesResult.Success(currencies);
    }

    /// <inheritdoc />
    public async Task<GetCurrencyResult> GetCurrencyByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetCurrencyResult.Failure("Currency ID is required.");
        }

        var currency = await _currencyRepository.GetByIdAsync(id);

        if (currency == null)
        {
            return GetCurrencyResult.Failure("Currency not found.");
        }

        return GetCurrencyResult.Success(currency);
    }

    /// <inheritdoc />
    public async Task<CreateCurrencyResult> CreateCurrencyAsync(CreateCurrencyCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateCurrencyResult.Failure(validationErrors);
        }

        var normalizedCode = command.Code.ToUpperInvariant();

        // Check if currency code already exists
        var existingCurrency = await _currencyRepository.GetByCodeAsync(normalizedCode);
        if (existingCurrency != null)
        {
            return CreateCurrencyResult.Failure($"Currency with code '{normalizedCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var currency = new Currency
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = command.Name,
            Symbol = command.Symbol,
            DecimalPlaces = command.DecimalPlaces,
            IsBaseCurrency = false,
            IsEnabled = command.IsEnabled,
            ExchangeRateToBase = null,
            ExchangeRateSource = null,
            ExchangeRateUpdatedAt = null,
            CreatedAt = now,
            CreatedByUserId = command.CreatedByUserId
        };

        await _currencyRepository.AddAsync(currency);

        // Create history record
        var history = new CurrencyHistory
        {
            Id = Guid.NewGuid(),
            CurrencyId = currency.Id,
            ChangeType = "Created",
            PreviousValues = null,
            NewValues = SerializeCurrency(currency),
            ChangedAt = now,
            ChangedByUserId = command.CreatedByUserId,
            ChangedByUserEmail = command.CreatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Created currency '{Code}' ({Name}) by user {UserId}",
            currency.Code,
            currency.Name,
            command.CreatedByUserId);

        return CreateCurrencyResult.Success(currency);
    }

    /// <inheritdoc />
    public async Task<UpdateCurrencyResult> UpdateCurrencyAsync(UpdateCurrencyCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateCurrencyResult.Failure(validationErrors);
        }

        var existingCurrency = await _currencyRepository.GetByIdAsync(command.Id);
        if (existingCurrency == null)
        {
            return UpdateCurrencyResult.Failure("Currency not found.");
        }

        var previousValues = SerializeCurrency(existingCurrency);
        var now = DateTimeOffset.UtcNow;

        existingCurrency.Name = command.Name;
        existingCurrency.Symbol = command.Symbol;
        existingCurrency.DecimalPlaces = command.DecimalPlaces;
        existingCurrency.UpdatedAt = now;
        existingCurrency.UpdatedByUserId = command.UpdatedByUserId;

        await _currencyRepository.UpdateAsync(existingCurrency);

        // Create history record
        var history = new CurrencyHistory
        {
            Id = Guid.NewGuid(),
            CurrencyId = existingCurrency.Id,
            ChangeType = "Updated",
            PreviousValues = previousValues,
            NewValues = SerializeCurrency(existingCurrency),
            ChangedAt = now,
            ChangedByUserId = command.UpdatedByUserId,
            ChangedByUserEmail = command.UpdatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Updated currency '{Code}' ({Name}) by user {UserId}",
            existingCurrency.Code,
            existingCurrency.Name,
            command.UpdatedByUserId);

        return UpdateCurrencyResult.Success(existingCurrency);
    }

    /// <inheritdoc />
    public async Task<EnableCurrencyResult> EnableCurrencyAsync(Guid id, string userId, string? userEmail = null)
    {
        if (id == Guid.Empty)
        {
            return EnableCurrencyResult.Failure("Currency ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return EnableCurrencyResult.Failure("User ID is required.");
        }

        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
        {
            return EnableCurrencyResult.Failure("Currency not found.");
        }

        if (currency.IsEnabled)
        {
            return EnableCurrencyResult.Failure("Currency is already enabled.");
        }

        var previousValues = SerializeCurrency(currency);
        var now = DateTimeOffset.UtcNow;

        currency.IsEnabled = true;
        currency.UpdatedAt = now;
        currency.UpdatedByUserId = userId;

        await _currencyRepository.UpdateAsync(currency);

        // Create history record
        var history = new CurrencyHistory
        {
            Id = Guid.NewGuid(),
            CurrencyId = currency.Id,
            ChangeType = "Enabled",
            PreviousValues = previousValues,
            NewValues = SerializeCurrency(currency),
            ChangedAt = now,
            ChangedByUserId = userId,
            ChangedByUserEmail = userEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Enabled currency '{Code}' by user {UserId}",
            currency.Code,
            userId);

        return EnableCurrencyResult.Success(currency);
    }

    /// <inheritdoc />
    public async Task<DisableCurrencyResult> DisableCurrencyAsync(Guid id, string userId, string? userEmail = null, string? reason = null)
    {
        if (id == Guid.Empty)
        {
            return DisableCurrencyResult.Failure("Currency ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return DisableCurrencyResult.Failure("User ID is required.");
        }

        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
        {
            return DisableCurrencyResult.Failure("Currency not found.");
        }

        if (!currency.IsEnabled)
        {
            return DisableCurrencyResult.Failure("Currency is already disabled.");
        }

        if (currency.IsBaseCurrency)
        {
            return DisableCurrencyResult.Failure("Cannot disable the base currency. Please set another currency as base first.");
        }

        var previousValues = SerializeCurrency(currency);
        var now = DateTimeOffset.UtcNow;

        currency.IsEnabled = false;
        currency.UpdatedAt = now;
        currency.UpdatedByUserId = userId;

        await _currencyRepository.UpdateAsync(currency);

        // Create history record
        var history = new CurrencyHistory
        {
            Id = Guid.NewGuid(),
            CurrencyId = currency.Id,
            ChangeType = "Disabled",
            PreviousValues = previousValues,
            NewValues = SerializeCurrency(currency),
            ChangedAt = now,
            ChangedByUserId = userId,
            ChangedByUserEmail = userEmail,
            Reason = reason
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Disabled currency '{Code}' by user {UserId}. Reason: {Reason}",
            currency.Code,
            userId,
            reason ?? "No reason provided");

        return DisableCurrencyResult.Success(currency);
    }

    /// <inheritdoc />
    public async Task<SetBaseCurrencyResult> SetBaseCurrencyAsync(Guid id, string userId, string? userEmail = null, string? confirmationCode = null)
    {
        if (id == Guid.Empty)
        {
            return SetBaseCurrencyResult.Failure("Currency ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return SetBaseCurrencyResult.Failure("User ID is required.");
        }

        var currency = await _currencyRepository.GetByIdAsync(id);
        if (currency == null)
        {
            return SetBaseCurrencyResult.Failure("Currency not found.");
        }

        if (currency.IsBaseCurrency)
        {
            return SetBaseCurrencyResult.Failure("This currency is already the base currency.");
        }

        if (!currency.IsEnabled)
        {
            return SetBaseCurrencyResult.Failure("Cannot set a disabled currency as base currency. Please enable it first.");
        }

        var currentBaseCurrency = await _currencyRepository.GetBaseCurrencyAsync();

        // Require confirmation for this significant operation
        if (confirmationCode != ConfirmationCodeValue)
        {
            var warningMessage = currentBaseCurrency != null
                ? $"WARNING: Changing the base currency from '{currentBaseCurrency.Code}' to '{currency.Code}' will affect all exchange rate calculations. This is a significant operation that may impact existing listings and transactions. All exchange rates will need to be recalculated relative to the new base currency."
                : $"WARNING: Setting '{currency.Code}' as the base currency is a significant operation that will affect all exchange rate calculations.";

            return SetBaseCurrencyResult.ConfirmationRequired(warningMessage);
        }

        var now = DateTimeOffset.UtcNow;

        // Remove base currency flag from current base currency
        if (currentBaseCurrency != null)
        {
            var previousBaseCurrencyValues = SerializeCurrency(currentBaseCurrency);

            currentBaseCurrency.IsBaseCurrency = false;
            currentBaseCurrency.UpdatedAt = now;
            currentBaseCurrency.UpdatedByUserId = userId;

            await _currencyRepository.UpdateAsync(currentBaseCurrency);

            // Create history record for previous base currency
            var previousBaseHistory = new CurrencyHistory
            {
                Id = Guid.NewGuid(),
                CurrencyId = currentBaseCurrency.Id,
                ChangeType = "Updated",
                PreviousValues = previousBaseCurrencyValues,
                NewValues = SerializeCurrency(currentBaseCurrency),
                ChangedAt = now,
                ChangedByUserId = userId,
                ChangedByUserEmail = userEmail,
                Reason = $"Base currency changed to {currency.Code}"
            };

            await _historyRepository.AddAsync(previousBaseHistory);
        }

        // Set new base currency
        var previousValues = SerializeCurrency(currency);

        currency.IsBaseCurrency = true;
        currency.ExchangeRateToBase = null; // Base currency has no exchange rate
        currency.ExchangeRateSource = null;
        currency.ExchangeRateUpdatedAt = null;
        currency.UpdatedAt = now;
        currency.UpdatedByUserId = userId;

        await _currencyRepository.UpdateAsync(currency);

        // Create history record
        var history = new CurrencyHistory
        {
            Id = Guid.NewGuid(),
            CurrencyId = currency.Id,
            ChangeType = "SetAsBase",
            PreviousValues = previousValues,
            NewValues = SerializeCurrency(currency),
            ChangedAt = now,
            ChangedByUserId = userId,
            ChangedByUserEmail = userEmail,
            Reason = currentBaseCurrency != null
                ? $"Changed from {currentBaseCurrency.Code} to {currency.Code}"
                : "Set as initial base currency"
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Set currency '{Code}' as base currency by user {UserId}. Previous base: {PreviousBase}",
            currency.Code,
            userId,
            currentBaseCurrency?.Code ?? "None");

        return SetBaseCurrencyResult.Success(currency, currentBaseCurrency);
    }

    /// <inheritdoc />
    public async Task<GetCurrencyHistoryResult> GetCurrencyHistoryAsync(Guid currencyId)
    {
        if (currencyId == Guid.Empty)
        {
            return GetCurrencyHistoryResult.Failure("Currency ID is required.");
        }

        var currency = await _currencyRepository.GetByIdAsync(currencyId);
        var history = await _historyRepository.GetByCurrencyIdAsync(currencyId);

        _logger.LogInformation(
            "Retrieved {Count} history records for currency {CurrencyId}",
            history.Count,
            currencyId);

        return GetCurrencyHistoryResult.Success(history, currency);
    }

    private static List<string> ValidateCreateCommand(CreateCurrencyCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            errors.Add("Currency code is required.");
        }
        else if (command.Code.Length != 3)
        {
            errors.Add("Currency code must be exactly 3 characters (ISO 4217).");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Currency name is required.");
        }
        else if (command.Name.Length > 100)
        {
            errors.Add("Currency name must not exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Symbol))
        {
            errors.Add("Currency symbol is required.");
        }
        else if (command.Symbol.Length > 5)
        {
            errors.Add("Currency symbol must not exceed 5 characters.");
        }

        if (command.DecimalPlaces < 0 || command.DecimalPlaces > 8)
        {
            errors.Add("Decimal places must be between 0 and 8.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateCurrencyCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Currency ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Currency name is required.");
        }
        else if (command.Name.Length > 100)
        {
            errors.Add("Currency name must not exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Symbol))
        {
            errors.Add("Currency symbol is required.");
        }
        else if (command.Symbol.Length > 5)
        {
            errors.Add("Currency symbol must not exceed 5 characters.");
        }

        if (command.DecimalPlaces < 0 || command.DecimalPlaces > 8)
        {
            errors.Add("Decimal places must be between 0 and 8.");
        }

        if (string.IsNullOrWhiteSpace(command.UpdatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static string SerializeCurrency(Currency currency)
    {
        var data = new
        {
            currency.Id,
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.DecimalPlaces,
            currency.IsBaseCurrency,
            currency.IsEnabled,
            currency.ExchangeRateToBase,
            currency.ExchangeRateSource,
            currency.ExchangeRateUpdatedAt,
            currency.CreatedAt,
            currency.CreatedByUserId,
            currency.UpdatedAt,
            currency.UpdatedByUserId
        };

        return JsonSerializer.Serialize(data);
    }
}
