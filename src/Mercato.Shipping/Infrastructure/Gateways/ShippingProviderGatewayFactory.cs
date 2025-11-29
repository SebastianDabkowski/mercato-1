using Mercato.Shipping.Application.Services;

namespace Mercato.Shipping.Infrastructure.Gateways;

/// <summary>
/// Factory for creating shipping provider gateways based on provider code.
/// </summary>
public interface IShippingProviderGatewayFactory
{
    /// <summary>
    /// Gets the gateway for a specific shipping provider.
    /// </summary>
    /// <param name="providerCode">The provider code (e.g., "DHL", "FEDEX").</param>
    /// <returns>The shipping provider gateway if found; otherwise, null.</returns>
    IShippingProviderGateway? GetGateway(string providerCode);

    /// <summary>
    /// Gets all registered provider codes.
    /// </summary>
    /// <returns>A list of available provider codes.</returns>
    IReadOnlyList<string> GetAvailableProviderCodes();
}

/// <summary>
/// Factory implementation for creating shipping provider gateways.
/// </summary>
public class ShippingProviderGatewayFactory : IShippingProviderGatewayFactory
{
    private readonly Dictionary<string, IShippingProviderGateway> _gateways;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingProviderGatewayFactory"/> class.
    /// </summary>
    /// <param name="gateways">The collection of available gateways.</param>
    public ShippingProviderGatewayFactory(IEnumerable<IShippingProviderGateway> gateways)
    {
        _gateways = gateways.ToDictionary(
            g => g.ProviderCode.ToUpperInvariant(),
            g => g,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IShippingProviderGateway? GetGateway(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            return null;
        }

        return _gateways.TryGetValue(providerCode.ToUpperInvariant(), out var gateway) ? gateway : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableProviderCodes()
    {
        return _gateways.Keys.ToList();
    }
}
