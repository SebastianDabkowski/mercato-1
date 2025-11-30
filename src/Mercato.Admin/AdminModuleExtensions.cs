using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Admin.Infrastructure.Persistence;
using Mercato.Admin.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Admin;

// TODO: FIX it: Consider adding IDesignTimeDbContextFactory<AdminDbContext> for migrations support.
// This allows running EF Core migrations without requiring the full application startup.
public static class AdminModuleExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Register AdminDbContext
        if (configuration != null)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<AdminDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
        }

        // Register repositories
        services.AddScoped<IRoleChangeAuditRepository, RoleChangeAuditRepository>();
        services.AddScoped<IAuthenticationEventRepository, AuthenticationEventRepository>();
        services.AddScoped<ISlaConfigurationRepository, SlaConfigurationRepository>();
        services.AddScoped<ISlaTrackingRepository, SlaTrackingRepository>();
        services.AddScoped<IAdminAuditRepository, AdminAuditRepository>();
        // Note: MarketplaceDashboardRepository requires OrderDbContext, SellerDbContext, and ProductDbContext
        // which are registered by their respective modules (Orders, Seller, Product) in Program.cs.
        services.AddScoped<IMarketplaceDashboardRepository, MarketplaceDashboardRepository>();
        // Note: OrderRevenueReportRepository requires OrderDbContext and PaymentDbContext
        // which are registered by their respective modules (Orders, Payments) in Program.cs.
        services.AddScoped<IOrderRevenueReportRepository, OrderRevenueReportRepository>();

        // Register services
        services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();
        services.AddScoped<IAuthenticationEventService, AuthenticationEventService>();
        services.AddScoped<ISlaTrackingService, SlaTrackingService>();
        services.AddScoped<IAdminCaseService, AdminCaseService>();
        services.AddScoped<IReviewModerationService, ReviewModerationService>();
        services.AddScoped<IMarketplaceDashboardService, MarketplaceDashboardService>();
        services.AddScoped<IOrderRevenueReportService, OrderRevenueReportService>();

        return services;
    }
}
