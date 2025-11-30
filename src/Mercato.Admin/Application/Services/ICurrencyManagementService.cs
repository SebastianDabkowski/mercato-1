using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing currencies from the admin panel.
/// </summary>
public interface ICurrencyManagementService
{
    /// <summary>
    /// Gets all currencies.
    /// </summary>
    /// <returns>The result containing all currencies.</returns>
    Task<GetCurrenciesResult> GetAllCurrenciesAsync();

    /// <summary>
    /// Gets a specific currency by ID.
    /// </summary>
    /// <param name="id">The currency identifier.</param>
    /// <returns>The result containing the currency if found.</returns>
    Task<GetCurrencyResult> GetCurrencyByIdAsync(Guid id);

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    /// <param name="command">The command containing currency details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateCurrencyResult> CreateCurrencyAsync(CreateCurrencyCommand command);

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    /// <param name="command">The command containing updated currency details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCurrencyResult> UpdateCurrencyAsync(UpdateCurrencyCommand command);

    /// <summary>
    /// Enables a currency for use in listings and transactions.
    /// </summary>
    /// <param name="id">The currency ID to enable.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="userEmail">The email of the user performing the action.</param>
    /// <returns>The result of the enable operation.</returns>
    Task<EnableCurrencyResult> EnableCurrencyAsync(Guid id, string userId, string? userEmail = null);

    /// <summary>
    /// Disables a currency from use in new listings and transactions.
    /// Existing historical data is preserved.
    /// </summary>
    /// <param name="id">The currency ID to disable.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="userEmail">The email of the user performing the action.</param>
    /// <param name="reason">Optional reason for disabling.</param>
    /// <returns>The result of the disable operation.</returns>
    Task<DisableCurrencyResult> DisableCurrencyAsync(Guid id, string userId, string? userEmail = null, string? reason = null);

    /// <summary>
    /// Sets a currency as the platform's base currency.
    /// This is a significant operation that affects all exchange rate calculations.
    /// </summary>
    /// <param name="id">The currency ID to set as base.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="userEmail">The email of the user performing the action.</param>
    /// <param name="confirmationCode">Confirmation code required for this operation.</param>
    /// <returns>The result of the set base currency operation.</returns>
    Task<SetBaseCurrencyResult> SetBaseCurrencyAsync(Guid id, string userId, string? userEmail = null, string? confirmationCode = null);

    /// <summary>
    /// Gets the history of changes for a specific currency.
    /// </summary>
    /// <param name="currencyId">The currency ID.</param>
    /// <returns>The result containing the currency history.</returns>
    Task<GetCurrencyHistoryResult> GetCurrencyHistoryAsync(Guid currencyId);
}

