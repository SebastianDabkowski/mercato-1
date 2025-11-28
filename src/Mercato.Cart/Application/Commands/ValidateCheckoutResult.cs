namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Result of checkout validation including stock and price checks.
/// </summary>
public class ValidateCheckoutResult
{
    /// <summary>
    /// Gets a value indicating whether the validation succeeded (all items valid).
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of general errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the list of stock validation issues.
    /// </summary>
    public IReadOnlyList<StockValidationIssue> StockIssues { get; private init; } = [];

    /// <summary>
    /// Gets the list of price change issues.
    /// </summary>
    public IReadOnlyList<PriceChangeIssue> PriceChanges { get; private init; } = [];

    /// <summary>
    /// Gets the validated cart items with current prices and availability.
    /// Only populated when validation succeeds.
    /// </summary>
    public IReadOnlyList<ValidatedCartItem> ValidatedItems { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether there are stock issues.
    /// </summary>
    public bool HasStockIssues => StockIssues.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there are price changes.
    /// </summary>
    public bool HasPriceChanges => PriceChanges.Count > 0;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="validatedItems">The validated cart items.</param>
    /// <returns>A successful result.</returns>
    public static ValidateCheckoutResult Success(IReadOnlyList<ValidatedCartItem> validatedItems) => new()
    {
        Succeeded = true,
        Errors = [],
        ValidatedItems = validatedItems
    };

    /// <summary>
    /// Creates a failed result with stock and/or price issues.
    /// </summary>
    /// <param name="stockIssues">The stock validation issues.</param>
    /// <param name="priceChanges">The price change issues.</param>
    /// <returns>A failed result.</returns>
    public static ValidateCheckoutResult ValidationFailed(
        IReadOnlyList<StockValidationIssue> stockIssues,
        IReadOnlyList<PriceChangeIssue> priceChanges) => new()
    {
        Succeeded = false,
        Errors = [],
        StockIssues = stockIssues,
        PriceChanges = priceChanges
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ValidateCheckoutResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ValidateCheckoutResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Represents a stock validation issue for a cart item.
/// </summary>
public class StockValidationIssue
{
    /// <summary>
    /// Gets or sets the cart item ID.
    /// </summary>
    public Guid CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requested quantity in the cart.
    /// </summary>
    public int RequestedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the current available stock.
    /// </summary>
    public int AvailableStock { get; set; }

    /// <summary>
    /// Gets a value indicating whether the product is completely out of stock.
    /// </summary>
    public bool IsOutOfStock => AvailableStock == 0;

    /// <summary>
    /// Gets a value indicating whether the product is unavailable (no longer active).
    /// </summary>
    public bool IsUnavailable { get; set; }

    /// <summary>
    /// Gets the validation message for this issue.
    /// </summary>
    public string Message => IsUnavailable
        ? $"{ProductTitle} is no longer available."
        : IsOutOfStock
            ? $"{ProductTitle} is out of stock."
            : $"{ProductTitle}: only {AvailableStock} available (requested {RequestedQuantity}).";
}

/// <summary>
/// Represents a price change for a cart item.
/// </summary>
public class PriceChangeIssue
{
    /// <summary>
    /// Gets or sets the cart item ID.
    /// </summary>
    public Guid CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original price when the item was added to cart.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Gets or sets the current price of the product.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets the price difference (positive means price increased).
    /// </summary>
    public decimal PriceDifference => CurrentPrice - OriginalPrice;

    /// <summary>
    /// Gets a value indicating whether the price increased.
    /// </summary>
    public bool PriceIncreased => CurrentPrice > OriginalPrice;

    /// <summary>
    /// Gets the validation message for this price change.
    /// </summary>
    public string Message => PriceIncreased
        ? $"{ProductTitle}: price increased from {OriginalPrice:C} to {CurrentPrice:C}."
        : $"{ProductTitle}: price decreased from {OriginalPrice:C} to {CurrentPrice:C}.";
}

/// <summary>
/// Represents a validated cart item with current product information.
/// </summary>
public class ValidatedCartItem
{
    /// <summary>
    /// Gets or sets the cart item ID.
    /// </summary>
    public Guid CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current price of the product.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the total price for this item (quantity × unit price).
    /// </summary>
    public decimal TotalPrice => UnitPrice * Quantity;
}
