using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Notifications.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for message thread data access operations.
/// </summary>
public class MessageThreadRepository : IMessageThreadRepository
{
    private readonly NotificationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageThreadRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public MessageThreadRepository(NotificationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<MessageThread?> GetByIdAsync(Guid id)
    {
        return await _context.MessageThreads.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetByProductIdAsync(
        Guid productId,
        int page,
        int pageSize)
    {
        var query = _context.MessageThreads
            .Where(t => t.ProductId == productId && t.ThreadType == MessageThreadType.ProductQuestion);

        var totalCount = await query.CountAsync();

        var threads = await query
            .OrderByDescending(t => t.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (threads, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetByBuyerIdAsync(
        string buyerId,
        int page,
        int pageSize)
    {
        var query = _context.MessageThreads.Where(t => t.BuyerId == buyerId);

        var totalCount = await query.CountAsync();

        var threads = await query
            .OrderByDescending(t => t.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (threads, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetBySellerIdAsync(
        string sellerId,
        int page,
        int pageSize)
    {
        var query = _context.MessageThreads.Where(t => t.SellerId == sellerId);

        var totalCount = await query.CountAsync();

        var threads = await query
            .OrderByDescending(t => t.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (threads, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetAllAsync(
        int page,
        int pageSize)
    {
        var query = _context.MessageThreads.AsQueryable();

        var totalCount = await query.CountAsync();

        var threads = await query
            .OrderByDescending(t => t.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (threads, totalCount);
    }

    /// <inheritdoc />
    public async Task<MessageThread> AddAsync(MessageThread thread)
    {
        await _context.MessageThreads.AddAsync(thread);
        await _context.SaveChangesAsync();
        return thread;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(MessageThread thread)
    {
        _context.MessageThreads.Update(thread);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessAsync(Guid threadId, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.MessageThreads.AnyAsync(t => t.Id == threadId);
        }

        return await _context.MessageThreads
            .AnyAsync(t => t.Id == threadId && (t.BuyerId == userId || t.SellerId == userId));
    }
}
