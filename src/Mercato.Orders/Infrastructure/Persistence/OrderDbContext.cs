using Mercato.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Orders module.
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the orders DbSet.
    /// </summary>
    public DbSet<Order> Orders { get; set; }

    /// <summary>
    /// Gets or sets the order items DbSet.
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-orders DbSet.
    /// </summary>
    public DbSet<SellerSubOrder> SellerSubOrders { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order items DbSet.
    /// </summary>
    public DbSet<SellerSubOrderItem> SellerSubOrderItems { get; set; }

    /// <summary>
    /// Gets or sets the return requests DbSet.
    /// </summary>
    public DbSet<ReturnRequest> ReturnRequests { get; set; }

    /// <summary>
    /// Gets or sets the case items DbSet.
    /// </summary>
    public DbSet<CaseItem> CaseItems { get; set; }

    /// <summary>
    /// Gets or sets the case messages DbSet.
    /// </summary>
    public DbSet<CaseMessage> CaseMessages { get; set; }

    /// <summary>
    /// Gets or sets the shipping status histories DbSet.
    /// </summary>
    public DbSet<ShippingStatusHistory> ShippingStatusHistories { get; set; }

    /// <summary>
    /// Gets or sets the case status histories DbSet.
    /// </summary>
    public DbSet<CaseStatusHistory> CaseStatusHistories { get; set; }

    /// <summary>
    /// Gets or sets the product reviews DbSet.
    /// </summary>
    public DbSet<ProductReview> ProductReviews { get; set; }

    /// <summary>
    /// Gets or sets the seller ratings DbSet.
    /// </summary>
    public DbSet<SellerRating> SellerRatings { get; set; }

    /// <summary>
    /// Gets or sets the review reports DbSet.
    /// </summary>
    public DbSet<ReviewReport> ReviewReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BuyerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.BuyerId);
            entity.HasIndex(e => e.PaymentTransactionId);
            // Composite index for buyer order filtering (status, date range)
            entity.HasIndex(e => new { e.BuyerId, e.CreatedAt, e.Status })
                .HasDatabaseName("IX_Orders_BuyerId_CreatedAt_Status");
            entity.Property(e => e.ItemsSubtotal).HasPrecision(18, 2);
            entity.Property(e => e.ShippingTotal).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.DeliveryFullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DeliveryAddressLine1).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DeliveryAddressLine2).HasMaxLength(500);
            entity.Property(e => e.DeliveryCity).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DeliveryState).HasMaxLength(100);
            entity.Property(e => e.DeliveryPostalCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DeliveryCountry).IsRequired().HasMaxLength(2);
            entity.Property(e => e.DeliveryPhoneNumber).HasMaxLength(30);
            entity.Property(e => e.BuyerEmail).HasMaxLength(256);
            entity.Property(e => e.DeliveryInstructions).HasMaxLength(1000);
            entity.HasMany(e => e.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
            entity.HasMany(e => e.SellerSubOrders).WithOne(s => s.Order).HasForeignKey(s => s.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.StoreName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductImageUrl).HasMaxLength(500);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
        });

        modelBuilder.Entity<SellerSubOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubOrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.SubOrderNumber).IsUnique();
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.StoreId);
            entity.Property(e => e.StoreName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ItemsSubtotal).HasPrecision(18, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.ShippingCarrier).HasMaxLength(100);
            entity.Property(e => e.ShippingMethodName).HasMaxLength(100);
            entity.HasMany(e => e.Items).WithOne(i => i.SellerSubOrder).HasForeignKey(i => i.SellerSubOrderId);
            // Ignore computed properties
            entity.Ignore(e => e.CancelledItemsTotal);
            entity.Ignore(e => e.ShippedItemsTotal);
            entity.Ignore(e => e.IsPartiallyFulfilled);
        });

        modelBuilder.Entity<SellerSubOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.ProductImageUrl).HasMaxLength(500);
            entity.HasIndex(e => e.SellerSubOrderId);
            entity.HasIndex(e => e.ProductId);
            // Composite index for partial fulfillment status queries
            entity.HasIndex(e => new { e.SellerSubOrderId, e.Status })
                .HasDatabaseName("IX_SellerSubOrderItems_SubOrderId_Status");
            // Ignore computed properties
            entity.Ignore(e => e.TotalPrice);
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CaseNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.CaseNumber).IsUnique();
            entity.Property(e => e.BuyerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SellerNotes).HasMaxLength(2000);
            entity.Property(e => e.ResolutionReason).HasMaxLength(2000);
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.LastActivityByUserId).HasMaxLength(450);
            entity.Property(e => e.EscalatedByUserId).HasMaxLength(450);
            entity.Property(e => e.EscalationReason).HasMaxLength(2000);
            entity.Property(e => e.AdminDecision).HasMaxLength(100);
            entity.Property(e => e.AdminDecisionReason).HasMaxLength(2000);
            entity.Property(e => e.AdminDecisionByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.SellerSubOrderId);
            entity.HasIndex(e => e.BuyerId);
            entity.HasIndex(e => e.LinkedRefundId);
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ReturnRequests_Status");
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderId);
            entity.HasMany(e => e.CaseItems)
                .WithOne(ci => ci.ReturnRequest)
                .HasForeignKey(ci => ci.ReturnRequestId);
            entity.HasMany(e => e.Messages)
                .WithOne(m => m.ReturnRequest)
                .HasForeignKey(m => m.ReturnRequestId);
            entity.HasMany(e => e.StatusHistory)
                .WithOne(h => h.ReturnRequest)
                .HasForeignKey(h => h.ReturnRequestId);
            // Ignore computed properties
            entity.Ignore(e => e.HasSelectedItems);
        });

        modelBuilder.Entity<CaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReturnRequestId);
            entity.HasIndex(e => e.SellerSubOrderItemId);
            // Composite index to efficiently check for existing cases on items
            entity.HasIndex(e => new { e.SellerSubOrderItemId, e.ReturnRequestId })
                .HasDatabaseName("IX_CaseItems_ItemId_RequestId");
            entity.HasOne(e => e.SellerSubOrderItem)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderItemId);
        });

        modelBuilder.Entity<CaseMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.SenderRole).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.ReturnRequestId);
            entity.HasIndex(e => new { e.ReturnRequestId, e.CreatedAt })
                .HasDatabaseName("IX_CaseMessages_RequestId_CreatedAt");
        });

        modelBuilder.Entity<ShippingStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangedByUserId).HasMaxLength(450);
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.ShippingCarrier).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.SellerSubOrderId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderId);
        });

        modelBuilder.Entity<CaseStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangedByUserId).HasMaxLength(450);
            entity.Property(e => e.ChangedByRole).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.ReturnRequestId);
            entity.HasIndex(e => new { e.ReturnRequestId, e.ChangedAt })
                .HasDatabaseName("IX_CaseStatusHistories_RequestId_ChangedAt");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BuyerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ReviewText).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.BuyerId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.SellerSubOrderItemId).IsUnique();
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ProductReviews_Status");
            entity.HasIndex(e => new { e.ProductId, e.Status })
                .HasDatabaseName("IX_ProductReviews_ProductId_Status");
            entity.HasOne(e => e.SellerSubOrderItem)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderItemId);
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderId);
        });

        modelBuilder.Entity<SellerRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BuyerId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.BuyerId);
            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.SellerSubOrderId).IsUnique();
            entity.HasIndex(e => new { e.StoreId, e.CreatedAt })
                .HasDatabaseName("IX_SellerRatings_StoreId_CreatedAt");
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderId);
        });

        modelBuilder.Entity<ReviewReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReporterId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.AdditionalDetails).HasMaxLength(1000);
            entity.HasIndex(e => e.ReviewId);
            entity.HasIndex(e => e.ReporterId);
            // Unique constraint to prevent duplicate reports by the same user
            entity.HasIndex(e => new { e.ReviewId, e.ReporterId })
                .IsUnique()
                .HasDatabaseName("IX_ReviewReports_ReviewId_ReporterId");
            entity.HasOne(e => e.Review)
                .WithMany()
                .HasForeignKey(e => e.ReviewId);
        });
    }
}
