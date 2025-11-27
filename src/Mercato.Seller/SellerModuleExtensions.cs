using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Seller;

public static class SellerModuleExtensions
{
    public static IServiceCollection AddSellerModule(this IServiceCollection services)
    {
        // TODO: Register Seller module services and infrastructure here
        return services;
    }
}
