using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for seller reputation data access.
/// </summary>
public class SellerReputationRepository : ISellerReputationRepository
{
    private readonly SellerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerReputationRepository"/> class.
    /// </summary>
    /// <param name="context">The seller database context.</param>
    public SellerReputationRepository(SellerDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SellerReputation?> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.SellerReputations
            .FirstOrDefaultAsync(r => r.StoreId == storeId);
    }

    /// <inheritdoc />
    public async Task CreateAsync(SellerReputation reputation)
    {
        ArgumentNullException.ThrowIfNull(reputation);

        _context.SellerReputations.Add(reputation);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SellerReputation reputation)
    {
        ArgumentNullException.ThrowIfNull(reputation);

        _context.SellerReputations.Update(reputation);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerReputation>> GetByStoreIdsAsync(IEnumerable<Guid> storeIds)
    {
        ArgumentNullException.ThrowIfNull(storeIds);

        var idList = storeIds.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await _context.SellerReputations
            .Where(r => idList.Contains(r.StoreId))
            .ToListAsync();
    }
}
