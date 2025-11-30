using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for security incident data access operations.
/// </summary>
public class SecurityIncidentRepository : ISecurityIncidentRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityIncidentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SecurityIncidentRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<SecurityIncident> AddAsync(SecurityIncident incident, CancellationToken cancellationToken = default)
    {
        _context.SecurityIncidents.Add(incident);
        await _context.SaveChangesAsync(cancellationToken);
        return incident;
    }

    /// <inheritdoc/>
    public async Task<SecurityIncident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SecurityIncident> UpdateAsync(SecurityIncident incident, CancellationToken cancellationToken = default)
    {
        _context.SecurityIncidents.Update(incident);
        await _context.SaveChangesAsync(cancellationToken);
        return incident;
    }

    /// <inheritdoc/>
    public async Task<SecurityIncidentStatusChange> AddStatusChangeAsync(SecurityIncidentStatusChange statusChange, CancellationToken cancellationToken = default)
    {
        _context.SecurityIncidentStatusChanges.Add(statusChange);
        await _context.SaveChangesAsync(cancellationToken);
        return statusChange;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecurityIncidentStatusChange>> GetStatusChangesAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityIncidentStatusChanges
            .AsNoTracking()
            .Where(sc => sc.SecurityIncidentId == incidentId)
            .OrderBy(sc => sc.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecurityIncident>> GetFilteredAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        SecurityIncidentSeverity? severity = null,
        SecurityIncidentStatus? status = null,
        string? detectionRule = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityIncidents.AsNoTracking();

        if (startDate.HasValue)
        {
            query = query.Where(i => i.DetectedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.DetectedAt <= endDate.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(i => i.Severity == severity.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(detectionRule))
        {
            query = query.Where(i => i.DetectionRule == detectionRule);
        }

        return await query
            .OrderByDescending(i => i.DetectedAt)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IDictionary<SecurityIncidentStatus, int>> GetIncidentCountsByStatusAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var counts = await _context.SecurityIncidents
            .AsNoTracking()
            .Where(i => i.DetectedAt >= startDate && i.DetectedAt <= endDate)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }
}
