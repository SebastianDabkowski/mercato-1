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
        
        // Register buyer login service
        services.AddScoped<IBuyerLoginService, BuyerLoginService>();
        
        // Register Google login service
        services.AddScoped<IGoogleLoginService, GoogleLoginService>();
        
        // Register Facebook login service
        services.AddScoped<IFacebookLoginService, FacebookLoginService>();
        
        return services;
    }
}
