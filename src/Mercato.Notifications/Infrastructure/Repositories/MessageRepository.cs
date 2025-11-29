using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Mercato.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Notifications.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for message data access operations.
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly NotificationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public MessageRepository(NotificationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _context.Messages
            .Include(m => m.Thread)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Message> Messages, int TotalCount)> GetByThreadIdAsync(
        Guid threadId,
        int page,
        int pageSize)
    {
        var query = _context.Messages.Where(m => m.ThreadId == threadId);

        var totalCount = await query.CountAsync();

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (messages, totalCount);
    }

    /// <inheritdoc />
    public async Task<Message> AddAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();
        return message;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(Guid id, string userId)
    {
        var message = await _context.Messages
            .Include(m => m.Thread)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null)
        {
            return false;
        }

        // Only the recipient can mark a message as read (not the sender)
        var thread = message.Thread;
        if (thread == null)
        {
            return false;
        }

        var isRecipient = (thread.BuyerId == userId && message.SenderId != userId) ||
                          (thread.SellerId == userId && message.SenderId != userId);

        if (!isRecipient)
        {
            return false;
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }
}
