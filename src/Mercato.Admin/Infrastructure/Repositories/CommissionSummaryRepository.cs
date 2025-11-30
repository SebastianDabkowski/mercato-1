using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Infrastructure.Persistence;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for aggregating commission summary data.
/// </summary>
public class CommissionSummaryRepository : ICommissionSummaryRepository
{
    private readonly PaymentDbContext _paymentDbContext;
    private readonly SellerDbContext _sellerDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionSummaryRepository"/> class.
    /// </summary>
    /// <param name="paymentDbContext">The payments database context.</param>
    /// <param name="sellerDbContext">The seller database context.</param>
    public CommissionSummaryRepository(
        PaymentDbContext paymentDbContext,
        SellerDbContext sellerDbContext)
    {
        _paymentDbContext = paymentDbContext ?? throw new ArgumentNullException(nameof(paymentDbContext));
        _sellerDbContext = sellerDbContext ?? throw new ArgumentNullException(nameof(sellerDbContext));
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<SellerCommissionSummaryRow> Rows, decimal TotalGMV, decimal TotalCommission, decimal TotalNetPayout, int TotalOrderCount)> GetSummaryDataAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate)
    {
        // Start with commission records as the base
        var query = _paymentDbContext.CommissionRecords.AsNoTracking();

        // Apply date filter on CalculatedAt (when the commission was recorded)
        if (fromDate.HasValue)
        {
            query = query.Where(c => c.CalculatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(c => c.CalculatedAt <= toDate.Value);
        }

        // Get all commission records for the period
        var commissionRecords = await query.ToListAsync();

        // Get seller names from store table
        var sellerIds = commissionRecords.Select(c => c.SellerId).Distinct().ToList();
        var stores = await _sellerDbContext.Stores
            .AsNoTracking()
            .Where(s => sellerIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        var storeNameLookup = stores.ToDictionary(s => s.Id, s => s.Name);

        // Group by seller and calculate aggregates
        var groupedData = commissionRecords
            .GroupBy(c => c.SellerId)
            .Select(g => new SellerCommissionSummaryRow
            {
                SellerId = g.Key,
                SellerName = storeNameLookup.GetValueOrDefault(g.Key, "Unknown Seller"),
                TotalGMV = g.Sum(c => CalculateGMV(c)),
                TotalCommission = g.Sum(c => c.NetCommissionAmount),
                TotalNetPayout = g.Sum(c => CalculateNetPayout(c)),
                OrderCount = g.Select(c => c.OrderId).Distinct().Count()
            })
            .OrderByDescending(r => r.TotalGMV)
            .ToList();

        // Calculate overall totals
        var totalGMV = groupedData.Sum(r => r.TotalGMV);
        var totalCommission = groupedData.Sum(r => r.TotalCommission);
        var totalNetPayout = groupedData.Sum(r => r.TotalNetPayout);
        var totalOrderCount = groupedData.Sum(r => r.OrderCount);

        return (groupedData, totalGMV, totalCommission, totalNetPayout, totalOrderCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<OrderCommissionRow> Rows, int TotalCount, Guid SellerId, string SellerName)> GetSellerOrdersAsync(
        Guid sellerId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize)
    {
        // Start with commission records for the specific seller
        var query = _paymentDbContext.CommissionRecords
            .AsNoTracking()
            .Where(c => c.SellerId == sellerId);

        // Apply date filter
        if (fromDate.HasValue)
        {
            query = query.Where(c => c.CalculatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(c => c.CalculatedAt <= toDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paginated results
        var commissionRecords = await query
            .OrderByDescending(c => c.CalculatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get seller name
        var store = await _sellerDbContext.Stores
            .AsNoTracking()
            .Where(s => s.Id == sellerId)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync();

        var sellerName = store?.Name ?? "Unknown Seller";

        // Map to result rows
        var rows = commissionRecords.Select(c => new OrderCommissionRow
        {
            OrderId = c.OrderId,
            OrderDate = c.CalculatedAt,
            OrderAmount = CalculateGMV(c),
            CommissionRate = c.CommissionRate,
            CommissionAmount = c.NetCommissionAmount,
            NetPayout = CalculateNetPayout(c),
            CalculatedAt = c.CalculatedAt
        }).ToList();

        return (rows, totalCount, sellerId, sellerName);
    }

    /// <summary>
    /// Calculates the Gross Merchandise Value (GMV) from a commission record.
    /// GMV is the original order amount minus any refunded amount.
    /// </summary>
    /// <param name="record">The commission record.</param>
    /// <returns>The calculated GMV.</returns>
    private static decimal CalculateGMV(CommissionRecord record)
    {
        return record.OrderAmount - record.RefundedAmount;
    }

    /// <summary>
    /// Calculates the net payout to the seller from a commission record.
    /// Net payout is GMV minus the net commission amount.
    /// </summary>
    /// <param name="record">The commission record.</param>
    /// <returns>The calculated net payout.</returns>
    private static decimal CalculateNetPayout(CommissionRecord record)
    {
        return CalculateGMV(record) - record.NetCommissionAmount;
    }
}
