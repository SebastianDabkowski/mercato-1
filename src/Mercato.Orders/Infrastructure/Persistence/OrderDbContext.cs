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
            entity.HasMany(e => e.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
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
    }
}
