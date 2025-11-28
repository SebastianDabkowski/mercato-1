using Mercato.Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Cart.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Cart module.
/// </summary>
public class CartDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CartDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public CartDbContext(DbContextOptions<CartDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the carts DbSet.
    /// </summary>
    public DbSet<Domain.Entities.Cart> Carts { get; set; }

    /// <summary>
    /// Gets or sets the cart items DbSet.
    /// </summary>
    public DbSet<CartItem> CartItems { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCart(modelBuilder);
        ConfigureCartItem(modelBuilder);
    }

    private static void ConfigureCart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Entities.Cart>(entity =>
        {
            entity.ToTable("Carts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.BuyerId)
                .HasMaxLength(450);

            entity.Property(e => e.GuestCartId)
                .HasMaxLength(450);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            // Index for querying carts by buyer (now nullable and non-unique to allow guest carts)
            entity.HasIndex(e => e.BuyerId)
                .HasFilter("[BuyerId] IS NOT NULL")
                .IsUnique();

            // Index for querying carts by guest cart ID
            entity.HasIndex(e => e.GuestCartId)
                .HasFilter("[GuestCartId] IS NOT NULL")
                .IsUnique();

            // Relationship to cart items
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Cart)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCartItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.CartId)
                .IsRequired();

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.StoreId)
                .IsRequired();

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.ProductTitle)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ProductPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(e => e.ProductImageUrl)
                .HasMaxLength(500);

            entity.Property(e => e.StoreName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            // Index for querying cart items by cart
            entity.HasIndex(e => e.CartId);

            // Index for querying cart items by product
            entity.HasIndex(e => new { e.CartId, e.ProductId })
                .IsUnique();

            // Index for querying cart items by store (for grouping)
            entity.HasIndex(e => new { e.CartId, e.StoreId });
        });
    }
}
