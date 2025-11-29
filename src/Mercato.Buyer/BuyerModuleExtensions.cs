using Mercato.Buyer.Application.Services;
using Mercato.Buyer.Domain.Interfaces;
using Mercato.Buyer.Infrastructure;
using Mercato.Buyer.Infrastructure.Persistence;
using Mercato.Buyer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Buyer;

/// <summary>
/// Extension methods for registering buyer module services.
/// </summary>
public static class BuyerModuleExtensions
{
    /// <summary>
    /// Adds buyer module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddBuyerModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register BuyerDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<BuyerDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<IDeliveryAddressRepository, DeliveryAddressRepository>();

        // Configure buyer email settings
        services.Configure<BuyerEmailSettings>(configuration.GetSection("BuyerEmail"));

        // Register services
        services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
        services.AddScoped<IDeliveryAddressService, DeliveryAddressService>();
        services.AddScoped<IBuyerEmailNotificationService, BuyerEmailNotificationService>();

        return services;
    }
}
