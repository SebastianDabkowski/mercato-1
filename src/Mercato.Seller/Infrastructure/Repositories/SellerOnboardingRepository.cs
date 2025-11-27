using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for seller onboarding data access.
/// </summary>
public class SellerOnboardingRepository : ISellerOnboardingRepository
{
    private readonly SellerDbContext _context;

    public SellerOnboardingRepository(SellerDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SellerOnboarding?> GetBySellerIdAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _context.SellerOnboardings
            .FirstOrDefaultAsync(o => o.SellerId == sellerId);
    }

    /// <inheritdoc />
    public async Task<SellerOnboarding?> GetByIdAsync(Guid id)
    {
        return await _context.SellerOnboardings.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task CreateAsync(SellerOnboarding onboarding)
    {
        ArgumentNullException.ThrowIfNull(onboarding);

        _context.SellerOnboardings.Add(onboarding);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SellerOnboarding onboarding)
    {
        ArgumentNullException.ThrowIfNull(onboarding);

        _context.SellerOnboardings.Update(onboarding);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _context.SellerOnboardings.AnyAsync(o => o.SellerId == sellerId);
    }
}
