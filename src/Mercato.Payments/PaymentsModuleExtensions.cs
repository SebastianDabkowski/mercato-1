using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Mercato.Payments.Infrastructure.Persistence;
using Mercato.Payments.Infrastructure.Repositories;
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

        // Register EscrowSettings from configuration
        services.Configure<EscrowSettings>(configuration.GetSection("Escrow"));

        // Register repositories
        services.AddScoped<IEscrowRepository, EscrowRepository>();

        // Register services
        services.AddScoped<IPaymentStatusMapper, PaymentStatusMapper>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IEscrowService, EscrowService>();

        return services;
    }
}
