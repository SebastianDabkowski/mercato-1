namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for retrieving seller contact emails for payout notifications.
/// Note: In the Mercato system, seller identity is represented by the Store entity.
/// The SellerId in payout context corresponds to the Store.Id.
/// </summary>
public interface ISellerEmailProvider
{
    /// <summary>
    /// Gets the contact email for a seller by their store identifier.
    /// </summary>
    /// <param name="sellerId">The seller's store identifier (Store.Id).</param>
    /// <returns>The seller contact email if found and available; otherwise, null.</returns>
    Task<string?> GetSellerEmailAsync(Guid sellerId);

    /// <summary>
    /// Gets the contact emails for multiple sellers by their store identifiers.
    /// </summary>
    /// <param name="sellerIds">The seller store identifiers (Store.Id values).</param>
    /// <returns>A dictionary mapping seller IDs to their contact emails. Sellers without emails are not included.</returns>
    Task<IDictionary<Guid, string>> GetSellerEmailsAsync(IEnumerable<Guid> sellerIds);
}
