using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for data processing activity management.
/// </summary>
public class DataProcessingActivityRepository : IDataProcessingActivityRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProcessingActivityRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DataProcessingActivityRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<DataProcessingActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DataProcessingActivity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivities
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DataProcessingActivity>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivities
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DataProcessingActivity> AddAsync(DataProcessingActivity activity, CancellationToken cancellationToken = default)
    {
        _context.DataProcessingActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(DataProcessingActivity activity, CancellationToken cancellationToken = default)
    {
        _context.DataProcessingActivities.Update(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
