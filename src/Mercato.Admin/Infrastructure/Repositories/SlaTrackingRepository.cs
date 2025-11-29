using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SLA tracking records using Entity Framework Core.
/// </summary>
public class SlaTrackingRepository : ISlaTrackingRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlaTrackingRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public SlaTrackingRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<SlaTrackingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SlaTrackingRecords
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SlaTrackingRecord?> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SlaTrackingRecords
            .FirstOrDefaultAsync(r => r.CaseId == caseId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaTrackingRecord>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SlaTrackingRecords
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.CaseCreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaTrackingRecord>> GetBreachedCasesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SlaTrackingRecords
            .Where(r => (r.IsFirstResponseBreached || r.IsResolutionBreached) &&
                        r.Status != SlaStatus.Closed &&
                        r.Status != SlaStatus.ResolvedWithinSla)
            .OrderByDescending(r => r.CaseCreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaTrackingRecord>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? storeId = null,
        SlaStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SlaTrackingRecords
            .Where(r => r.CaseCreatedAt >= startDate && r.CaseCreatedAt <= endDate);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CaseCreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SlaStoreStatistics> GetStoreStatisticsAsync(
        Guid storeId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SlaTrackingRecords
            .Where(r => r.StoreId == storeId &&
                        r.CaseCreatedAt >= startDate &&
                        r.CaseCreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        return CalculateStoreStatistics(storeId, records);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaStoreStatistics>> GetAllStoreStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SlaTrackingRecords
            .Where(r => r.CaseCreatedAt >= startDate && r.CaseCreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var groupedByStore = records.GroupBy(r => r.StoreId);
        var statistics = new List<SlaStoreStatistics>();

        foreach (var group in groupedByStore)
        {
            statistics.Add(CalculateStoreStatistics(group.Key, group.ToList()));
        }

        return statistics
            .OrderByDescending(s => s.TotalCases)
            .ThenBy(s => s.StoreName)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<SlaTrackingRecord> AddAsync(SlaTrackingRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _dbContext.SlaTrackingRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SlaTrackingRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        _dbContext.SlaTrackingRecords.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SlaStoreStatistics CalculateStoreStatistics(Guid storeId, IReadOnlyList<SlaTrackingRecord> records)
    {
        var storeName = records.FirstOrDefault()?.StoreName ?? string.Empty;
        var totalCases = records.Count;
        var resolvedWithinSla = records.Count(r => r.Status == SlaStatus.ResolvedWithinSla);
        var respondedWithinSla = records.Count(r => r.FirstResponseAt.HasValue && !r.IsFirstResponseBreached);
        var firstResponseBreaches = records.Count(r => r.IsFirstResponseBreached);
        var resolutionBreaches = records.Count(r => r.IsResolutionBreached);

        var responseTimes = records
            .Where(r => r.ResponseTimeHours.HasValue)
            .Select(r => r.ResponseTimeHours!.Value)
            .ToList();

        var resolutionTimes = records
            .Where(r => r.ResolutionTimeHours.HasValue)
            .Select(r => r.ResolutionTimeHours!.Value)
            .ToList();

        return new SlaStoreStatistics
        {
            StoreId = storeId,
            StoreName = storeName,
            TotalCases = totalCases,
            CasesResolvedWithinSla = resolvedWithinSla,
            CasesRespondedWithinSla = respondedWithinSla,
            FirstResponseBreaches = firstResponseBreaches,
            ResolutionBreaches = resolutionBreaches,
            AverageResponseTimeHours = responseTimes.Count > 0 ? Math.Round(responseTimes.Average(), 2) : 0,
            AverageResolutionTimeHours = resolutionTimes.Count > 0 ? Math.Round(resolutionTimes.Average(), 2) : 0
        };
    }
}
