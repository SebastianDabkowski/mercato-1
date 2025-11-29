using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for commission invoice operations.
/// </summary>
public class CommissionInvoiceService : ICommissionInvoiceService
{
    private readonly ICommissionInvoiceRepository _invoiceRepository;
    private readonly ICommissionRecordRepository _commissionRecordRepository;
    private readonly ILogger<CommissionInvoiceService> _logger;
    private readonly InvoiceSettings _invoiceSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionInvoiceService"/> class.
    /// </summary>
    /// <param name="invoiceRepository">The invoice repository.</param>
    /// <param name="commissionRecordRepository">The commission record repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="invoiceSettings">The invoice settings.</param>
    public CommissionInvoiceService(
        ICommissionInvoiceRepository invoiceRepository,
        ICommissionRecordRepository commissionRecordRepository,
        ILogger<CommissionInvoiceService> logger,
        IOptions<InvoiceSettings> invoiceSettings)
    {
        _invoiceRepository = invoiceRepository;
        _commissionRecordRepository = commissionRecordRepository;
        _logger = logger;
        _invoiceSettings = invoiceSettings.Value;
    }

    /// <inheritdoc />
    public async Task<GenerateMonthlyInvoiceResult> GenerateMonthlyInvoiceAsync(GenerateMonthlyInvoiceCommand command)
    {
        var errors = ValidateGenerateMonthlyInvoiceCommand(command);
        if (errors.Count > 0)
        {
            return GenerateMonthlyInvoiceResult.Failure(errors);
        }

        // Check if an invoice already exists for this period
        var existingInvoice = await _invoiceRepository.GetBySellerYearMonthAsync(
            command.SellerId, command.Year, command.Month, InvoiceType.Standard);

        if (existingInvoice != null)
        {
            return GenerateMonthlyInvoiceResult.Failure(
                $"An invoice already exists for {command.Year}-{command.Month:D2}.");
        }

        // Get commission records for the seller
        var allRecords = await _commissionRecordRepository.GetBySellerIdAsync(command.SellerId);

        // Filter records for the specified period
        var periodStart = new DateTimeOffset(command.Year, command.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1);

        var periodRecords = allRecords
            .Where(r => r.CalculatedAt >= periodStart && r.CalculatedAt < periodEnd)
            .ToList();

        if (periodRecords.Count == 0)
        {
            return GenerateMonthlyInvoiceResult.Failure(
                $"No commission records found for {command.Year}-{command.Month:D2}.");
        }

        var now = DateTimeOffset.UtcNow;
        var invoiceNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(command.Year);

        // Calculate totals
        var netAmount = periodRecords.Sum(r => r.NetCommissionAmount);
        var taxRate = _invoiceSettings.DefaultTaxRate;
        var taxAmount = netAmount * (taxRate / 100m);
        var grossAmount = netAmount + taxAmount;

        // Create line items
        var lineItems = periodRecords.Select(r => new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            CommissionRecordId = r.Id,
            Description = $"Commission for Order {FormatShortGuid(r.OrderId)}",
            Quantity = 1,
            UnitPrice = r.NetCommissionAmount,
            NetAmount = r.NetCommissionAmount
        }).ToList();

