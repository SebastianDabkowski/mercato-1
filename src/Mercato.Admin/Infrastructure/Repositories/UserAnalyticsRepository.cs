using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Mercato.Orders.Infrastructure.Persistence;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user analytics data aggregations.
/// All queries return aggregated, anonymized data to comply with privacy requirements.
/// </summary>
public class UserAnalyticsRepository : IUserAnalyticsRepository
{
    private readonly AdminDbContext _adminDbContext;
    private readonly OrderDbContext _orderDbContext;
    private readonly SellerDbContext _sellerDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAnalyticsRepository"/> class.
    /// </summary>
    /// <param name="adminDbContext">The admin database context for authentication events.</param>
    /// <param name="orderDbContext">The orders database context for order data.</param>
    /// <param name="sellerDbContext">The seller database context for store data.</param>
    public UserAnalyticsRepository(
        AdminDbContext adminDbContext,
        OrderDbContext orderDbContext,
        SellerDbContext sellerDbContext)
    {
        ArgumentNullException.ThrowIfNull(adminDbContext);
        ArgumentNullException.ThrowIfNull(orderDbContext);
        ArgumentNullException.ThrowIfNull(sellerDbContext);

        _adminDbContext = adminDbContext;
        _orderDbContext = orderDbContext;
        _sellerDbContext = sellerDbContext;
    }

    /// <inheritdoc/>
    public async Task<int> GetUsersLoggedInCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await _adminDbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .Where(e => e.EventType == AuthenticationEventType.Login)
            .Where(e => e.IsSuccessful)
            .Where(e => e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetUsersLoggedInByRoleAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var results = await _adminDbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .Where(e => e.EventType == AuthenticationEventType.Login)
            .Where(e => e.IsSuccessful)
            .Where(e => e.UserId != null && e.UserRole != null)
            .GroupBy(e => e.UserRole!)
            .Select(g => new { Role = g.Key, UserCount = g.Select(e => e.UserId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        return results.ToDictionary(r => r.Role, r => r.UserCount);
    }

    /// <inheritdoc/>
    public async Task<int> GetUsersWhoPlacedOrdersCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await _orderDbContext.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Select(o => o.BuyerId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetNewBuyerRegistrationsCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        // Count first-time login events for Buyer role within the period
        // This uses the first login as a proxy for registration since we don't have registration date
        // This approach counts unique users who logged in as Buyer for the first time in the period
        // Note: This is an approximation. For accurate registration counts, 
        // consider extending IdentityUser with a RegistrationDate property.
        
        // Get users who logged in as Buyer during this period
        var buyerLoginsInPeriod = await _adminDbContext.AuthenticationEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .Where(e => e.EventType == AuthenticationEventType.Login)
            .Where(e => e.IsSuccessful)
            .Where(e => e.UserRole == "Buyer")
            .Where(e => e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!buyerLoginsInPeriod.Any())
        {
            return 0;
        }

        // Get users who had logged in as Buyer before this period
        var usersWithPriorLogins = await _adminDbContext.AuthenticationEvents
            .Where(e => e.OccurredAt < startDate)
            .Where(e => e.EventType == AuthenticationEventType.Login)
            .Where(e => e.IsSuccessful)
            .Where(e => e.UserRole == "Buyer")
            .Where(e => e.UserId != null)
            .Where(e => buyerLoginsInPeriod.Contains(e.UserId))
            .Select(e => e.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // New buyers are those who logged in during the period but not before
        return buyerLoginsInPeriod.Count - usersWithPriorLogins.Count;
    }

    /// <inheritdoc/>
    public async Task<int> GetNewSellerRegistrationsCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        // Count new stores created during the period as a proxy for seller registrations
        return await _sellerDbContext.Stores
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .CountAsync(cancellationToken);
    }
}
