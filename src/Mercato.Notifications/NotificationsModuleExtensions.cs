using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure;
using Mercato.Notifications.Infrastructure.Persistence;
using Mercato.Notifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Notifications;

/// <summary>
/// Extension methods for registering Notifications module services.
/// </summary>
public static class NotificationsModuleExtensions
{
    /// <summary>
    /// Adds the Notifications module services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register NotificationDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageThreadRepository, MessageThreadRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

        // Register web push client
        services.AddScoped<IWebPushClient, WebPushClient>();

        // Register services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessagingService, MessagingService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();

        return services;
    }
}
