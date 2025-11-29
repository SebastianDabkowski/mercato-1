using Mercato.Shipping.Application.Services;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure;
using Mercato.Shipping.Infrastructure.Gateways;
using Mercato.Shipping.Infrastructure.Persistence;
using Mercato.Shipping.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Shipping;

/// <summary>
/// Extension methods for registering shipping module services.
/// </summary>
public static class ShippingModuleExtensions
{
    /// <summary>
    /// Adds shipping module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddShippingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register ShippingDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ShippingDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<IShippingProviderRepository, ShippingProviderRepository>();
        services.AddScoped<IStoreShippingProviderRepository, StoreShippingProviderRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShipmentStatusUpdateRepository, ShipmentStatusUpdateRepository>();

        // Register gateways
        services.AddScoped<IShippingProviderGateway, MockDhlGateway>();
        services.AddScoped<IShippingProviderGateway, MockFedExGateway>();
        services.AddScoped<IShippingProviderGatewayFactory, ShippingProviderGatewayFactory>();

        // Register services
        services.AddScoped<IShippingProviderService, ShippingProviderService>();
        services.AddScoped<IShipmentService, ShipmentService>();

        return services;
    }
}
