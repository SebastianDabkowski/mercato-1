using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for sending payout notification emails to sellers.
/// </summary>
public class PayoutNotificationService : IPayoutNotificationService
{
    private readonly ILogger<PayoutNotificationService> _logger;
    private readonly SellerEmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayoutNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="emailSettings">The email settings.</param>
    public PayoutNotificationService(
        ILogger<PayoutNotificationService> logger,
        IOptions<SellerEmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <inheritdoc />
    public Task<PayoutNotificationResult> SendPayoutProcessedNotificationAsync(Payout payout, string sellerEmail)
    {
        if (string.IsNullOrEmpty(sellerEmail))
        {
            return Task.FromResult(PayoutNotificationResult.Failure("Seller email is required."));
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

            return Task.FromResult(PayoutNotificationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payout processed notification email to {Email} for payout {PayoutId}",
                sellerEmail, payout.Id);
            return Task.FromResult(PayoutNotificationResult.Failure("Failed to send payout processed notification email. Please contact support."));
        }
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
