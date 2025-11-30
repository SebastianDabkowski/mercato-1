using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for product moderation operations.
/// </summary>
public class ProductModerationRepository : IProductModerationRepository
{
    private readonly ProductDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductModerationRepository"/> class.
    /// </summary>
    /// <param name="context">The product database context.</param>
    public ProductModerationRepository(ProductDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> GetProductsForModerationAsync(
        IReadOnlyList<ProductModerationStatus>? moderationStatuses,
        string? category,
        string? searchTerm,
        int page,
        int pageSize)
    {
        var query = _context.Products.AsQueryable();

        // Apply moderation status filter
        if (moderationStatuses != null && moderationStatuses.Count > 0)
        {
            query = query.Where(p => moderationStatuses.Contains(p.ModerationStatus));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Escape LIKE wildcards to prevent unintended pattern matching
            var escapedSearchTerm = EscapeLikePattern(searchTerm);
            var likePattern = $"%{escapedSearchTerm}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Title, likePattern, "\\") ||
                (p.Description != null && EF.Functions.Like(p.Description, likePattern, "\\")));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    /// <summary>
    /// Escapes LIKE pattern special characters to prevent SQL injection via wildcards.
    /// </summary>
    /// <param name="input">The input string to escape.</param>
    /// <returns>The escaped string safe for use in LIKE patterns.</returns>
    private static string EscapeLikePattern(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Product?> GetProductForModerationAsync(Guid productId)
    {
        return await _context.Products.FindAsync(productId);
    }

    /// <inheritdoc />
    public async Task UpdateModerationStatusAsync(Domain.Entities.Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateModerationStatusBulkAsync(IEnumerable<Domain.Entities.Product> products)
    {
        _context.Products.UpdateRange(products);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<ProductModerationDecision> AddModerationDecisionAsync(ProductModerationDecision decision)
    {
        _context.ProductModerationDecisions.Add(decision);
        await _context.SaveChangesAsync();
        return decision;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductModerationDecision>> GetModerationHistoryAsync(Guid productId)
    {
        return await _context.ProductModerationDecisions
            .Where(d => d.ProductId == productId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync()
    {
        return await _context.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.Product>> GetProductsByIdsAsync(IEnumerable<Guid> productIds)
    {
        var idList = productIds.ToList();
        return await _context.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }
}
