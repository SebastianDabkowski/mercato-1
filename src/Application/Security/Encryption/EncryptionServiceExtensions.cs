using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Application.Security.Encryption;

/// <summary>
/// Extension methods for registering encryption services.
/// </summary>
public static class EncryptionServiceExtensions
{
    /// <summary>
    /// Adds encryption services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncryptionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new EncryptionOptions();
        configuration.GetSection(EncryptionOptions.SectionName).Bind(options);

        // Configure Data Protection with key persistence
        services.AddDataProtection()
            .SetApplicationName(options.ApplicationName)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        // Register encryption options
        services.AddSingleton(options);

        // Register the encryption service based on provider configuration
        if (options.Provider.Equals("AzureKeyVault", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(options.KeyVaultUri))
        {
            // For Azure Key Vault, additional configuration would be needed
            // This is a placeholder for Azure Key Vault integration
            // In production, use Azure.Security.KeyVault.Keys package
            services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();
        }
        else
        {
            services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();
        }

        return services;
    }
}
