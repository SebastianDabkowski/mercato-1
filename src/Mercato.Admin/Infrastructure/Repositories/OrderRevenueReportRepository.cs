using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Infrastructure.Persistence;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for aggregating order and revenue report data.
/// </summary>
public class OrderRevenueReportRepository : IOrderRevenueReportRepository
{
    private readonly OrderDbContext _orderDbContext;
    private readonly PaymentDbContext _paymentDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderRevenueReportRepository"/> class.
    /// </summary>
    /// <param name="orderDbContext">The orders database context.</param>
    /// <param name="paymentDbContext">The payments database context.</param>
    public OrderRevenueReportRepository(
        OrderDbContext orderDbContext,
        PaymentDbContext paymentDbContext)
    {
        _orderDbContext = orderDbContext ?? throw new ArgumentNullException(nameof(orderDbContext));
        _paymentDbContext = paymentDbContext ?? throw new ArgumentNullException(nameof(paymentDbContext));
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<OrderRevenueReportRow> Rows, int TotalCount, decimal TotalOrderValue, decimal TotalCommission, decimal TotalPayoutAmount)> GetReportDataAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        Guid? sellerId,
        IReadOnlyList<OrderStatus>? orderStatuses,
        IReadOnlyList<PaymentStatus>? paymentStatuses,
        int page,
        int pageSize)
    {
        // Start with seller sub-orders as the base to group by seller
        var subOrderQuery = _orderDbContext.SellerSubOrders
            .Include(s => s.Order)
            .AsNoTracking();

        // Apply date filter
        if (fromDate.HasValue)
        {
            subOrderQuery = subOrderQuery.Where(s => s.Order.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            subOrderQuery = subOrderQuery.Where(s => s.Order.CreatedAt <= toDate.Value);
        }

        // Apply seller filter
        if (sellerId.HasValue)
        {
            subOrderQuery = subOrderQuery.Where(s => s.StoreId == sellerId.Value);
        }

        // Apply order status filter
        if (orderStatuses != null && orderStatuses.Count > 0)
        {
            subOrderQuery = subOrderQuery.Where(s => orderStatuses.Contains(s.Order.Status));
        }

        // Get order IDs to filter commission and escrow queries
        var orderIds = await subOrderQuery.Select(s => s.OrderId).Distinct().ToListAsync();

        // Get commission records filtered by order IDs
        var commissionRecords = await _paymentDbContext.CommissionRecords
            .AsNoTracking()
            .Where(c => orderIds.Contains(c.OrderId))
            .ToListAsync();

        // Create a dictionary for quick lookup by OrderId and SellerId
        var commissionLookup = commissionRecords
            .GroupBy(c => (c.OrderId, c.SellerId))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.NetCommissionAmount));

        // Get escrow entries filtered by order IDs for payment status information
        var escrowEntries = await _paymentDbContext.EscrowEntries
            .AsNoTracking()
            .Where(e => orderIds.Contains(e.OrderId))
            .ToListAsync();

        // Create a dictionary for payment status lookup by OrderId and SellerId
        var escrowLookup = escrowEntries
            .GroupBy(e => (e.OrderId, e.SellerId))
            .ToDictionary(g => g.Key, g => g.First().Status);

        // Execute the query for sub-orders
        var subOrders = await subOrderQuery.ToListAsync();

        // Build the report rows
        var allRows = new List<OrderRevenueReportRow>();

        foreach (var subOrder in subOrders)
        {
            var orderValue = subOrder.TotalAmount;
            var commissionKey = (subOrder.OrderId, subOrder.StoreId);
            var commission = commissionLookup.GetValueOrDefault(commissionKey, 0m);
            var payoutAmount = orderValue - commission;

            // Get payment status from escrow or default to Pending
            var paymentStatus = escrowLookup.GetValueOrDefault((subOrder.OrderId, subOrder.StoreId));
            var mappedPaymentStatus = MapEscrowStatusToPaymentStatus(paymentStatus);

            allRows.Add(new OrderRevenueReportRow
            {
                OrderId = subOrder.OrderId,
                OrderNumber = subOrder.Order.OrderNumber,
                OrderDate = subOrder.Order.CreatedAt,
                BuyerEmail = !string.IsNullOrEmpty(subOrder.Order.BuyerEmail)
                    ? subOrder.Order.BuyerEmail
                    : subOrder.Order.BuyerId,
                SellerName = subOrder.StoreName,
                SellerId = subOrder.StoreId,
                OrderStatus = subOrder.Order.Status,
                PaymentStatus = mappedPaymentStatus,
                OrderValue = orderValue,
                Commission = commission,
                PayoutAmount = payoutAmount
            });
        }

        // Apply payment status filter in memory
        if (paymentStatuses != null && paymentStatuses.Count > 0)
        {
            allRows = allRows.Where(r => paymentStatuses.Contains(r.PaymentStatus)).ToList();
        }

        // Calculate totals from all matching rows
        var totalCount = allRows.Count;
        var totalOrderValue = allRows.Sum(r => r.OrderValue);
        var totalCommission = allRows.Sum(r => r.Commission);
        var totalPayoutAmount = allRows.Sum(r => r.PayoutAmount);

        // Apply pagination
        var pagedRows = allRows
            .OrderByDescending(r => r.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedRows, totalCount, totalOrderValue, totalCommission, totalPayoutAmount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid SellerId, string SellerName)>> GetDistinctSellersAsync()
    {
        var sellers = await _orderDbContext.SellerSubOrders
            .AsNoTracking()
            .Select(s => new { s.StoreId, s.StoreName })
            .Distinct()
            .OrderBy(s => s.StoreName)
            .ToListAsync();

        return sellers.Select(s => (s.StoreId, s.StoreName)).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        Guid? sellerId,
        IReadOnlyList<OrderStatus>? orderStatuses,
        IReadOnlyList<PaymentStatus>? paymentStatuses)
    {
        // Start with seller sub-orders as the base
        var subOrderQuery = _orderDbContext.SellerSubOrders
            .Include(s => s.Order)
            .AsNoTracking();

        // Apply date filter
        if (fromDate.HasValue)
        {
            subOrderQuery = subOrderQuery.Where(s => s.Order.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            subOrderQuery = subOrderQuery.Where(s => s.Order.CreatedAt <= toDate.Value);
        }

        // Apply seller filter
        if (sellerId.HasValue)
        {
            subOrderQuery = subOrderQuery.Where(s => s.StoreId == sellerId.Value);
        }

        // Apply order status filter
        if (orderStatuses != null && orderStatuses.Count > 0)
        {
            subOrderQuery = subOrderQuery.Where(s => orderStatuses.Contains(s.Order.Status));
        }

        // If no payment status filter, we can count directly
        if (paymentStatuses == null || paymentStatuses.Count == 0)
        {
            return await subOrderQuery.CountAsync();
        }

        // If payment status filter is applied, we need to load and filter in memory
        // This is a simplified count that may need refinement for very large datasets
        var orderIds = await subOrderQuery.Select(s => s.OrderId).Distinct().ToListAsync();
        var escrowEntries = await _paymentDbContext.EscrowEntries
            .AsNoTracking()
            .Where(e => orderIds.Contains(e.OrderId))
            .ToListAsync();

        var escrowLookup = escrowEntries
            .GroupBy(e => (e.OrderId, e.SellerId))
            .ToDictionary(g => g.Key, g => MapEscrowStatusToPaymentStatus(g.First().Status));

        var subOrders = await subOrderQuery.Select(s => new { s.OrderId, s.StoreId }).ToListAsync();
        
        return subOrders.Count(s =>
        {
            var paymentStatus = escrowLookup.GetValueOrDefault((s.OrderId, s.StoreId), PaymentStatus.Pending);
            return paymentStatuses.Contains(paymentStatus);
        });
    }

    private static PaymentStatus MapEscrowStatusToPaymentStatus(EscrowStatus escrowStatus)
    {
        return escrowStatus switch
        {
            EscrowStatus.Held => PaymentStatus.Paid,
            EscrowStatus.Released => PaymentStatus.Paid,
            EscrowStatus.PartiallyRefunded => PaymentStatus.Refunded,
            EscrowStatus.Refunded => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };
    }
}