/// <summary>
/// Result of getting all currencies.
/// </summary>
public class GetCurrenciesResult
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
    /// Gets the currencies.
    /// </summary>
    public IReadOnlyList<Currency> Currencies { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with currencies.
    /// </summary>
    /// <param name="currencies">The currencies.</param>
    /// <returns>A successful result.</returns>
    public static GetCurrenciesResult Success(IReadOnlyList<Currency> currencies) => new()
    {
        Succeeded = true,
        Errors = [],
        Currencies = currencies
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCurrenciesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCurrenciesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCurrenciesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single currency.
/// </summary>
public class GetCurrencyResult
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
    /// Gets the currency.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Creates a successful result with a currency.
    /// </summary>
    /// <param name="currency">The currency.</param>
    /// <returns>A successful result.</returns>
    public static GetCurrencyResult Success(Currency currency) => new()
    {
        Succeeded = true,
        Errors = [],
        Currency = currency
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCurrencyResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCurrencyResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCurrencyResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new currency.
/// </summary>
public class CreateCurrencyCommand
{
    /// <summary>
    /// Gets or sets the ISO 4217 currency code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the currency name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the currency symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of decimal places.
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether this currency is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID creating this currency.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user creating this currency.
    /// </summary>
    public string? CreatedByUserEmail { get; set; }
}

/// <summary>
/// Result of creating a currency.
/// </summary>
public class CreateCurrencyResult
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
    /// Gets the created currency.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Creates a successful result with the created currency.
    /// </summary>
    /// <param name="currency">The created currency.</param>
    /// <returns>A successful result.</returns>
    public static CreateCurrencyResult Success(Currency currency) => new()
    {
        Succeeded = true,
        Errors = [],
        Currency = currency
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateCurrencyResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateCurrencyResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateCurrencyResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing currency.
/// </summary>
public class UpdateCurrencyCommand
{
    /// <summary>
    /// Gets or sets the ID of the currency to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the currency name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the currency symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of decimal places.
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Gets or sets the user ID updating this currency.
    /// </summary>
    public string UpdatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user updating this currency.
    /// </summary>
    public string? UpdatedByUserEmail { get; set; }
}

/// <summary>
/// Result of updating a currency.
/// </summary>
public class UpdateCurrencyResult
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
    /// Gets the updated currency.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated currency.
    /// </summary>
    /// <param name="currency">The updated currency.</param>
    /// <returns>A successful result.</returns>
    public static UpdateCurrencyResult Success(Currency currency) => new()
    {
        Succeeded = true,
        Errors = [],
        Currency = currency
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateCurrencyResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateCurrencyResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateCurrencyResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of enabling a currency.
/// </summary>
public class EnableCurrencyResult
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
    /// Gets the enabled currency.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Creates a successful result with the enabled currency.
    /// </summary>
    /// <param name="currency">The enabled currency.</param>
    /// <returns>A successful result.</returns>
    public static EnableCurrencyResult Success(Currency currency) => new()
    {
        Succeeded = true,
        Errors = [],
        Currency = currency
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static EnableCurrencyResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static EnableCurrencyResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static EnableCurrencyResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of disabling a currency.
/// </summary>
public class DisableCurrencyResult
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
    /// Gets the disabled currency.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Creates a successful result with the disabled currency.
    /// </summary>
    /// <param name="currency">The disabled currency.</param>
    /// <returns>A successful result.</returns>
    public static DisableCurrencyResult Success(Currency currency) => new()
    {
        Succeeded = true,
        Errors = [],
        Currency = currency
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DisableCurrencyResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DisableCurrencyResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DisableCurrencyResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of setting a currency as the base currency.
/// </summary>
public class SetBaseCurrencyResult
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
    /// Gets a value indicating whether confirmation is required.
    /// </summary>
    public bool RequiresConfirmation { get; private init; }

    /// <summary>
    /// Gets the warning message about the impact of changing the base currency.
    /// </summary>
    public string? WarningMessage { get; private init; }

    /// <summary>
    /// Gets the new base currency.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Gets the previous base currency.
    /// </summary>
    public Currency? PreviousBaseCurrency { get; private init; }

    /// <summary>
    /// Creates a successful result with the new base currency.
    /// </summary>
    /// <param name="currency">The new base currency.</param>
    /// <param name="previousBaseCurrency">The previous base currency.</param>
    /// <returns>A successful result.</returns>
    public static SetBaseCurrencyResult Success(Currency currency, Currency? previousBaseCurrency) => new()
    {
        Succeeded = true,
        Errors = [],
        Currency = currency,
        PreviousBaseCurrency = previousBaseCurrency
    };

    /// <summary>
    /// Creates a result requiring confirmation before proceeding.
    /// </summary>
    /// <param name="warningMessage">The warning message about the impact.</param>
    /// <returns>A result requiring confirmation.</returns>
    public static SetBaseCurrencyResult ConfirmationRequired(string warningMessage) => new()
    {
        Succeeded = false,
        RequiresConfirmation = true,
        WarningMessage = warningMessage,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SetBaseCurrencyResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SetBaseCurrencyResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static SetBaseCurrencyResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting currency history.
/// </summary>
public class GetCurrencyHistoryResult
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
    /// Gets the currency history records.
    /// </summary>
    public IReadOnlyList<CurrencyHistory> History { get; private init; } = [];

    /// <summary>
    /// Gets the currency for context.
    /// </summary>
    public Currency? Currency { get; private init; }

    /// <summary>
    /// Creates a successful result with history records.
    /// </summary>
    /// <param name="history">The history records.</param>
    /// <param name="currency">The currency.</param>
    /// <returns>A successful result.</returns>
    public static GetCurrencyHistoryResult Success(IReadOnlyList<CurrencyHistory> history, Currency? currency) => new()
    {
        Succeeded = true,
        Errors = [],
        History = history,
        Currency = currency
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCurrencyHistoryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCurrencyHistoryResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCurrencyHistoryResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
