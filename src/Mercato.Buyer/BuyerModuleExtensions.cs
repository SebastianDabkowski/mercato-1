using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Buyer;

public static class BuyerModuleExtensions
{
    public static IServiceCollection AddBuyerModule(this IServiceCollection services)
    {
        // TODO: Register Buyer module services and infrastructure here
        return services;
    }
}
