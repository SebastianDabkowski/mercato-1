using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for authentication events using Entity Framework Core.
/// </summary>
public class AuthenticationEventRepository : IAuthenticationEventRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationEventRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public AuthenticationEventRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task AddAsync(AuthenticationEvent authEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authEvent);
        await _dbContext.AuthenticationEvents.AddAsync(authEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuthenticationEvent>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuthenticationEvent>> GetFilteredAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        AuthenticationEventType? eventType = null,
        string? userRole = null,
        string? ipAddressHash = null,
        bool? isSuccessful = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuthenticationEvents.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= endDate.Value);
        }

        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        if (!string.IsNullOrEmpty(userRole))
        {
            query = query.Where(e => e.UserRole == userRole);
        }

        if (!string.IsNullOrEmpty(ipAddressHash))
        {
            query = query.Where(e => e.IpAddressHash == ipAddressHash);
        }

        if (isSuccessful.HasValue)
        {
            query = query.Where(e => e.IsSuccessful == isSuccessful.Value);
        }

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<AuthenticationEventType, int>> GetEventCountsByTypeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetFailedAttemptsByIpAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int minimumAttempts = 5,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .Where(e => !e.IsSuccessful)
            .Where(e => e.EventType == AuthenticationEventType.Login)
            .Where(e => e.IpAddressHash != null)
            .GroupBy(e => e.IpAddressHash!)
            .Select(g => new { IpHash = g.Key, Count = g.Count() })
            .Where(x => x.Count >= minimumAttempts)
            .ToDictionaryAsync(x => x.IpHash, x => x.Count, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetRapidLoginAttemptsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int minimumAttempts = 10,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .Where(e => e.EventType == AuthenticationEventType.Login)
            .GroupBy(e => e.Email)
            .Select(g => new { Email = g.Key, Count = g.Count() })
            .Where(x => x.Count >= minimumAttempts)
            .ToDictionaryAsync(x => x.Email, x => x.Count, cancellationToken);
    }
}
