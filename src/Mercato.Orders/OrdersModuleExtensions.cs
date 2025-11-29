using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Mercato.Orders.Infrastructure.Persistence;
using Mercato.Orders.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Orders;

/// <summary>
/// Extension methods for registering Orders module services.
/// </summary>
public static class OrdersModuleExtensions
{
    /// <summary>
    /// Adds the Orders module services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register OrderDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ISellerSubOrderRepository, SellerSubOrderRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IShippingStatusHistoryRepository, ShippingStatusHistoryRepository>();
        services.AddScoped<ICaseMessageRepository, CaseMessageRepository>();
        services.AddScoped<ICaseStatusHistoryRepository, CaseStatusHistoryRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();

        // Configure email settings
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        // Configure return settings
        services.Configure<ReturnSettings>(configuration.GetSection("Returns"));

        // Register services
        services.AddScoped<IOrderConfirmationEmailService, OrderConfirmationEmailService>();
        services.AddScoped<IShippingNotificationService, ShippingNotificationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductReviewService, ProductReviewService>();

        return services;
    }
}
