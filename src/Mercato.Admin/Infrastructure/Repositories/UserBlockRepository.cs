using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user block data access operations.
/// </summary>
public class UserBlockRepository : IUserBlockRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserBlockRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserBlockRepository(AdminDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<UserBlockInfo?> GetActiveBlockAsync(string userId)
    {
        return await _context.UserBlockInfos
            .Where(b => b.UserId == userId && b.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<UserBlockInfo?> GetByIdAsync(Guid id)
    {
        return await _context.UserBlockInfos
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserBlockInfo>> GetBlockHistoryAsync(string userId)
    {
        return await _context.UserBlockInfos
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BlockedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserBlockInfo> AddAsync(UserBlockInfo blockInfo)
    {
        await _context.UserBlockInfos.AddAsync(blockInfo);
        await _context.SaveChangesAsync();
        return blockInfo;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserBlockInfo blockInfo)
    {
        _context.UserBlockInfos.Update(blockInfo);
        await _context.SaveChangesAsync();
    }
}
