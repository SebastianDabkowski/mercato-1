namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for bulk updating price and/or stock for multiple products.
/// </summary>
public class BulkUpdatePriceStockCommand
{
    /// <summary>
    /// Gets or sets the seller ID performing the update (for authorization and audit).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns these products (for authorization).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the list of product IDs to update.
    /// </summary>
    public IReadOnlyList<Guid> ProductIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the price update configuration. Null if price should not be updated.
    /// </summary>
    public BulkPriceUpdate? PriceUpdate { get; set; }

    /// <summary>
    /// Gets or sets the stock update configuration. Null if stock should not be updated.
    /// </summary>
    public BulkStockUpdate? StockUpdate { get; set; }
}

/// <summary>
/// Configuration for bulk price updates.
/// </summary>
public class BulkPriceUpdate
{
    /// <summary>
    /// Gets or sets the type of price update to perform.
    /// </summary>
    public BulkPriceUpdateType UpdateType { get; set; }

    /// <summary>
    /// Gets or sets the value to use for the update.
    /// For Fixed: the new price.
    /// For PercentageIncrease/PercentageDecrease: the percentage (e.g., 10 for 10%).
    /// For AmountIncrease/AmountDecrease: the amount to add/subtract.
    /// </summary>
    public decimal Value { get; set; }
}

/// <summary>
/// Configuration for bulk stock updates.
/// </summary>
public class BulkStockUpdate
{
    /// <summary>
    /// Gets or sets the type of stock update to perform.
    /// </summary>
    public BulkStockUpdateType UpdateType { get; set; }

    /// <summary>
    /// Gets or sets the value to use for the update.
    /// For Fixed: the new stock value.
    /// For Increase/Decrease: the amount to add/subtract.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Types of bulk price updates.
/// </summary>
public enum BulkPriceUpdateType
{
    /// <summary>
    /// Set price to a fixed value.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Increase price by a percentage.
    /// </summary>
    PercentageIncrease = 1,

    /// <summary>
    /// Decrease price by a percentage.
    /// </summary>
    PercentageDecrease = 2,

    /// <summary>
    /// Increase price by a fixed amount.
    /// </summary>
    AmountIncrease = 3,

    /// <summary>
    /// Decrease price by a fixed amount.
    /// </summary>
    AmountDecrease = 4
}

/// <summary>
/// Types of bulk stock updates.
/// </summary>
public enum BulkStockUpdateType
{
    /// <summary>
    /// Set stock to a fixed value.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Increase stock by an amount.
    /// </summary>
    Increase = 1,

    /// <summary>
    /// Decrease stock by an amount.
    /// </summary>
    Decrease = 2
}
