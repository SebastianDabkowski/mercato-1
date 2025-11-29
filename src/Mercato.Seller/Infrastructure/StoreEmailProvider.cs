using Mercato.Orders.Application.Services;
using Mercato.Seller.Domain.Interfaces;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for providing store contact emails.
/// </summary>
public class StoreEmailProvider : IStoreEmailProvider
{
    private readonly IStoreRepository _storeRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreEmailProvider"/> class.
    /// </summary>
    /// <param name="storeRepository">The store repository.</param>
    public StoreEmailProvider(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    /// <inheritdoc />
    public async Task<string?> GetStoreEmailAsync(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return null;
        }

        var store = await _storeRepository.GetByIdAsync(storeId);
        return store?.ContactEmail;
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, string>> GetStoreEmailsAsync(IEnumerable<Guid> storeIds)
    {
        var ids = storeIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var stores = await _storeRepository.GetByIdsAsync(ids);
        return stores
            .Where(s => !string.IsNullOrEmpty(s.ContactEmail))
            .ToDictionary(s => s.Id, s => s.ContactEmail!);
    }
}
