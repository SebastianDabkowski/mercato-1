using Mercato.Identity.Application.Services;
using Mercato.Identity.Domain.Interfaces;
using Mercato.Identity.Infrastructure;
using Mercato.Identity.Infrastructure.Repositories;
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
        
        // Register seller registration service
        services.AddScoped<ISellerRegistrationService, SellerRegistrationService>();
        
        // Register seller login service
        services.AddScoped<ISellerLoginService, SellerLoginService>();
        
        // Register Google login service
        services.AddScoped<IGoogleLoginService, GoogleLoginService>();
        
        // Register Facebook login service
        services.AddScoped<IFacebookLoginService, FacebookLoginService>();
        
        // Register account linking service
        services.AddScoped<IAccountLinkingService, AccountLinkingService>();
        
        // Register password reset service
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        
        // Register password change service
        services.AddScoped<IPasswordChangeService, PasswordChangeService>();
        
        // Register RBAC repositories as singletons to maintain state across requests
        services.AddSingleton<IPermissionRepository, PermissionRepository>();
        services.AddSingleton<IRolePermissionRepository, RolePermissionRepository>();
        
        // Register RBAC configuration service
        services.AddScoped<IRbacConfigurationService, RbacConfigurationService>();
        
        // Register user data export service for GDPR compliance
        services.AddScoped<IUserDataExportService, UserDataExportService>();
        
        return services;
    }
}
