namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a currency configuration for the marketplace platform.
/// Currencies define the monetary units available for listings and transactions.
/// </summary>
public class Currency
{
    /// <summary>
    /// Gets or sets the unique identifier for the currency.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., USD, EUR, GBP).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full name of the currency (e.g., United States Dollar).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the currency symbol (e.g., $, €, £).
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of decimal places for display (typically 2).
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether this is the platform's base currency.
    /// Only one currency can be the base currency at any time.
    /// </summary>
    public bool IsBaseCurrency { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this currency is available for use.
    /// Disabled currencies cannot be used for new listings or transactions.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the exchange rate relative to the base currency.
    /// Null for the base currency itself.
    /// </summary>
    public decimal? ExchangeRateToBase { get; set; }

    /// <summary>
    /// Gets or sets the source of the exchange rate (e.g., "ECB", "Manual", "OpenExchangeRates").
    /// </summary>
    public string? ExchangeRateSource { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the exchange rate was last updated.
    /// </summary>
    public DateTimeOffset? ExchangeRateUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this currency was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this currency.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this currency was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this currency.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
