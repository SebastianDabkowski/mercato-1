using Mercato.Payments.Application.Services;
using Mercato.Seller.Domain.Interfaces;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for providing seller contact emails for payout notifications.
/// </summary>
public class SellerEmailProvider : ISellerEmailProvider
{
    private readonly IStoreRepository _storeRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerEmailProvider"/> class.
    /// </summary>
    /// <param name="storeRepository">The store repository.</param>
    public SellerEmailProvider(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    /// <inheritdoc />
    public async Task<string?> GetSellerEmailAsync(Guid sellerId)
    {
        if (sellerId == Guid.Empty)
        {
            return null;
        }

        var store = await _storeRepository.GetByIdAsync(sellerId);
        return store?.ContactEmail;
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, string>> GetSellerEmailsAsync(IEnumerable<Guid> sellerIds)
    {
        var ids = sellerIds.Where(id => id != Guid.Empty).Distinct().ToList();
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
