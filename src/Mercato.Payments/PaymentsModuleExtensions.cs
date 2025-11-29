using Mercato.Payments.Application.Services;
using Mercato.Payments.Infrastructure;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Payments;

/// <summary>
/// Extension methods for registering payment module services.
/// </summary>
public static class PaymentsModuleExtensions
{
    /// <summary>
    /// Adds payment module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register PaymentDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register PaymentSettings from configuration
        services.Configure<PaymentSettings>(configuration.GetSection("Payments"));

        // Register services
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
