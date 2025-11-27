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

        // Register repositories
        services.AddScoped<IKycRepository, KycRepository>();

        // Register services
        services.AddScoped<IKycService, KycService>();

        return services;
    }
}
