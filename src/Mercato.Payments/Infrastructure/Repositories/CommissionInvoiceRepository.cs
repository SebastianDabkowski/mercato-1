using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for commission invoice data access operations.
/// </summary>
public class CommissionInvoiceRepository : ICommissionInvoiceRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionInvoiceRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public CommissionInvoiceRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetByIdAsync(Guid id)
    {
        return await _dbContext.CommissionInvoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetByIdForSellerAsync(Guid id, Guid sellerId)
    {
        return await _dbContext.CommissionInvoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id && i.SellerId == sellerId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommissionInvoice>> GetBySellerIdAsync(Guid sellerId)
    {
        return await _dbContext.CommissionInvoices
            .Where(i => i.SellerId == sellerId)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetBySellerYearMonthAsync(Guid sellerId, int year, int month, InvoiceType invoiceType)
    {
        return await _dbContext.CommissionInvoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => 
                i.SellerId == sellerId && 
                i.Year == year && 
                i.Month == month && 
                i.InvoiceType == invoiceType &&
                i.Status != InvoiceStatus.Cancelled);
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice> AddAsync(CommissionInvoice invoice)
    {
        await _dbContext.CommissionInvoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync();
        return invoice;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CommissionInvoice invoice)
    {
        _dbContext.CommissionInvoices.Update(invoice);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<string> GetNextInvoiceNumberAsync(int year)
    {
        var lastInvoiceNumber = await _dbContext.CommissionInvoices
            .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-"))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastInvoiceNumber))
        {
            var parts = lastInvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"INV-{year}-{nextNumber:D6}";
    }
}
