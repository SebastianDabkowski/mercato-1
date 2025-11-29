using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Queries;

/// <summary>
/// Query for retrieving return requests with filtering options.
/// </summary>
public class ReturnRequestQuery
{
    /// <summary>
    /// Gets or sets the buyer ID to filter by.
    /// </summary>
    public string? BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the store ID to filter by.
    /// </summary>
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID to filter by.
    /// </summary>
    public Guid? SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the statuses to filter by.
    /// </summary>
    public IReadOnlyList<ReturnStatus> Statuses { get; set; } = [];
}
