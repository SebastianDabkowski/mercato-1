using Mercato.Identity.Application.Services;
using Mercato.Identity.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // Register buyer registration service
        services.AddScoped<IBuyerRegistrationService, BuyerRegistrationService>();
        
        return services;
    }
}
