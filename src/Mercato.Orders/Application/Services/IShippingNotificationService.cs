using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for sending shipping notification emails.
/// </summary>
public interface IShippingNotificationService
{
    /// <summary>
    /// Sends a shipping notification email to the buyer when an order has been shipped.
    /// </summary>
    /// <param name="sellerSubOrder">The seller sub-order that was shipped.</param>
    /// <param name="parentOrder">The parent order containing buyer information.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<SendEmailResult> SendShippingNotificationAsync(SellerSubOrder sellerSubOrder, Order parentOrder);
}
