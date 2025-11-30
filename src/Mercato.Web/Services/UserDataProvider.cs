using Mercato.Buyer.Domain.Interfaces;
using Mercato.Identity.Application.Queries;
using Mercato.Identity.Application.Services;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Seller.Domain.Interfaces;

namespace Mercato.Web.Services;

/// <summary>
/// Implementation of user data provider that aggregates data from multiple modules for GDPR data export.
/// </summary>
public class UserDataProvider : IUserDataProvider
{
    private readonly IDeliveryAddressRepository _deliveryAddressRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IConsentRepository _consentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataProvider"/> class.
    /// </summary>
    /// <param name="deliveryAddressRepository">The delivery address repository.</param>
    /// <param name="orderRepository">The order repository.</param>
    /// <param name="storeRepository">The store repository.</param>
    /// <param name="consentRepository">The consent repository.</param>
    public UserDataProvider(
        IDeliveryAddressRepository deliveryAddressRepository,
        IOrderRepository orderRepository,
        IStoreRepository storeRepository,
        IConsentRepository consentRepository)
    {
        _deliveryAddressRepository = deliveryAddressRepository ?? throw new ArgumentNullException(nameof(deliveryAddressRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _consentRepository = consentRepository ?? throw new ArgumentNullException(nameof(consentRepository));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeliveryAddressData>> GetDeliveryAddressesAsync(string userId)
    {
        var addresses = await _deliveryAddressRepository.GetByBuyerIdAsync(userId);

        return addresses.Select(a => new DeliveryAddressData
        {
            Label = a.Label,
            FullName = a.FullName,
            AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2,
            City = a.City,
            State = a.State,
            PostalCode = a.PostalCode,
            Country = a.Country,
            PhoneNumber = a.PhoneNumber,
            IsDefault = a.IsDefault,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderData>> GetOrdersAsync(string userId)
    {
        var orders = await _orderRepository.GetByBuyerIdAsync(userId);

        return orders.Select(o => new OrderData
        {
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            DeliveryFullName = o.DeliveryFullName,
            DeliveryAddress = BuildDeliveryAddress(o),
            DeliveryCity = o.DeliveryCity,
            DeliveryCountry = o.DeliveryCountry,
            BuyerEmail = o.BuyerEmail,
            Items = o.Items.Select(i => new OrderItemData
            {
                ProductName = i.ProductTitle,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<StoreData?> GetStoreAsync(string userId)
    {
        var store = await _storeRepository.GetBySellerIdAsync(userId);

        if (store == null)
        {
            return null;
        }

        return new StoreData
        {
            Name = store.Name,
            Description = store.Description,
            ContactEmail = store.ContactEmail,
            ContactPhone = store.ContactPhone,
            WebsiteUrl = store.WebsiteUrl,
            CreatedAt = store.CreatedAt,
            Status = store.Status.ToString()
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsentData>> GetConsentsAsync(string userId)
    {
        var consents = await _consentRepository.GetUserConsentsAsync(userId);

        return consents.Select(c => new ConsentData
        {
            ConsentType = c.ConsentVersion?.ConsentType?.Name ?? "Unknown",
            IsGranted = c.IsGranted,
            ConsentDate = c.ConsentedAt
        }).ToList();
    }

    private static string BuildDeliveryAddress(Mercato.Orders.Domain.Entities.Order order)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(order.DeliveryAddressLine1))
        {
            parts.Add(order.DeliveryAddressLine1);
        }
        
        if (!string.IsNullOrWhiteSpace(order.DeliveryAddressLine2))
        {
            parts.Add(order.DeliveryAddressLine2);
        }

        return string.Join(", ", parts);
    }
}
