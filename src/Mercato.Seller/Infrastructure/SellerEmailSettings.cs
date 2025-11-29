namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Configuration settings for seller notification email functionality.
/// </summary>
public class SellerEmailSettings
{
    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string SenderEmail { get; set; } = "noreply@mercato.com";

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string SenderName { get; set; } = "Mercato Marketplace";

    /// <summary>
    /// Gets or sets the base URL for email links.
    /// </summary>
    public string BaseUrl { get; set; } = "https://mercato.com";

    /// <summary>
    /// Gets or sets the new order notification email subject template.
    /// {0} = Sub-Order Number
    /// </summary>
    public string NewOrderSubjectTemplate { get; set; } = "New Order Received - {0}";

    /// <summary>
    /// Gets or sets the new order notification email body template.
    /// {0} = Sub-Order Number
    /// {1} = Order Date
    /// {2} = Buyer Name
    /// {3} = Items List
    /// {4} = Items Subtotal
    /// {5} = Shipping Cost
    /// {6} = Total Amount
    /// {7} = Order Details URL
    /// </summary>
    public string NewOrderBodyTemplate { get; set; } = @"
You have received a new order!

Sub-Order Number: {0}
Order Date: {1}
Buyer: {2}

Items Ordered:
{3}

Order Summary:
  Items Subtotal: {4}
  Shipping: {5}
  Total: {6}

View Order Details: {7}

Please process this order promptly to maintain your seller rating.

Thank you for selling on Mercato!
";

    /// <summary>
    /// Gets or sets the return/complaint notification email subject template.
    /// {0} = Case Number
    /// {1} = Case Type (Return or Complaint)
    /// </summary>
    public string ReturnComplaintSubjectTemplate { get; set; } = "New {1} Case - {0}";

    /// <summary>
    /// Gets or sets the return/complaint notification email body template.
    /// {0} = Case Number
    /// {1} = Case Type (Return or Complaint)
    /// {2} = Sub-Order Number
    /// {3} = Reason
    /// {4} = Created Date
    /// {5} = Items List
    /// {6} = Case Details URL
    /// </summary>
    public string ReturnComplaintBodyTemplate { get; set; } = @"
A new {1} case has been opened for one of your orders.

Case Number: {0}
Case Type: {1}
Sub-Order Number: {2}
Date Opened: {4}

Reason:
{3}

Items Affected:
{5}

View Case Details: {6}

Please review this case and respond promptly to ensure a positive resolution.

Thank you for selling on Mercato!
";

    /// <summary>
    /// Gets or sets the payout processed notification email subject template.
    /// {0} = Payout Amount
    /// {1} = Currency
    /// </summary>
    public string PayoutProcessedSubjectTemplate { get; set; } = "Payout Processed - {0} {1}";

    /// <summary>
    /// Gets or sets the payout processed notification email body template.
    /// {0} = Payout Amount
    /// {1} = Currency
    /// {2} = Payout Date
    /// {3} = Payout ID
    /// {4} = Payout Details URL
    /// </summary>
    public string PayoutProcessedBodyTemplate { get; set; } = @"
Great news! Your payout has been processed.

Payout Details:
  Amount: {0} {1}
  Processed On: {2}
  Payout ID: {3}

View Payout Details: {4}

The funds should arrive in your account according to your payment provider's timeline.

Thank you for selling on Mercato!
";
}
