namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for retrieving store contact emails for seller notifications.
/// </summary>
public interface IStoreEmailProvider
{
    /// <summary>
    /// Gets the contact email for a store by its identifier.
    /// </summary>
    /// <param name="storeId">The store identifier.</param>
    /// <returns>The store contact email if found and available; otherwise, null.</returns>
    Task<string?> GetStoreEmailAsync(Guid storeId);

    /// <summary>
    /// Gets the contact emails for multiple stores by their identifiers.
    /// </summary>
    /// <param name="storeIds">The store identifiers.</param>
    /// <returns>A dictionary mapping store IDs to their contact emails. Stores without emails are not included.</returns>
    Task<IDictionary<Guid, string>> GetStoreEmailsAsync(IEnumerable<Guid> storeIds);
}
