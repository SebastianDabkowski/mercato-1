using Mercato.Analytics.Application.Services;
using Mercato.Analytics.Domain.Interfaces;
using Mercato.Analytics.Infrastructure.Persistence;
using Mercato.Analytics.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Analytics;

/// <summary>
/// Extension methods for registering analytics module services.
/// </summary>
public static class AnalyticsModuleExtensions
{
    /// <summary>
    /// Adds analytics module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.Configure<AnalyticsOptions>(configuration.GetSection("Analytics"));

        // Register AnalyticsDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AnalyticsDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        // Register repositories
        services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();

        // Register services
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
