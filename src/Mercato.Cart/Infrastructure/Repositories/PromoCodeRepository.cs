using Mercato.Cart.Domain.Entities;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Cart.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for promo code data access operations.
/// </summary>
public class PromoCodeRepository : IPromoCodeRepository
{
    private readonly CartDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromoCodeRepository"/> class.
    /// </summary>
    /// <param name="context">The cart database context.</param>
    public PromoCodeRepository(CartDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PromoCode?> GetByCodeAsync(string code)
    {
        // Use EF.Functions.Collate for case-insensitive comparison with SQL Server
        // This allows the database to use indexes effectively
        return await _context.PromoCodes
            .FirstOrDefaultAsync(p => EF.Functions.Collate(p.Code, "Latin1_General_CI_AS") == code);
    }

    /// <inheritdoc />
    public async Task<PromoCode?> GetByIdAsync(Guid id)
    {
        return await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <inheritdoc />
    public async Task<PromoCode> AddAsync(PromoCode promoCode)
    {
        _context.PromoCodes.Add(promoCode);
        await _context.SaveChangesAsync();
        return promoCode;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PromoCode promoCode)
    {
        _context.PromoCodes.Update(promoCode);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task IncrementUsageCountAsync(Guid id)
    {
        var promoCode = await _context.PromoCodes.FindAsync(id);
        if (promoCode != null)
        {
            promoCode.UsageCount++;
            promoCode.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
