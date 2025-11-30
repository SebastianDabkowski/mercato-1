using Mercato.Analytics.Domain.Entities;
using Mercato.Analytics.Domain.Interfaces;
using Mercato.Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Analytics.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for analytics event data access operations.
/// </summary>
public class AnalyticsEventRepository : IAnalyticsEventRepository
{
    private readonly AnalyticsDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsEventRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AnalyticsEventRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AnalyticsEvent> AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        await _context.AnalyticsEvents.AddAsync(analyticsEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return analyticsEvent;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnalyticsEvent>> GetByTimeRangeAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.AnalyticsEvents
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnalyticsEvent>> GetByEventTypeAsync(
        AnalyticsEventType eventType,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.AnalyticsEvents
            .Where(e => e.EventType == eventType && e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IDictionary<AnalyticsEventType, int>> GetEventCountsByTypeAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.AnalyticsEvents
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);
    }
}
