using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Service implementation for sending order confirmation emails.
/// Uses a configurable template system for email content.
/// </summary>
public class OrderConfirmationEmailService : IOrderConfirmationEmailService
{
    private readonly ILogger<OrderConfirmationEmailService> _logger;
    private readonly EmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderConfirmationEmailService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="emailSettings">The email settings.</param>
    public OrderConfirmationEmailService(
        ILogger<OrderConfirmationEmailService> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <inheritdoc />
    public Task<SendEmailResult> SendOrderConfirmationAsync(Order order, string buyerEmail)
    {
        if (string.IsNullOrEmpty(buyerEmail))
        {
            return Task.FromResult(SendEmailResult.Failure("Buyer email is required."));
        }

        try
        {
            var subject = string.Format(
                _emailSettings.OrderConfirmationSubjectTemplate,
                order.OrderNumber);

            var body = BuildEmailBody(order);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            // Configure Email:Provider and Email:SmtpSettings in appsettings.json for production.
            // For now, we log the email content for development purposes.
            _logger.LogInformation(
                "Order confirmation email prepared for {Email}, Order {OrderNumber}. Subject: {Subject}. Body length: {BodyLength} chars",
                buyerEmail, order.OrderNumber, subject, body.Length);

            return Task.FromResult(SendEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email to {Email} for order {OrderNumber}",
                buyerEmail, order.OrderNumber);
            return Task.FromResult(SendEmailResult.Failure("Failed to send confirmation email. Please contact support."));
        }
    }

    private string BuildEmailBody(Order order)
    {
        var itemsList = string.Join("\n", order.Items.Select(item =>
            $"  - {item.ProductTitle} (x{item.Quantity}) - {item.UnitPrice:C} each = {item.TotalPrice:C}"));

        var deliveryAddress = FormatDeliveryAddress(order);

        var body = string.Format(
            _emailSettings.OrderConfirmationBodyTemplate,
            order.OrderNumber,
            order.CreatedAt.ToString("MMMM dd, yyyy 'at' h:mm tt"),
            itemsList,
            order.ItemsSubtotal.ToString("C"),
            order.ShippingTotal.ToString("C"),
            order.TotalAmount.ToString("C"),
            deliveryAddress,
            _emailSettings.EstimatedDeliveryDays);

        return body;
    }

    private static string FormatDeliveryAddress(Order order)
    {
        var lines = new List<string>
        {
            order.DeliveryFullName,
            order.DeliveryAddressLine1
        };

        if (!string.IsNullOrEmpty(order.DeliveryAddressLine2))
        {
            lines.Add(order.DeliveryAddressLine2);
        }

        var cityStateZip = string.IsNullOrEmpty(order.DeliveryState)
            ? $"{order.DeliveryCity} {order.DeliveryPostalCode}"
            : $"{order.DeliveryCity}, {order.DeliveryState} {order.DeliveryPostalCode}";

        lines.Add(cityStateZip);
        lines.Add(order.DeliveryCountry);

        if (!string.IsNullOrEmpty(order.DeliveryPhoneNumber))
        {
            lines.Add($"Phone: {order.DeliveryPhoneNumber}");
        }

        return string.Join("\n", lines);
    }
}

/// <summary>
/// Configuration settings for email functionality.
/// </summary>
public class EmailSettings
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
    /// Gets or sets the order confirmation email subject template.
    /// {0} = Order Number
    /// </summary>
    public string OrderConfirmationSubjectTemplate { get; set; } = "Order Confirmation - {0}";

    /// <summary>
    /// Gets or sets the order confirmation email body template.
    /// {0} = Order Number
    /// {1} = Order Date
    /// {2} = Items List
    /// {3} = Items Subtotal
    /// {4} = Shipping Total
    /// {5} = Total Amount
    /// {6} = Delivery Address
    /// {7} = Estimated Delivery Days
    /// </summary>
    public string OrderConfirmationBodyTemplate { get; set; } = @"
Thank you for your order!

Order Number: {0}
Order Date: {1}

Items Ordered:
{2}

Order Summary:
  Subtotal: {3}
  Shipping: {4}
  Total: {5}

Delivery Address:
{6}

Estimated Delivery: {7} business days

Thank you for shopping with Mercato!
";

    /// <summary>
    /// Gets or sets the estimated delivery days message.
    /// </summary>
    public string EstimatedDeliveryDays { get; set; } = "5-7";
}
