using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for commission invoice operations.
/// </summary>
public interface ICommissionInvoiceService
{
    /// <summary>
    /// Generates a monthly invoice for a seller based on commission records.
    /// </summary>
    /// <param name="command">The generate monthly invoice command.</param>
    /// <returns>The result of the invoice generation.</returns>
    Task<GenerateMonthlyInvoiceResult> GenerateMonthlyInvoiceAsync(GenerateMonthlyInvoiceCommand command);

    /// <summary>
    /// Gets an invoice by ID for a specific seller.
    /// </summary>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The result containing the invoice.</returns>
    Task<GetInvoiceResult> GetInvoiceByIdAsync(Guid id, Guid sellerId);

    /// <summary>
    /// Gets all invoices for a seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The result containing invoices.</returns>
    Task<GetInvoicesResult> GetInvoicesBySellerIdAsync(Guid sellerId);

    /// <summary>
    /// Generates a PDF for an invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice identifier.</param>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The result containing PDF bytes.</returns>
    Task<GeneratePdfResult> GeneratePdfAsync(Guid invoiceId, Guid sellerId);

    /// <summary>
    /// Creates a credit note for an existing invoice.
    /// </summary>
    /// <param name="command">The create credit note command.</param>
    /// <returns>The result of the credit note creation.</returns>
    Task<CreateCreditNoteResult> CreateCreditNoteAsync(CreateCreditNoteCommand command);
}

/// <summary>
/// Command to generate a monthly invoice for a seller.
/// </summary>
public class GenerateMonthlyInvoiceCommand
{
    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the year of the billing period.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the month of the billing period.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets optional notes for the invoice.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Result of generating a monthly invoice.
/// </summary>
public class GenerateMonthlyInvoiceResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the generated invoice.
    /// </summary>
    public CommissionInvoice? Invoice { get; private init; }

    /// <summary>
    /// Creates a successful result with the generated invoice.
    /// </summary>
    /// <param name="invoice">The generated invoice.</param>
    /// <returns>A successful result.</returns>
    public static GenerateMonthlyInvoiceResult Success(CommissionInvoice invoice) => new()
    {
        Succeeded = true,
        Errors = [],
        Invoice = invoice
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GenerateMonthlyInvoiceResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GenerateMonthlyInvoiceResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GenerateMonthlyInvoiceResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single invoice.
/// </summary>
public class GetInvoiceResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the invoice.
    /// </summary>
    public CommissionInvoice? Invoice { get; private init; }

    /// <summary>
    /// Creates a successful result with the invoice.
    /// </summary>
    /// <param name="invoice">The invoice.</param>
    /// <returns>A successful result.</returns>
    public static GetInvoiceResult Success(CommissionInvoice invoice) => new()
    {
        Succeeded = true,
        Errors = [],
        Invoice = invoice
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetInvoiceResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetInvoiceResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetInvoiceResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting multiple invoices.
/// </summary>
public class GetInvoicesResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the invoices.
    /// </summary>
    public IReadOnlyList<CommissionInvoice> Invoices { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with invoices.
    /// </summary>
    /// <param name="invoices">The invoices.</param>
    /// <returns>A successful result.</returns>
    public static GetInvoicesResult Success(IReadOnlyList<CommissionInvoice> invoices) => new()
    {
        Succeeded = true,
        Errors = [],
        Invoices = invoices
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetInvoicesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetInvoicesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetInvoicesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of generating a PDF for an invoice.
/// </summary>
public class GeneratePdfResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the PDF content as bytes.
    /// </summary>
    public byte[] PdfContent { get; private init; } = [];

    /// <summary>
    /// Gets the suggested filename for the PDF.
    /// </summary>
    public string FileName { get; private init; } = string.Empty;

    /// <summary>
    /// Creates a successful result with PDF content.
    /// </summary>
    /// <param name="pdfContent">The PDF content bytes.</param>
    /// <param name="fileName">The suggested filename.</param>
    /// <returns>A successful result.</returns>
    public static GeneratePdfResult Success(byte[] pdfContent, string fileName) => new()
    {
        Succeeded = true,
        Errors = [],
        PdfContent = pdfContent,
        FileName = fileName
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GeneratePdfResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GeneratePdfResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GeneratePdfResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a credit note for an existing invoice.
/// </summary>
public class CreateCreditNoteCommand
{
    /// <summary>
    /// Gets or sets the original invoice ID to create a credit note for.
    /// </summary>
    public Guid OriginalInvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the amount to credit (must be positive, will be negated).
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// Gets or sets the reason for the credit note.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of creating a credit note.
/// </summary>
public class CreateCreditNoteResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the created credit note.
    /// </summary>
    public CommissionInvoice? CreditNote { get; private init; }

    /// <summary>
    /// Creates a successful result with the credit note.
    /// </summary>
    /// <param name="creditNote">The created credit note.</param>
    /// <returns>A successful result.</returns>
    public static CreateCreditNoteResult Success(CommissionInvoice creditNote) => new()
    {
        Succeeded = true,
        Errors = [],
        CreditNote = creditNote
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateCreditNoteResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateCreditNoteResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateCreditNoteResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
