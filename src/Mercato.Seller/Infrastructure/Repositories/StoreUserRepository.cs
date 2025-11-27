using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for store user data access.
/// </summary>
public class StoreUserRepository : IStoreUserRepository
{
    private readonly SellerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreUserRepository"/> class.
    /// </summary>
    /// <param name="context">The seller database context.</param>
    public StoreUserRepository(SellerDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<StoreUser?> GetByIdAsync(Guid id)
    {
        return await _context.StoreUsers
            .Include(su => su.Store)
            .FirstOrDefaultAsync(su => su.Id == id);
    }

    /// <inheritdoc />
    public async Task<StoreUser?> GetByInvitationTokenAsync(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return await _context.StoreUsers
            .Include(su => su.Store)
            .FirstOrDefaultAsync(su => su.InvitationToken == token);
    }

    /// <inheritdoc />
    public async Task<StoreUser?> GetByStoreAndUserIdAsync(Guid storeId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _context.StoreUsers
            .Include(su => su.Store)
            .FirstOrDefaultAsync(su => su.StoreId == storeId && su.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<StoreUser?> GetByStoreAndEmailAsync(Guid storeId, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await _context.StoreUsers
            .Include(su => su.Store)
            .FirstOrDefaultAsync(su => su.StoreId == storeId && su.Email == email);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreUser>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.StoreUsers
            .Include(su => su.Store)
            .Where(su => su.StoreId == storeId)
            .OrderBy(su => su.Email)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreUser>> GetByUserIdAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _context.StoreUsers
            .Include(su => su.Store)
            .Where(su => su.UserId == userId && su.Status == StoreUserStatus.Active)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task CreateAsync(StoreUser storeUser)
    {
        ArgumentNullException.ThrowIfNull(storeUser);

        _context.StoreUsers.Add(storeUser);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(StoreUser storeUser)
    {
        ArgumentNullException.ThrowIfNull(storeUser);

        _context.StoreUsers.Update(storeUser);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsForStoreAsync(Guid storeId, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await _context.StoreUsers
            .AnyAsync(su => su.StoreId == storeId && su.Email == email);
    }
}
