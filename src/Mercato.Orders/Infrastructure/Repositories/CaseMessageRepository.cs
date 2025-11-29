using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for case message data access operations.
/// </summary>
public class CaseMessageRepository : ICaseMessageRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaseMessageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CaseMessageRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaseMessage>> GetByReturnRequestIdAsync(Guid returnRequestId)
    {
        return await _context.CaseMessages
            .Where(m => m.ReturnRequestId == returnRequestId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CaseMessage> AddAsync(CaseMessage message)
    {
        await _context.CaseMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        return message;
    }
}
