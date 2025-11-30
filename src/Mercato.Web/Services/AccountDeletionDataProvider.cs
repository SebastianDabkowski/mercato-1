using Mercato.Buyer.Domain.Interfaces;
using Mercato.Identity.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;

namespace Mercato.Web.Services;

/// <summary>
/// Implementation of account deletion data provider that coordinates data operations across multiple modules.
/// </summary>
public class AccountDeletionDataProvider : IAccountDeletionDataProvider
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryAddressRepository _deliveryAddressRepository;
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly ILogger<AccountDeletionDataProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountDeletionDataProvider"/> class.
    /// </summary>
    /// <param name="orderRepository">The order repository.</param>
    /// <param name="deliveryAddressRepository">The delivery address repository.</param>
    /// <param name="productReviewRepository">The product review repository.</param>
    /// <param name="storeRepository">The store repository.</param>
    /// <param name="returnRequestRepository">The return request repository.</param>
    /// <param name="refundRepository">The refund repository.</param>
    /// <param name="logger">The logger.</param>
    public AccountDeletionDataProvider(
        IOrderRepository orderRepository,
        IDeliveryAddressRepository deliveryAddressRepository,
        IProductReviewRepository productReviewRepository,
        IStoreRepository storeRepository,
        IReturnRequestRepository returnRequestRepository,
        IRefundRepository refundRepository,
        ILogger<AccountDeletionDataProvider> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _deliveryAddressRepository = deliveryAddressRepository ?? throw new ArgumentNullException(nameof(deliveryAddressRepository));
        _productReviewRepository = productReviewRepository ?? throw new ArgumentNullException(nameof(productReviewRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _returnRequestRepository = returnRequestRepository ?? throw new ArgumentNullException(nameof(returnRequestRepository));
        _refundRepository = refundRepository ?? throw new ArgumentNullException(nameof(refundRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Constant for anonymized user display name
    private const string AnonymizedUserDisplayName = "Deleted User";

    /// <inheritdoc/>
    public async Task<int> GetOrderCountAsync(string userId)
    {
        var orders = await _orderRepository.GetByBuyerIdAsync(userId);
        return orders.Count;
    }

    /// <inheritdoc/>
    public async Task<int> GetDeliveryAddressCountAsync(string userId)
    {
        var addresses = await _deliveryAddressRepository.GetByBuyerIdAsync(userId);
        return addresses.Count;
    }

    /// <inheritdoc/>
    public async Task<int> GetReviewCountAsync(string userId)
    {
        var reviews = await _productReviewRepository.GetByBuyerIdAsync(userId);
        return reviews.Count;
    }

    /// <inheritdoc/>
    public async Task<string?> GetStoreNameAsync(string userId)
    {
        var store = await _storeRepository.GetBySellerIdAsync(userId);
        return store?.Name;
    }

    /// <inheritdoc/>
    public async Task<int> GetOpenDisputeCountAsync(string userId)
    {
        var returnRequests = await _returnRequestRepository.GetByBuyerIdAsync(userId);
        
        // Count cases that are in active states (not completed or rejected)
        var openStatuses = new[] 
        { 
            ReturnStatus.Requested, 
            ReturnStatus.UnderReview,
            ReturnStatus.Approved,
            ReturnStatus.UnderAdminReview 
        };
        
        return returnRequests.Count(r => openStatuses.Contains(r.Status));
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingRefundCountAsync(string userId)
    {
        // Get all orders for this user and check for pending refunds on each
        var orders = await _orderRepository.GetByBuyerIdAsync(userId);
        var pendingRefundCount = 0;
        
        var pendingStatuses = new[]
        {
            RefundStatus.Pending,
            RefundStatus.Processing
        };
        
        foreach (var order in orders)
        {
            var refunds = await _refundRepository.GetByOrderIdAsync(order.Id);
            pendingRefundCount += refunds.Count(r => pendingStatuses.Contains(r.Status));
        }
        
        return pendingRefundCount;
    }

    /// <inheritdoc/>
    public async Task<int> AnonymizeOrderDataAsync(string userId)
    {
        var orders = await _orderRepository.GetByBuyerIdAsync(userId);
        var anonymizedCount = 0;

        foreach (var order in orders)
        {
            // Anonymize personal data while preserving business-critical fields
            order.DeliveryFullName = "[DELETED USER]";
            order.DeliveryAddressLine1 = "[ADDRESS REMOVED]";
            order.DeliveryAddressLine2 = null;
            order.DeliveryCity = "[CITY REMOVED]";
            order.DeliveryState = null;
            order.DeliveryPostalCode = "[REMOVED]";
            order.DeliveryCountry = order.DeliveryCountry; // Keep country for tax reporting
            order.DeliveryPhoneNumber = null;
            order.BuyerEmail = null;
            order.DeliveryInstructions = null;

            // Note: BuyerId is kept but will reference a deleted user
            // Business-critical fields preserved: amounts, dates, product IDs, order status

            await _orderRepository.UpdateAsync(order);
            anonymizedCount++;

            _logger.LogDebug("Anonymized order {OrderNumber} for user {UserId}", order.OrderNumber, userId);
        }

        return anonymizedCount;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteDeliveryAddressesAsync(string userId)
    {
        var addresses = await _deliveryAddressRepository.GetByBuyerIdAsync(userId);
        var deletedCount = 0;

        foreach (var address in addresses)
        {
            await _deliveryAddressRepository.DeleteAsync(address);
            deletedCount++;

            _logger.LogDebug("Deleted delivery address {AddressId} for user {UserId}", address.Id, userId);
        }

        return deletedCount;
    }

    /// <inheritdoc/>
    public async Task<int> AnonymizeReviewsAsync(string userId)
    {
        var reviews = await _productReviewRepository.GetByBuyerIdAsync(userId);
        var anonymizedCount = 0;

        foreach (var review in reviews)
        {
            // Anonymize reviewer identity while keeping review content
            review.BuyerDisplayName = AnonymizedUserDisplayName;

            await _productReviewRepository.UpdateAsync(review);
            anonymizedCount++;

            _logger.LogDebug("Anonymized review {ReviewId} for user {UserId}", review.Id, userId);
        }

        return anonymizedCount;
    }

    /// <inheritdoc/>
    public async Task<bool> AnonymizeStoreDataAsync(string userId)
    {
        var store = await _storeRepository.GetBySellerIdAsync(userId);
        
        if (store == null)
        {
            return false;
        }

        // Anonymize store contact information
        store.ContactEmail = null;
        store.ContactPhone = null;
        store.Status = StoreStatus.Suspended;

        // Note: Store name is kept for historical order reference but we mark it as closed
        // Business-critical data is preserved: historical orders reference this store ID

        await _storeRepository.UpdateAsync(store);

        _logger.LogDebug("Anonymized store {StoreId} for seller {UserId}", store.Id, userId);

        return true;
    }
}
