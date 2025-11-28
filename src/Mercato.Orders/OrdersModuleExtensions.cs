using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Mercato.Orders.Infrastructure.Persistence;
using Mercato.Orders.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Orders;

public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register OrderDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register services
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
