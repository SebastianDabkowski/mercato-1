namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Command for creating a new order from validated cart items.
/// </summary>
public class CreateOrderCommand
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment transaction ID.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the validated cart items.
    /// </summary>
    public IReadOnlyList<CreateOrderItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total shipping cost.
    /// </summary>
    public decimal ShippingTotal { get; set; }

    /// <summary>
    /// Gets or sets the delivery address.
    /// </summary>
    public DeliveryAddressInfo DeliveryAddress { get; set; } = new();
}

/// <summary>
/// Represents an item to be included in the order.
/// </summary>
public class CreateOrderItem
{
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
    /// Gets or sets the unit price.
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
    /// Gets or sets the product image URL.
    /// </summary>
    public string? ProductImageUrl { get; set; }
}

/// <summary>
/// Represents delivery address information for an order.
/// </summary>
public class DeliveryAddressInfo
{
    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address line 1.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address line 2.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state/province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
