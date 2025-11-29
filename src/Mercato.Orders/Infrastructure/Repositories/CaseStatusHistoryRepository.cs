using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for case status history data access operations.
/// </summary>
public class CaseStatusHistoryRepository : ICaseStatusHistoryRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaseStatusHistoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CaseStatusHistoryRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaseStatusHistory>> GetByReturnRequestIdAsync(Guid returnRequestId)
    {
        return await _context.CaseStatusHistories
            .Where(h => h.ReturnRequestId == returnRequestId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CaseStatusHistory> AddAsync(CaseStatusHistory history)
    {
        await _context.CaseStatusHistories.AddAsync(history);
        await _context.SaveChangesAsync();
        return history;
    }
}
