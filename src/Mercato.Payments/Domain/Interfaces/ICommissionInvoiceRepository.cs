using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for commission invoice data access.
/// </summary>
public interface ICommissionInvoiceRepository
{
    /// <summary>Gets an invoice by ID.</summary>
    /// <param name="id">The invoice identifier.</param>
    /// <returns>The invoice if found; otherwise, null.</returns>
    Task<CommissionInvoice?> GetByIdAsync(Guid id);

    /// <summary>Gets an invoice by ID for a specific seller.</summary>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The invoice if found and belongs to the seller; otherwise, null.</returns>
    Task<CommissionInvoice?> GetByIdForSellerAsync(Guid id, Guid sellerId);

    /// <summary>Gets all invoices for a seller.</summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>A read-only list of invoices for the seller.</returns>
    Task<IReadOnlyList<CommissionInvoice>> GetBySellerIdAsync(Guid sellerId);

    /// <summary>Gets an invoice by seller, year, and month.</summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="year">The year of the billing period.</param>
    /// <param name="month">The month of the billing period.</param>
    /// <param name="invoiceType">The type of invoice.</param>
    /// <returns>The invoice if found; otherwise, null.</returns>
    Task<CommissionInvoice?> GetBySellerYearMonthAsync(Guid sellerId, int year, int month, InvoiceType invoiceType);

    /// <summary>Adds a new invoice.</summary>
    /// <param name="invoice">The invoice to add.</param>
    /// <returns>The added invoice.</returns>
    Task<CommissionInvoice> AddAsync(CommissionInvoice invoice);

    /// <summary>Updates an existing invoice.</summary>
    /// <param name="invoice">The invoice to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(CommissionInvoice invoice);

    /// <summary>Gets the next sequential invoice number.</summary>
    /// <param name="year">The year for the invoice number.</param>
    /// <returns>The next sequential invoice number.</returns>
    Task<string> GetNextInvoiceNumberAsync(int year);
}
