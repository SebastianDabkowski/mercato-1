using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Payments;

public class CommissionInvoiceServiceTests
{
    private readonly Mock<ICommissionInvoiceRepository> _mockInvoiceRepository;
    private readonly Mock<ICommissionRecordRepository> _mockRecordRepository;
    private readonly Mock<ILogger<CommissionInvoiceService>> _mockLogger;

    public CommissionInvoiceServiceTests()
    {
        _mockInvoiceRepository = new Mock<ICommissionInvoiceRepository>(MockBehavior.Strict);
        _mockRecordRepository = new Mock<ICommissionRecordRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CommissionInvoiceService>>();
    }

    private CommissionInvoiceService CreateService(InvoiceSettings? settings = null)
    {
        var invoiceSettings = settings ?? new InvoiceSettings();
        var options = Options.Create(invoiceSettings);
        return new CommissionInvoiceService(
            _mockInvoiceRepository.Object,
            _mockRecordRepository.Object,
            _mockLogger.Object,
            options);
    }

    #region GenerateMonthlyInvoiceAsync Tests

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_ValidCommand_GeneratesInvoice()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = sellerId,
            Year = 2024,
            Month = 1
        };

        var commissionRecords = new List<CommissionRecord>
        {
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                OrderId = orderId,
                NetCommissionAmount = 50.00m,
                CalculatedAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero)
            },
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                OrderId = Guid.NewGuid(),
                NetCommissionAmount = 75.00m,
                CalculatedAt = new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero)
            }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetBySellerYearMonthAsync(sellerId, 2024, 1, InvoiceType.Standard))
            .ReturnsAsync((CommissionInvoice?)null);

        _mockRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(commissionRecords);

        _mockInvoiceRepository
            .Setup(r => r.GetNextInvoiceNumberAsync(2024))
            .ReturnsAsync("INV-2024-000001");

        _mockInvoiceRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionInvoice>()))
            .ReturnsAsync((CommissionInvoice i) => i);

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Invoice);
        Assert.Equal("INV-2024-000001", result.Invoice.InvoiceNumber);
        Assert.Equal(sellerId, result.Invoice.SellerId);
        Assert.Equal(2024, result.Invoice.Year);
        Assert.Equal(1, result.Invoice.Month);
        Assert.Equal(InvoiceType.Standard, result.Invoice.InvoiceType);
        Assert.Equal(InvoiceStatus.Issued, result.Invoice.Status);
        Assert.Equal(125.00m, result.Invoice.NetAmount); // 50 + 75
        Assert.Equal(125.00m, result.Invoice.GrossAmount); // No tax by default
        Assert.Equal(2, result.Invoice.LineItems.Count);
    }

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_WithTaxRate_CalculatesTaxCorrectly()
    {
        // Arrange
        var settings = new InvoiceSettings { DefaultTaxRate = 20.0m };
        var service = CreateService(settings);
        var sellerId = Guid.NewGuid();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = sellerId,
            Year = 2024,
            Month = 1
        };

        var commissionRecords = new List<CommissionRecord>
        {
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                NetCommissionAmount = 100.00m,
                CalculatedAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero)
            }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetBySellerYearMonthAsync(sellerId, 2024, 1, InvoiceType.Standard))
            .ReturnsAsync((CommissionInvoice?)null);

        _mockRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(commissionRecords);

        _mockInvoiceRepository
            .Setup(r => r.GetNextInvoiceNumberAsync(2024))
            .ReturnsAsync("INV-2024-000001");

        _mockInvoiceRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionInvoice>()))
            .ReturnsAsync((CommissionInvoice i) => i);

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Invoice);
        Assert.Equal(100.00m, result.Invoice.NetAmount);
        Assert.Equal(20.0m, result.Invoice.TaxRate);
        Assert.Equal(20.00m, result.Invoice.TaxAmount); // 100 * 20%
        Assert.Equal(120.00m, result.Invoice.GrossAmount); // 100 + 20
    }

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_ExistingInvoice_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = sellerId,
            Year = 2024,
            Month = 1
        };

        var existingInvoice = new CommissionInvoice
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Year = 2024,
            Month = 1,
            InvoiceType = InvoiceType.Standard
        };

        _mockInvoiceRepository
            .Setup(r => r.GetBySellerYearMonthAsync(sellerId, 2024, 1, InvoiceType.Standard))
            .ReturnsAsync(existingInvoice);

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_NoCommissionRecords_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = sellerId,
            Year = 2024,
            Month = 1
        };

        _mockInvoiceRepository
            .Setup(r => r.GetBySellerYearMonthAsync(sellerId, 2024, 1, InvoiceType.Standard))
            .ReturnsAsync((CommissionInvoice?)null);

        _mockRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(new List<CommissionRecord>());

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No commission records"));
    }

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = Guid.Empty,
            Year = 2024,
            Month = 1
        };

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_InvalidYear_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = Guid.NewGuid(),
            Year = 1999,
            Month = 1
        };

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Year must be between 2000 and 2100"));
    }

    [Fact]
    public async Task GenerateMonthlyInvoiceAsync_InvalidMonth_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new GenerateMonthlyInvoiceCommand
        {
            SellerId = Guid.NewGuid(),
            Year = 2024,
            Month = 13
        };

        // Act
        var result = await service.GenerateMonthlyInvoiceAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Month must be between 1 and 12"));
    }

    #endregion

    #region GetInvoiceByIdAsync Tests

    [Fact]
    public async Task GetInvoiceByIdAsync_ValidIds_ReturnsInvoice()
    {
        // Arrange
        var service = CreateService();
        var invoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var invoice = new CommissionInvoice
        {
            Id = invoiceId,
            SellerId = sellerId,
            InvoiceNumber = "INV-2024-000001"
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(invoiceId, sellerId))
            .ReturnsAsync(invoice);

        // Act
        var result = await service.GetInvoiceByIdAsync(invoiceId, sellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Invoice);
        Assert.Equal(invoiceId, result.Invoice.Id);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_EmptyInvoiceId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetInvoiceByIdAsync(Guid.Empty, Guid.NewGuid());

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Invoice ID is required"));
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetInvoiceByIdAsync(Guid.NewGuid(), Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_InvoiceNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var invoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(invoiceId, sellerId))
            .ReturnsAsync((CommissionInvoice?)null);

        // Act
        var result = await service.GetInvoiceByIdAsync(invoiceId, sellerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Invoice not found"));
    }

    #endregion

    #region GetInvoicesBySellerIdAsync Tests

    [Fact]
    public async Task GetInvoicesBySellerIdAsync_ValidSellerId_ReturnsInvoices()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var invoices = new List<CommissionInvoice>
        {
            new CommissionInvoice { Id = Guid.NewGuid(), SellerId = sellerId },
            new CommissionInvoice { Id = Guid.NewGuid(), SellerId = sellerId }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(invoices);

        // Act
        var result = await service.GetInvoicesBySellerIdAsync(sellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Invoices.Count);
    }

    [Fact]
    public async Task GetInvoicesBySellerIdAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetInvoicesBySellerIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    #endregion

    #region GeneratePdfAsync Tests

    [Fact]
    public async Task GeneratePdfAsync_ValidInvoice_ReturnsPdf()
    {
        // Arrange
        var service = CreateService();
        var invoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var invoice = new CommissionInvoice
        {
            Id = invoiceId,
            SellerId = sellerId,
            InvoiceNumber = "INV-2024-000001",
            Year = 2024,
            Month = 1,
            NetAmount = 100.00m,
            TaxAmount = 0m,
            GrossAmount = 100.00m,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
            Status = InvoiceStatus.Issued,
            InvoiceType = InvoiceType.Standard,
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Test commission",
                    NetAmount = 100.00m
                }
            }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(invoiceId, sellerId))
            .ReturnsAsync(invoice);

        // Act
        var result = await service.GeneratePdfAsync(invoiceId, sellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.PdfContent);
        Assert.True(result.PdfContent.Length > 0);
        Assert.Equal("INV-2024-000001.pdf", result.FileName);
    }

    [Fact]
    public async Task GeneratePdfAsync_EmptyInvoiceId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GeneratePdfAsync(Guid.Empty, Guid.NewGuid());

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Invoice ID is required"));
    }

    [Fact]
    public async Task GeneratePdfAsync_InvoiceNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var invoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(invoiceId, sellerId))
            .ReturnsAsync((CommissionInvoice?)null);

        // Act
        var result = await service.GeneratePdfAsync(invoiceId, sellerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Invoice not found"));
    }

    #endregion

    #region CreateCreditNoteAsync Tests

    [Fact]
    public async Task CreateCreditNoteAsync_ValidCommand_CreatesCreditNote()
    {
        // Arrange
        var service = CreateService();
        var originalInvoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = originalInvoiceId,
            SellerId = sellerId,
            CreditAmount = 50.00m,
            Reason = "Refund for cancelled order"
        };

        var originalInvoice = new CommissionInvoice
        {
            Id = originalInvoiceId,
            SellerId = sellerId,
            InvoiceNumber = "INV-2024-000001",
            GrossAmount = 100.00m,
            Currency = "USD",
            Status = InvoiceStatus.Issued
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(originalInvoiceId, sellerId))
            .ReturnsAsync(originalInvoice);

        _mockInvoiceRepository
            .Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<int>()))
            .ReturnsAsync("INV-2024-000002");

        _mockInvoiceRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionInvoice>()))
            .ReturnsAsync((CommissionInvoice i) => i);

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.CreditNote);
        Assert.Equal(InvoiceType.CreditNote, result.CreditNote.InvoiceType);
        Assert.Equal(-50.00m, result.CreditNote.GrossAmount);
        Assert.Equal(originalInvoiceId, result.CreditNote.OriginalInvoiceId);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_FullCredit_UpdatesOriginalInvoice()
    {
        // Arrange
        var service = CreateService();
        var originalInvoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = originalInvoiceId,
            SellerId = sellerId,
            CreditAmount = 100.00m,
            Reason = "Full refund"
        };

        var originalInvoice = new CommissionInvoice
        {
            Id = originalInvoiceId,
            SellerId = sellerId,
            InvoiceNumber = "INV-2024-000001",
            GrossAmount = 100.00m,
            Currency = "USD",
            Status = InvoiceStatus.Issued
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(originalInvoiceId, sellerId))
            .ReturnsAsync(originalInvoice);

        _mockInvoiceRepository
            .Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<int>()))
            .ReturnsAsync("INV-2024-000002");

        _mockInvoiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionInvoice>()))
            .Returns(Task.CompletedTask);

        _mockInvoiceRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionInvoice>()))
            .ReturnsAsync((CommissionInvoice i) => i);

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.Is<CommissionInvoice>(
            i => i.Status == InvoiceStatus.Corrected)), Times.Once);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_CreditExceedsOriginal_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var originalInvoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = originalInvoiceId,
            SellerId = sellerId,
            CreditAmount = 150.00m,
            Reason = "Excessive refund"
        };

        var originalInvoice = new CommissionInvoice
        {
            Id = originalInvoiceId,
            SellerId = sellerId,
            GrossAmount = 100.00m,
            Status = InvoiceStatus.Issued
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(originalInvoiceId, sellerId))
            .ReturnsAsync(originalInvoice);

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("cannot exceed"));
    }

    [Fact]
    public async Task CreateCreditNoteAsync_CancelledInvoice_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var originalInvoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = originalInvoiceId,
            SellerId = sellerId,
            CreditAmount = 50.00m,
            Reason = "Test"
        };

        var originalInvoice = new CommissionInvoice
        {
            Id = originalInvoiceId,
            SellerId = sellerId,
            GrossAmount = 100.00m,
            Status = InvoiceStatus.Cancelled
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(originalInvoiceId, sellerId))
            .ReturnsAsync(originalInvoice);

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("cancelled"));
    }

    [Fact]
    public async Task CreateCreditNoteAsync_EmptyOriginalInvoiceId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = Guid.Empty,
            SellerId = Guid.NewGuid(),
            CreditAmount = 50.00m,
            Reason = "Test"
        };

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Original invoice ID is required"));
    }

    [Fact]
    public async Task CreateCreditNoteAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = Guid.NewGuid(),
            SellerId = Guid.Empty,
            CreditAmount = 50.00m,
            Reason = "Test"
        };

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task CreateCreditNoteAsync_ZeroCreditAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CreditAmount = 0m,
            Reason = "Test"
        };

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Credit amount must be greater than zero"));
    }

    [Fact]
    public async Task CreateCreditNoteAsync_EmptyReason_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CreditAmount = 50.00m,
            Reason = ""
        };

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Reason is required"));
    }

    [Fact]
    public async Task CreateCreditNoteAsync_OriginalInvoiceNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var originalInvoiceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var command = new CreateCreditNoteCommand
        {
            OriginalInvoiceId = originalInvoiceId,
            SellerId = sellerId,
            CreditAmount = 50.00m,
            Reason = "Test"
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdForSellerAsync(originalInvoiceId, sellerId))
            .ReturnsAsync((CommissionInvoice?)null);

        // Act
        var result = await service.CreateCreditNoteAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Original invoice not found"));
    }

    #endregion
}
