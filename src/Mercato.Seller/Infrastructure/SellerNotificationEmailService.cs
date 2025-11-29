using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for sending notification emails to sellers.
/// Uses a configurable template system for email content.
/// </summary>
public class SellerNotificationEmailService : ISellerNotificationEmailService
{
    private readonly ILogger<SellerNotificationEmailService> _logger;
    private readonly SellerEmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerNotificationEmailService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="emailSettings">The email settings.</param>
    public SellerNotificationEmailService(
        ILogger<SellerNotificationEmailService> logger,
        IOptions<SellerEmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <inheritdoc />
    public Task<SendEmailResult> SendNewOrderNotificationAsync(SellerSubOrder subOrder, Order parentOrder, string sellerEmail)
    {
        if (string.IsNullOrEmpty(sellerEmail))
        {
            return Task.FromResult(SendEmailResult.Failure("Seller email is required."));
        }

        try
        {
            var subject = string.Format(
                _emailSettings.NewOrderSubjectTemplate,
                subOrder.SubOrderNumber);

            var body = BuildNewOrderEmailBody(subOrder, parentOrder);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            // Configure SellerEmail:Provider and SellerEmail:SmtpSettings in appsettings.json for production.
            // For now, we log the email content for development purposes.
            _logger.LogInformation(
                "New order notification email prepared for seller {Email}, Sub-Order {SubOrderNumber}. Subject: {Subject}. Body length: {BodyLength} chars",
                sellerEmail, subOrder.SubOrderNumber, subject, body.Length);

            return Task.FromResult(SendEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new order notification email to {Email} for sub-order {SubOrderNumber}",
                sellerEmail, subOrder.SubOrderNumber);
            return Task.FromResult(SendEmailResult.Failure("Failed to send new order notification email. Please contact support."));
        }
    }

    /// <inheritdoc />
    public Task<SendEmailResult> SendReturnOrComplaintNotificationAsync(ReturnRequest returnRequest, SellerSubOrder subOrder, string sellerEmail)
    {
        if (string.IsNullOrEmpty(sellerEmail))
        {
            return Task.FromResult(SendEmailResult.Failure("Seller email is required."));
        }

        try
        {
            var caseType = returnRequest.CaseType.ToString();
            var subject = string.Format(
                _emailSettings.ReturnComplaintSubjectTemplate,
                returnRequest.CaseNumber,
                caseType);

            var body = BuildReturnComplaintEmailBody(returnRequest, subOrder);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            // Configure SellerEmail:Provider and SellerEmail:SmtpSettings in appsettings.json for production.
            // For now, we log the email content for development purposes.
            _logger.LogInformation(
                "Return/complaint notification email prepared for seller {Email}, Case {CaseNumber}. Subject: {Subject}. Body length: {BodyLength} chars",
                sellerEmail, returnRequest.CaseNumber, subject, body.Length);

            return Task.FromResult(SendEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return/complaint notification email to {Email} for case {CaseNumber}",
                sellerEmail, returnRequest.CaseNumber);
            return Task.FromResult(SendEmailResult.Failure("Failed to send return/complaint notification email. Please contact support."));
        }
    }

    /// <inheritdoc />
    public Task<SendEmailResult> SendPayoutProcessedNotificationAsync(Payout payout, string sellerEmail)
    {
        if (string.IsNullOrEmpty(sellerEmail))
        {
            return Task.FromResult(SendEmailResult.Failure("Seller email is required."));
        }

        try
        {
            var subject = string.Format(
                _emailSettings.PayoutProcessedSubjectTemplate,
                payout.Amount.ToString("N2"),
                payout.Currency);

            var body = BuildPayoutProcessedEmailBody(payout);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            // Configure SellerEmail:Provider and SellerEmail:SmtpSettings in appsettings.json for production.
            // For now, we log the email content for development purposes.
            _logger.LogInformation(
                "Payout processed notification email prepared for seller {Email}, Payout {PayoutId}. Subject: {Subject}. Body length: {BodyLength} chars",
                sellerEmail, payout.Id, subject, body.Length);

            return Task.FromResult(SendEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payout processed notification email to {Email} for payout {PayoutId}",
                sellerEmail, payout.Id);
            return Task.FromResult(SendEmailResult.Failure("Failed to send payout processed notification email. Please contact support."));
        }
    }

    private string BuildNewOrderEmailBody(SellerSubOrder subOrder, Order parentOrder)
    {
        var itemsList = string.Join("\n", subOrder.Items.Select(item =>
            $"  - {item.ProductTitle} (x{item.Quantity}) - {item.UnitPrice:C} each = {item.TotalPrice:C}"));

        var orderDetailsUrl = $"{_emailSettings.BaseUrl}/Seller/Orders/Details?id={subOrder.Id}";

        var body = string.Format(
            _emailSettings.NewOrderBodyTemplate,
            subOrder.SubOrderNumber,
            subOrder.CreatedAt.ToString("MMMM dd, yyyy 'at' h:mm tt"),
            parentOrder.DeliveryFullName,
            itemsList,
            subOrder.ItemsSubtotal.ToString("C"),
            subOrder.ShippingCost.ToString("C"),
            subOrder.TotalAmount.ToString("C"),
            orderDetailsUrl);

        return body;
    }

    private string BuildReturnComplaintEmailBody(ReturnRequest returnRequest, SellerSubOrder subOrder)
    {
        var caseType = returnRequest.CaseType.ToString();
        
        // Build items list - if case has specific items, list those; otherwise list all sub-order items
        string itemsList;
        if (returnRequest.CaseItems.Count > 0)
        {
            var subOrderItemsDict = subOrder.Items.ToDictionary(i => i.Id);
            itemsList = string.Join("\n", returnRequest.CaseItems.Select(ci =>
            {
                if (subOrderItemsDict.TryGetValue(ci.SellerSubOrderItemId, out var item))
                {
                    return $"  - {item.ProductTitle} (x{ci.Quantity})";
                }
                return $"  - Item ID: {ci.SellerSubOrderItemId} (x{ci.Quantity})";
            }));
        }
        else
        {
            itemsList = string.Join("\n", subOrder.Items.Select(item =>
                $"  - {item.ProductTitle} (x{item.Quantity})"));
        }

        var caseDetailsUrl = $"{_emailSettings.BaseUrl}/Seller/Returns/Details?id={returnRequest.Id}";

        var body = string.Format(
            _emailSettings.ReturnComplaintBodyTemplate,
            returnRequest.CaseNumber,
            caseType,
            subOrder.SubOrderNumber,
            returnRequest.Reason,
            returnRequest.CreatedAt.ToString("MMMM dd, yyyy 'at' h:mm tt"),
            itemsList,
            caseDetailsUrl);

        return body;
    }

    private string BuildPayoutProcessedEmailBody(Payout payout)
    {
        var payoutDetailsUrl = $"{_emailSettings.BaseUrl}/Seller/Payouts/Details?id={payout.Id}";

        var body = string.Format(
            _emailSettings.PayoutProcessedBodyTemplate,
            payout.Amount.ToString("N2"),
            payout.Currency,
            payout.CompletedAt?.ToString("MMMM dd, yyyy 'at' h:mm tt") ?? "Just now",
            payout.Id,
            payoutDetailsUrl);

        return body;
    }
}
