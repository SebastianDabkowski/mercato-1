using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Admin.Infrastructure.Persistence;
using Mercato.Admin.Infrastructure.Repositories;
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

        // Register services
        services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();
        services.AddScoped<IAuthenticationEventService, AuthenticationEventService>();

        return services;
    }
}