        var invoice = new CommissionInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            SellerId = command.SellerId,
            Year = command.Year,
            Month = command.Month,
            InvoiceType = InvoiceType.Standard,
            Status = InvoiceStatus.Issued,
            NetAmount = netAmount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            GrossAmount = grossAmount,
            Currency = _invoiceSettings.DefaultCurrency,
            IssueDate = now,
            DueDate = now.AddDays(_invoiceSettings.PaymentDueDays),
            Notes = command.Notes,
            CreatedAt = now,
            LastUpdatedAt = now,
            LineItems = lineItems
        };

        // Set invoice ID on line items
        foreach (var lineItem in lineItems)
        {
            lineItem.InvoiceId = invoice.Id;
        }

        await _invoiceRepository.AddAsync(invoice);

        _logger.LogInformation(
            "Generated invoice {InvoiceNumber} for seller {SellerId}, period {Year}-{Month:D2}, amount {GrossAmount} {Currency}",
            invoiceNumber, command.SellerId, command.Year, command.Month, grossAmount, invoice.Currency);

        return GenerateMonthlyInvoiceResult.Success(invoice);
    }

    /// <inheritdoc />
    public async Task<GetInvoiceResult> GetInvoiceByIdAsync(Guid id, Guid sellerId)
    {
        if (id == Guid.Empty)
        {
            return GetInvoiceResult.Failure("Invoice ID is required.");
        }

        if (sellerId == Guid.Empty)
        {
            return GetInvoiceResult.Failure("Seller ID is required.");
        }

        var invoice = await _invoiceRepository.GetByIdForSellerAsync(id, sellerId);
        if (invoice == null)
        {
            return GetInvoiceResult.Failure("Invoice not found.");
        }

        return GetInvoiceResult.Success(invoice);
    }

    /// <inheritdoc />
    public async Task<GetInvoicesResult> GetInvoicesBySellerIdAsync(Guid sellerId)
    {
        if (sellerId == Guid.Empty)
        {
            return GetInvoicesResult.Failure("Seller ID is required.");
        }

        var invoices = await _invoiceRepository.GetBySellerIdAsync(sellerId);
        return GetInvoicesResult.Success(invoices);
    }

    /// <inheritdoc />
    public async Task<GeneratePdfResult> GeneratePdfAsync(Guid invoiceId, Guid sellerId)
    {
        if (invoiceId == Guid.Empty)
        {
            return GeneratePdfResult.Failure("Invoice ID is required.");
        }

        if (sellerId == Guid.Empty)
        {
            return GeneratePdfResult.Failure("Seller ID is required.");
        }

        var invoice = await _invoiceRepository.GetByIdForSellerAsync(invoiceId, sellerId);
        if (invoice == null)
        {
            return GeneratePdfResult.Failure("Invoice not found.");
        }

        var pdfContent = GeneratePdfContent(invoice);
        var fileName = $"{invoice.InvoiceNumber}.pdf";

        _logger.LogInformation(
            "Generated PDF for invoice {InvoiceNumber}, seller {SellerId}",
            invoice.InvoiceNumber, sellerId);

        return GeneratePdfResult.Success(pdfContent, fileName);
    }

    /// <inheritdoc />
    public async Task<CreateCreditNoteResult> CreateCreditNoteAsync(CreateCreditNoteCommand command)
    {
        var errors = ValidateCreateCreditNoteCommand(command);
        if (errors.Count > 0)
        {
            return CreateCreditNoteResult.Failure(errors);
        }

        var originalInvoice = await _invoiceRepository.GetByIdForSellerAsync(
            command.OriginalInvoiceId, command.SellerId);

        if (originalInvoice == null)
        {
            return CreateCreditNoteResult.Failure("Original invoice not found.");
        }

        if (originalInvoice.Status == InvoiceStatus.Cancelled)
        {
            return CreateCreditNoteResult.Failure("Cannot create credit note for a cancelled invoice.");
        }

        if (command.CreditAmount > originalInvoice.GrossAmount)
        {
            return CreateCreditNoteResult.Failure("Credit amount cannot exceed the original invoice amount.");
        }

        var now = DateTimeOffset.UtcNow;
        var invoiceNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(now.Year);

        // Calculate credit note amounts (negative values)
        var creditNetAmount = -command.CreditAmount / (1 + (_invoiceSettings.DefaultTaxRate / 100m));
        var creditTaxAmount = -command.CreditAmount - creditNetAmount;

        var creditNote = new CommissionInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            SellerId = command.SellerId,
            Year = now.Year,
            Month = now.Month,
            InvoiceType = InvoiceType.CreditNote,
            Status = InvoiceStatus.Issued,
            NetAmount = creditNetAmount,
            TaxRate = _invoiceSettings.DefaultTaxRate,
            TaxAmount = creditTaxAmount,
            GrossAmount = -command.CreditAmount,
            Currency = originalInvoice.Currency,
            IssueDate = now,
            DueDate = now,
            OriginalInvoiceId = command.OriginalInvoiceId,
            Notes = command.Reason,
            CreatedAt = now,
            LastUpdatedAt = now,
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = $"Credit note for invoice {originalInvoice.InvoiceNumber}: {command.Reason}",
                    Quantity = 1,
                    UnitPrice = creditNetAmount,
                    NetAmount = creditNetAmount
                }
            ]
        };

        // Set invoice ID on line items
        foreach (var lineItem in creditNote.LineItems)
        {
            lineItem.InvoiceId = creditNote.Id;
        }

        // Update original invoice status if fully credited
        if (command.CreditAmount >= originalInvoice.GrossAmount)
        {
            originalInvoice.Status = InvoiceStatus.Corrected;
            originalInvoice.LastUpdatedAt = now;
            await _invoiceRepository.UpdateAsync(originalInvoice);
        }

        await _invoiceRepository.AddAsync(creditNote);

        _logger.LogInformation(
            "Created credit note {CreditNoteNumber} for invoice {OriginalInvoiceNumber}, amount {CreditAmount}",
            creditNote.InvoiceNumber, originalInvoice.InvoiceNumber, command.CreditAmount);

        return CreateCreditNoteResult.Success(creditNote);
    }

    private static List<string> ValidateGenerateMonthlyInvoiceCommand(GenerateMonthlyInvoiceCommand command)
    {
        var errors = new List<string>();

        if (command.SellerId == Guid.Empty)
        {
            errors.Add("Seller ID is required.");
        }

        if (command.Year < 2000 || command.Year > 2100)
        {
            errors.Add("Year must be between 2000 and 2100.");
        }

        if (command.Month < 1 || command.Month > 12)
        {
            errors.Add("Month must be between 1 and 12.");
        }

        return errors;
    }

    private static List<string> ValidateCreateCreditNoteCommand(CreateCreditNoteCommand command)
    {
        var errors = new List<string>();

        if (command.OriginalInvoiceId == Guid.Empty)
        {
            errors.Add("Original invoice ID is required.");
        }

        if (command.SellerId == Guid.Empty)
        {
            errors.Add("Seller ID is required.");
        }

        if (command.CreditAmount <= 0)
        {
            errors.Add("Credit amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Reason is required for credit notes.");
        }

        return errors;
    }

    private static string FormatShortGuid(Guid id)
    {
        const int shortIdLength = 8;
        var idString = id.ToString();
        return idString.Length > shortIdLength
            ? $"{idString[..shortIdLength]}..."
            : idString;
    }

    private static byte[] GeneratePdfContent(CommissionInvoice invoice)
    {
        // TODO: Replace with a proper PDF library (e.g., iTextSharp, PdfSharpCore, or QuestPDF) for production use.
        // This implementation generates a minimal valid PDF structure for demonstration purposes.
        var sb = new StringBuilder();

        // PDF Header
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
        sb.AppendLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");
        sb.AppendLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj");

        // Build content
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 18 Tf");
        content.AppendLine("50 750 Td");
        content.AppendLine($"(COMMISSION INVOICE) Tj");
        content.AppendLine("/F1 12 Tf");
        content.AppendLine("0 -30 Td");
        content.AppendLine($"(Invoice Number: {invoice.InvoiceNumber}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Issue Date: {invoice.IssueDate:yyyy-MM-dd}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Due Date: {invoice.DueDate:yyyy-MM-dd}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Period: {invoice.Year}-{invoice.Month:D2}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Status: {invoice.Status}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Type: {invoice.InvoiceType}) Tj");
        content.AppendLine("0 -40 Td");
        content.AppendLine("(--- Line Items ---) Tj");

        var lineY = 0;
        foreach (var item in invoice.LineItems)
        {
            lineY -= 20;
            content.AppendLine($"0 {lineY} Td");
            content.AppendLine($"({item.Description}: {item.NetAmount:N2} {invoice.Currency}) Tj");
            lineY = 0;
        }

        content.AppendLine("0 -40 Td");
        content.AppendLine("(--- Summary ---) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Net Amount: {invoice.NetAmount:N2} {invoice.Currency}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Tax Rate: {invoice.TaxRate:N2}%) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Tax Amount: {invoice.TaxAmount:N2} {invoice.Currency}) Tj");
        content.AppendLine("0 -20 Td");
        content.AppendLine($"(Gross Amount: {invoice.GrossAmount:N2} {invoice.Currency}) Tj");

        if (!string.IsNullOrEmpty(invoice.Notes))
        {
            content.AppendLine("0 -40 Td");
            content.AppendLine($"(Notes: {invoice.Notes}) Tj");
        }

        content.AppendLine("ET");

        var contentBytes = Encoding.ASCII.GetBytes(content.ToString());
        var streamLength = contentBytes.Length;

        sb.AppendLine($"4 0 obj << /Length {streamLength} >> stream");
        sb.Append(content.ToString());
        sb.AppendLine("endstream endobj");
        sb.AppendLine("5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");
        sb.AppendLine("xref");
        sb.AppendLine("0 6");
        sb.AppendLine("0000000000 65535 f ");
        sb.AppendLine("0000000009 00000 n ");
        sb.AppendLine("0000000058 00000 n ");
        sb.AppendLine("0000000115 00000 n ");
        sb.AppendLine("0000000268 00000 n ");
        // Note: Offset values are approximate for this basic PDF structure.
        // A production implementation should calculate exact byte offsets.
        const int baseOffset = 350;
        const int trailerOffset = 400;
        sb.AppendLine($"0000000{baseOffset + streamLength:D3} 00000 n ");
        sb.AppendLine("trailer << /Size 6 /Root 1 0 R >>");
        sb.AppendLine("startxref");
        sb.AppendLine($"{trailerOffset + streamLength}");
        sb.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
