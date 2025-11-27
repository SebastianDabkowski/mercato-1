using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Orders;

public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        // TODO: Register Orders module services and infrastructure here
        return services;
    }
}
