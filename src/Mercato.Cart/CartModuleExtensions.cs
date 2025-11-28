using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Cart.Infrastructure;
using Mercato.Cart.Infrastructure.Persistence;
using Mercato.Cart.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Cart;

/// <summary>
/// Extension methods for registering cart module services.
/// </summary>
public static class CartModuleExtensions
{
    /// <summary>
    /// Adds cart module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCartModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register CartDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<CartDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<ICartRepository, CartRepository>();

        // Register services
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IShippingCalculator, ShippingCalculator>();
        services.AddScoped<ICommissionCalculator, CommissionCalculator>();
        services.AddScoped<IShippingMethodService, ShippingMethodService>();
        services.AddScoped<ICheckoutValidationService, CheckoutValidationService>();

        return services;
    }
}
