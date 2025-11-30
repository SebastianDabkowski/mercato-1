using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Notifications.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for push subscription data access operations.
/// </summary>
public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly NotificationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushSubscriptionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PushSubscriptionRepository(NotificationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(string userId)
    {
        return await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<PushSubscription> AddAsync(PushSubscription subscription)
    {
        await _context.PushSubscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await _context.PushSubscriptions.FindAsync(id);
        if (subscription == null)
        {
            return false;
        }

        _context.PushSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByUserIdAsync(string userId)
    {
        var count = await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync();

        return count;
    }
}
