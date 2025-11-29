using Mercato.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Payments module.
/// </summary>
public class PaymentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the escrow entries.
    /// </summary>
    public DbSet<EscrowEntry> EscrowEntries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commission rules.
    /// </summary>
    public DbSet<CommissionRule> CommissionRules { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commission records.
    /// </summary>
    public DbSet<CommissionRecord> CommissionRecords { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payouts.
    /// </summary>
    public DbSet<Payout> Payouts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlements.
    /// </summary>
    public DbSet<Settlement> Settlements { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlement line items.
    /// </summary>
    public DbSet<SettlementLineItem> SettlementLineItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commission invoices.
    /// </summary>
    public DbSet<CommissionInvoice> CommissionInvoices { get; set; } = null!;

    /// <summary>
    /// Gets or sets the invoice line items.
    /// </summary>
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the refunds.
    /// </summary>
    public DbSet<Refund> Refunds { get; set; } = null!;

    // TODO: Define DbSet<Transaction> when Transaction entity is implemented
    // public DbSet<Transaction> Transactions { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure EscrowEntry entity
        modelBuilder.Entity<EscrowEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.RefundedAmount)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();
            
            entity.Property(e => e.AuditNote)
                .HasMaxLength(500);

            // RemainingAmount is a calculated property, ignore it
            entity.Ignore(e => e.RemainingAmount);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.PaymentTransactionId);
            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => new { e.SellerId, e.Status });
        });

        // Configure CommissionRule entity
        modelBuilder.Entity<CommissionRule>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CommissionRate)
                .HasPrecision(18, 4);

            entity.Property(e => e.MinCommission)
                .HasPrecision(18, 4);

            entity.Property(e => e.MaxCommission)
                .HasPrecision(18, 4);

            entity.Property(e => e.CategoryId)
                .HasMaxLength(100);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => new { e.SellerId, e.CategoryId, e.IsActive });
        });

        // Configure CommissionRecord entity
        modelBuilder.Entity<CommissionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderAmount)
                .HasPrecision(18, 4);

            entity.Property(e => e.CommissionRate)
                .HasPrecision(18, 4);

            entity.Property(e => e.CommissionAmount)
                .HasPrecision(18, 4);

            entity.Property(e => e.RefundedAmount)
                .HasPrecision(18, 4);

            entity.Property(e => e.RefundedCommissionAmount)
                .HasPrecision(18, 4);

            entity.Property(e => e.NetCommissionAmount)
                .HasPrecision(18, 4);

            entity.Property(e => e.AppliedRuleDescription)
                .HasMaxLength(500);

            entity.HasIndex(e => e.PaymentTransactionId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => new { e.OrderId, e.SellerId });
        });

        // Configure Payout entity
        modelBuilder.Entity<Payout>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.ErrorReference)
                .HasMaxLength(100);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            entity.Property(e => e.ExternalReference)
                .HasMaxLength(200);

            entity.Property(e => e.AuditNote)
                .HasMaxLength(500);

            entity.Property(e => e.EscrowEntryIds)
                .HasMaxLength(4000);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.SellerId, e.Status });
            entity.HasIndex(e => new { e.Status, e.ScheduledAt });
        });

        // Configure Settlement entity
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.GrossSales)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalRefunds)
                .HasPrecision(18, 2);

            entity.Property(e => e.NetSales)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalCommission)
                .HasPrecision(18, 2);

            entity.Property(e => e.PreviousMonthAdjustments)
                .HasPrecision(18, 2);

            entity.Property(e => e.NetPayable)
                .HasPrecision(18, 2);

            entity.Property(e => e.AuditNotes)
                .HasMaxLength(4000);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Year, e.Month });
            entity.HasIndex(e => new { e.SellerId, e.Year, e.Month }).IsUnique();

            entity.HasMany(e => e.LineItems)
                .WithOne(li => li.Settlement)
                .HasForeignKey(li => li.SettlementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SettlementLineItem entity
        modelBuilder.Entity<SettlementLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderNumber)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.GrossAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.RefundAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.NetAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.CommissionAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.AdjustmentNotes)
                .HasMaxLength(500);

            entity.HasIndex(e => e.SettlementId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => new { e.SettlementId, e.OrderId });
        });

        // Configure CommissionInvoice entity
        modelBuilder.Entity<CommissionInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.NetAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.TaxRate)
                .HasPrecision(18, 4);

            entity.Property(e => e.TaxAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.GrossAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Year, e.Month });
            entity.HasIndex(e => new { e.SellerId, e.Year, e.Month, e.InvoiceType });

            entity.HasMany(e => e.LineItems)
                .WithOne(li => li.Invoice)
                .HasForeignKey(li => li.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure InvoiceLineItem entity
        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.NetAmount)
                .HasPrecision(18, 2);

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.CommissionRecordId);
        });

        // Configure Refund entity
        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.ExternalReferenceId)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000);

            entity.Property(e => e.InitiatedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.InitiatedByRole)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CommissionRefunded)
                .HasPrecision(18, 2);

            entity.Property(e => e.EscrowRefunded)
                .HasPrecision(18, 2);

            entity.Property(e => e.AuditNote)
                .HasMaxLength(500);

            entity.HasIndex(e => e.PaymentTransactionId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.OrderId, e.SellerId });
            entity.HasIndex(e => new { e.OrderId, e.Status });
        });

        // TODO: Configure other entity mappings when entities are defined
    }
}
