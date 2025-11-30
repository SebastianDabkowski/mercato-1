using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Admin.Infrastructure.Persistence;
using Mercato.Admin.Infrastructure.Repositories;
using Mercato.Identity.Application.Services;
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
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();
        services.AddScoped<IVatRuleRepository, VatRuleRepository>();
        services.AddScoped<IVatRuleHistoryRepository, VatRuleHistoryRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<ICurrencyHistoryRepository, CurrencyHistoryRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        // Note: MarketplaceDashboardRepository requires OrderDbContext, SellerDbContext, and ProductDbContext
        // which are registered by their respective modules (Orders, Seller, Product) in Program.cs.
        services.AddScoped<IMarketplaceDashboardRepository, MarketplaceDashboardRepository>();
        // Note: OrderRevenueReportRepository requires OrderDbContext and PaymentDbContext
        // which are registered by their respective modules (Orders, Payments) in Program.cs.
        services.AddScoped<IOrderRevenueReportRepository, OrderRevenueReportRepository>();
        // Note: CommissionSummaryRepository requires PaymentDbContext and SellerDbContext
        // which are registered by their respective modules (Payments, Seller) in Program.cs.
        services.AddScoped<ICommissionSummaryRepository, CommissionSummaryRepository>();
        // Note: UserAnalyticsRepository requires AdminDbContext, OrderDbContext, and SellerDbContext
        // which are registered by their respective modules (Orders, Seller) in Program.cs.
        services.AddScoped<IUserAnalyticsRepository, UserAnalyticsRepository>();

        // Register services
        services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();
        services.AddScoped<IUserAccountManagementService, UserAccountManagementService>();
        services.AddScoped<IAuthenticationEventService, AuthenticationEventService>();
        services.AddScoped<ISlaTrackingService, SlaTrackingService>();
        services.AddScoped<IAdminCaseService, AdminCaseService>();
        services.AddScoped<IReviewModerationService, ReviewModerationService>();
        services.AddScoped<IMarketplaceDashboardService, MarketplaceDashboardService>();
        services.AddScoped<IOrderRevenueReportService, OrderRevenueReportService>();
        services.AddScoped<ICommissionSummaryService, CommissionSummaryService>();
        services.AddScoped<IUserAnalyticsService, UserAnalyticsService>();
        services.AddScoped<IUserBlockCheckService, UserBlockCheckService>();
        // Note: ProductModerationService requires IProductModerationRepository from Product module,
        // IStoreRepository from Seller module, and INotificationService from Notifications module.
        services.AddScoped<IProductModerationService, ProductModerationService>();
        // Note: PhotoModerationService requires IPhotoModerationRepository from Product module,
        // IStoreRepository from Seller module, and INotificationService from Notifications module.
        services.AddScoped<IPhotoModerationService, PhotoModerationService>();
        // Note: CommissionRuleManagementService requires ICommissionRuleRepository from Payments module,
        // which is registered by the Payments module in Program.cs.
        services.AddScoped<ICommissionRuleManagementService, CommissionRuleManagementService>();
        services.AddScoped<IVatRuleManagementService, VatRuleManagementService>();
        services.AddScoped<ICurrencyManagementService, CurrencyManagementService>();
        services.AddScoped<IIntegrationManagementService, IntegrationManagementService>();

        return services;
    }
}
