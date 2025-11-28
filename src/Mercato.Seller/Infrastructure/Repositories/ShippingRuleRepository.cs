using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipping rule operations.
/// </summary>
public class ShippingRuleRepository : IShippingRuleRepository
{
    private readonly SellerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingRuleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ShippingRuleRepository(SellerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ShippingRule?> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.ShippingRules
            .FirstOrDefaultAsync(r => r.StoreId == storeId);
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, ShippingRule>> GetByStoreIdsAsync(IEnumerable<Guid> storeIds)
    {
        var storeIdList = storeIds.ToList();
        if (storeIdList.Count == 0)
        {
            return new Dictionary<Guid, ShippingRule>();
        }

        var rules = await _context.ShippingRules
            .Where(r => storeIdList.Contains(r.StoreId))
            .ToListAsync();

        return rules.ToDictionary(r => r.StoreId);
    }
}
