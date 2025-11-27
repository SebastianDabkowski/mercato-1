using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for payout settings data access.
/// </summary>
public class PayoutSettingsRepository : IPayoutSettingsRepository
{
    private readonly SellerDbContext _context;

    public PayoutSettingsRepository(SellerDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PayoutSettings?> GetBySellerIdAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _context.PayoutSettings
            .FirstOrDefaultAsync(p => p.SellerId == sellerId);
    }

    /// <inheritdoc />
    public async Task<PayoutSettings?> GetByIdAsync(Guid id)
    {
        return await _context.PayoutSettings.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task CreateAsync(PayoutSettings payoutSettings)
    {
        ArgumentNullException.ThrowIfNull(payoutSettings);

        _context.PayoutSettings.Add(payoutSettings);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PayoutSettings payoutSettings)
    {
        ArgumentNullException.ThrowIfNull(payoutSettings);

        _context.PayoutSettings.Update(payoutSettings);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _context.PayoutSettings.AnyAsync(p => p.SellerId == sellerId);
    }
}
