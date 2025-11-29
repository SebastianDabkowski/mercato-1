namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for deleting a shipping method.
/// </summary>
public class DeleteShippingMethodCommand
{
    /// <summary>
    /// Gets or sets the shipping method ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this shipping method belongs to (for authorization).
    /// </summary>
    public Guid StoreId { get; set; }
}
