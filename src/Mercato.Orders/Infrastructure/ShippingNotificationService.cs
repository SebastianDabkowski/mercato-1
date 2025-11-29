using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Service implementation for sending shipping notification emails.
/// Notifies buyers when their order has been shipped.
/// </summary>
public class ShippingNotificationService : IShippingNotificationService
{
    private readonly ILogger<ShippingNotificationService> _logger;
    private readonly EmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="emailSettings">The email settings.</param>
    public ShippingNotificationService(
        ILogger<ShippingNotificationService> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <inheritdoc />
    public Task<SendEmailResult> SendShippingNotificationAsync(SellerSubOrder sellerSubOrder, Order parentOrder)
    {
        if (string.IsNullOrEmpty(parentOrder.BuyerEmail))
        {
            return Task.FromResult(SendEmailResult.Failure("Buyer email is required."));
        }

        try
        {
            var subject = string.Format(
                _emailSettings.ShippingNotificationSubjectTemplate,
                sellerSubOrder.SubOrderNumber);

            var body = BuildEmailBody(sellerSubOrder, parentOrder);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            // Configure Email:Provider and Email:SmtpSettings in appsettings.json for production.
            // For now, we log the email content for development purposes.
            _logger.LogInformation(
                "Shipping notification email prepared for {Email}, Sub-Order {SubOrderNumber}. Subject: {Subject}. Body length: {BodyLength} chars",
                parentOrder.BuyerEmail, sellerSubOrder.SubOrderNumber, subject, body.Length);

            return Task.FromResult(SendEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send shipping notification email to {Email} for sub-order {SubOrderNumber}",
                parentOrder.BuyerEmail, sellerSubOrder.SubOrderNumber);
            return Task.FromResult(SendEmailResult.Failure("Failed to send shipping notification email. Please contact support."));
        }
    }

    private string BuildEmailBody(SellerSubOrder sellerSubOrder, Order parentOrder)
    {
        var itemsList = string.Join("\n", sellerSubOrder.Items.Select(item =>
            $"  - {item.ProductTitle} (x{item.Quantity})"));

        var deliveryAddress = FormatDeliveryAddress(parentOrder);

        var trackingInfo = FormatTrackingInfo(sellerSubOrder);

        var body = string.Format(
            _emailSettings.ShippingNotificationBodyTemplate,
            sellerSubOrder.SubOrderNumber,
            sellerSubOrder.StoreName,
            sellerSubOrder.ShippedAt?.ToString("MMMM dd, yyyy 'at' h:mm tt") ?? "Just now",
            itemsList,
            trackingInfo,
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

        return string.Join("\n", lines);
    }

    private static string FormatTrackingInfo(SellerSubOrder sellerSubOrder)
    {
        if (string.IsNullOrEmpty(sellerSubOrder.TrackingNumber) && string.IsNullOrEmpty(sellerSubOrder.ShippingCarrier))
        {
            return "Tracking information will be provided when available.";
        }

        var lines = new List<string>();

        if (!string.IsNullOrEmpty(sellerSubOrder.ShippingCarrier))
        {
            lines.Add($"Carrier: {sellerSubOrder.ShippingCarrier}");
        }

        if (!string.IsNullOrEmpty(sellerSubOrder.TrackingNumber))
        {
            lines.Add($"Tracking Number: {sellerSubOrder.TrackingNumber}");
            var trackingUrl = GetCarrierTrackingUrl(sellerSubOrder.ShippingCarrier, sellerSubOrder.TrackingNumber);
            if (!string.IsNullOrEmpty(trackingUrl))
            {
                lines.Add($"Track your package: {trackingUrl}");
            }
        }

        return string.Join("\n", lines);
    }

    private static string? GetCarrierTrackingUrl(string? carrier, string? trackingNumber)
    {
        if (string.IsNullOrEmpty(carrier) || string.IsNullOrEmpty(trackingNumber))
        {
            return null;
        }

        var carrierLower = carrier.ToLowerInvariant().Trim();
        var encodedTrackingNumber = Uri.EscapeDataString(trackingNumber);

        // Note: These URLs may need to be updated if carriers change their tracking URLs.
        // Consider moving to configuration if frequent updates are needed.
        return carrierLower switch
        {
            "ups" => $"https://www.ups.com/track?loc=en_US&tracknum={encodedTrackingNumber}",
            "fedex" => $"https://www.fedex.com/fedextrack/?tracknumbers={encodedTrackingNumber}",
            "usps" => $"https://tools.usps.com/go/TrackConfirmAction?tLabels={encodedTrackingNumber}",
            "dhl" => $"https://www.dhl.com/us-en/home/tracking/tracking-express.html?submit=1&tracking-id={encodedTrackingNumber}",
            _ => null
        };
    }
}
