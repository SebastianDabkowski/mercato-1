using Mercato.Buyer.Application.Services;
using Mercato.Buyer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Buyer;

public static class BuyerModuleExtensions
{
    public static IServiceCollection AddBuyerModule(this IServiceCollection services)
    {
        // Register Buyer module services
        services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();

        return services;
    }
}
