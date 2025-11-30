using Mercato.Orders.Application.Services;
using Mercato.Payments.Application.Services;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Mercato.Seller.Infrastructure.Persistence;
using Mercato.Seller.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Seller;

// TODO: FIX it: Consider adding IDesignTimeDbContextFactory<SellerDbContext> for migrations support.
// This allows running EF Core migrations without requiring the full application startup.
public static class SellerModuleExtensions
{
    public static IServiceCollection AddSellerModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register SellerDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<SellerDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Configure seller email settings
        services.Configure<SellerEmailSettings>(configuration.GetSection("SellerEmail"));

        // Register repositories
        services.AddScoped<IKycRepository, KycRepository>();
        services.AddScoped<ISellerOnboardingRepository, SellerOnboardingRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPayoutSettingsRepository, PayoutSettingsRepository>();
        services.AddScoped<IStoreUserRepository, StoreUserRepository>();
        services.AddScoped<IShippingRuleRepository, ShippingRuleRepository>();
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<ISellerReputationRepository, SellerReputationRepository>();
        services.AddScoped<ISellerSalesDashboardRepository, SellerSalesDashboardRepository>();

        // Register services
        services.AddScoped<IKycService, KycService>();
        services.AddScoped<ISellerOnboardingService, SellerOnboardingService>();
        services.AddScoped<IStoreProfileService, StoreProfileService>();
        services.AddScoped<IPayoutSettingsService, PayoutSettingsService>();
        services.AddScoped<IStoreUserService, StoreUserService>();
        services.AddScoped<IShippingMethodService, ShippingMethodService>();
        services.AddScoped<ISellerReputationService, SellerReputationService>();
        services.AddScoped<ISellerSalesDashboardService, SellerSalesDashboardService>();
        services.AddScoped<ISellerNotificationEmailService, SellerNotificationEmailService>();
        services.AddScoped<IStoreEmailProvider, StoreEmailProvider>();
        services.AddScoped<IPayoutNotificationService, PayoutNotificationService>();
        services.AddScoped<ISellerEmailProvider, SellerEmailProvider>();

        return services;
    }
}
