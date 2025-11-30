using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for data processing activity history management.
/// </summary>
public class DataProcessingActivityHistoryRepository : IDataProcessingActivityHistoryRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProcessingActivityHistoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DataProcessingActivityHistoryRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DataProcessingActivityHistory>> GetByActivityIdAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivityHistories
            .Where(h => h.DataProcessingActivityId == activityId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DataProcessingActivityHistory> AddAsync(DataProcessingActivityHistory history, CancellationToken cancellationToken = default)
    {
        _context.DataProcessingActivityHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);
        return history;
    }
}
