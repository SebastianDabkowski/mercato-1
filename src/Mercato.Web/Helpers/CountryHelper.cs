using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Helpers;

/// <summary>
/// Provides helper methods for working with country codes and names.
/// </summary>
public static class CountryHelper
{
    private static readonly Dictionary<string, string> CountryNames = new()
    {
        { "US", "United States" },
        { "CA", "Canada" },
        { "GB", "United Kingdom" },
        { "AU", "Australia" },
        { "DE", "Germany" },
        { "FR", "France" },
        { "IT", "Italy" },
        { "ES", "Spain" },
        { "JP", "Japan" },
        { "BR", "Brazil" },
        { "MX", "Mexico" },
        { "IN", "India" },
        { "NL", "Netherlands" },
        { "BE", "Belgium" },
        { "CH", "Switzerland" },
        { "AT", "Austria" },
        { "SE", "Sweden" },
        { "NO", "Norway" },
        { "DK", "Denmark" },
        { "FI", "Finland" },
        { "PL", "Poland" },
        { "IE", "Ireland" },
        { "PT", "Portugal" },
        { "NZ", "New Zealand" },
        { "SG", "Singapore" },
        { "HK", "Hong Kong" },
        { "KR", "South Korea" },
        { "TW", "Taiwan" }
    };

    /// <summary>
    /// Gets the display name for a country code.
    /// </summary>
    /// <param name="countryCode">The ISO country code.</param>
    /// <returns>The country display name, or the code if no name is found.</returns>
    public static string GetCountryName(string countryCode)
    {
        return CountryNames.GetValueOrDefault(countryCode, countryCode);
    }

    /// <summary>
    /// Gets a list of SelectListItems for the allowed shipping countries.
    /// </summary>
    /// <param name="countryCodes">The list of allowed country codes.</param>
    /// <returns>A list of SelectListItems sorted by country name.</returns>
    public static List<SelectListItem> GetCountrySelectList(IEnumerable<string> countryCodes)
    {
        return countryCodes
            .OrderBy(c => GetCountryName(c))
            .Select(c => new SelectListItem
            {
                Value = c,
                Text = GetCountryName(c)
            })
            .ToList();
    }

    /// <summary>
    /// Formats an address as a single line for display.
    /// </summary>
    /// <param name="city">The city.</param>
    /// <param name="state">The state or province (optional).</param>
    /// <param name="postalCode">The postal code.</param>
    /// <returns>A formatted city, state, postal code string.</returns>
    public static string FormatCityStatePostal(string? city, string? state, string? postalCode)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(city))
        {
            parts.Add(city);
        }
        
        if (!string.IsNullOrWhiteSpace(state))
        {
            parts.Add(state);
        }

        var result = string.Join(", ", parts);
        
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            result = string.IsNullOrEmpty(result) ? postalCode : $"{result} {postalCode}";
        }

        return result;
    }
}
