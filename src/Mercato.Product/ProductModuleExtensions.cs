using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Mercato.Product.Infrastructure.Persistence;
using Mercato.Product.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Product;

/// <summary>
/// Extension methods for registering product module services.
/// </summary>
public static class ProductModuleExtensions
{
    /// <summary>
    /// Adds product module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="contentRootPath">The content root path for file uploads. If null, uses current directory.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddProductModule(this IServiceCollection services, IConfiguration configuration, string? contentRootPath = null)
    {
        // Register ProductDbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ProductDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductImportRepository, ProductImportRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IProductModerationRepository, ProductModerationRepository>();

        // Register services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductImportService, ProductImportService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<ISearchSuggestionService, SearchSuggestionService>();

        // Register ProductImageService with wwwroot path
        services.AddScoped<IProductImageService>(sp =>
        {
            var imageRepository = sp.GetRequiredService<IProductImageRepository>();
            var productRepository = sp.GetRequiredService<IProductRepository>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProductImageService>>();

            // Get wwwroot path from configuration or use default
            var basePath = contentRootPath ?? Directory.GetCurrentDirectory();
            var wwwrootPath = Path.Combine(basePath, "wwwroot");

            return new ProductImageService(imageRepository, productRepository, logger, wwwrootPath);
        });

        return services;
    }
}
