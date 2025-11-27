using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Payments;

public static class PaymentsModuleExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        // TODO: Register Payments module services and infrastructure here
        return services;
    }
}
