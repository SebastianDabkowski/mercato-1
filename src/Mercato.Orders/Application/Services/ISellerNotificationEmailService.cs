using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;

namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for sending notification emails to sellers.
/// </summary>
public interface ISellerNotificationEmailService
{
    /// <summary>
    /// Sends a new order notification email to the seller when an order is placed for their products.
    /// </summary>
    /// <param name="subOrder">The seller sub-order that was created.</param>
    /// <param name="parentOrder">The parent order containing delivery information.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<SendEmailResult> SendNewOrderNotificationAsync(SellerSubOrder subOrder, Order parentOrder, string sellerEmail);

    /// <summary>
    /// Sends a return or complaint notification email to the seller when a case is created.
    /// </summary>
    /// <param name="returnRequest">The return request or complaint that was created.</param>
    /// <param name="subOrder">The seller sub-order associated with the return request.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<SendEmailResult> SendReturnOrComplaintNotificationAsync(ReturnRequest returnRequest, SellerSubOrder subOrder, string sellerEmail);

    /// <summary>
    /// Sends a payout processed notification email to the seller when a payout is completed.
    /// </summary>
    /// <param name="payout">The payout that was processed.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <returns>The result of the email send operation.</returns>
    Task<SendEmailResult> SendPayoutProcessedNotificationAsync(Payout payout, string sellerEmail);
}
