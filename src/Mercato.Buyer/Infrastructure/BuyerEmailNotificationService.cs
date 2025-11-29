using Mercato.Buyer.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Buyer.Infrastructure;

/// <summary>
/// Service implementation for sending email notifications to buyers.
/// Uses a configurable template system for email content.
/// </summary>
public class BuyerEmailNotificationService : IBuyerEmailNotificationService
{
    private readonly ILogger<BuyerEmailNotificationService> _logger;
    private readonly BuyerEmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuyerEmailNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="emailSettings">The email settings.</param>
    public BuyerEmailNotificationService(
        ILogger<BuyerEmailNotificationService> logger,
        IOptions<BuyerEmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <inheritdoc />
    public Task<BuyerEmailResult> SendRegistrationWelcomeEmailAsync(string buyerEmail, string? buyerName = null)
    {
        if (string.IsNullOrWhiteSpace(buyerEmail))
        {
            return Task.FromResult(BuyerEmailResult.Failure("Buyer email is required."));
        }

        try
        {
            var displayName = !string.IsNullOrWhiteSpace(buyerName) ? buyerName : "Valued Customer";

            var subject = _emailSettings.RegistrationWelcomeSubjectTemplate;

            var body = string.Format(
                _emailSettings.RegistrationWelcomeBodyTemplate,
                displayName);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            // Configure BuyerEmail:Provider and BuyerEmail:SmtpSettings in appsettings.json for production.
            // For now, we log the email content for development purposes.
            _logger.LogInformation(
                "Registration welcome email prepared for {Email}. Subject: {Subject}. Body length: {BodyLength} chars",
                buyerEmail, subject, body.Length);

            return Task.FromResult(BuyerEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration welcome email to {Email}", buyerEmail);
            return Task.FromResult(BuyerEmailResult.Failure("Failed to send registration email. Please contact support."));
        }
    }

    /// <inheritdoc />
    public Task<BuyerEmailResult> SendPayoutConfirmationEmailAsync(SendPayoutEmailCommand command)
    {
        var validationErrors = ValidatePayoutCommand(command);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(BuyerEmailResult.Failure(validationErrors));
        }

        try
        {
            var displayName = !string.IsNullOrWhiteSpace(command.BuyerName) ? command.BuyerName : "Valued Customer";

            var subject = string.Format(
                _emailSettings.PayoutConfirmationSubjectTemplate,
                command.PayoutReference);

            var body = string.Format(
                _emailSettings.PayoutConfirmationBodyTemplate,
                displayName,
                command.PayoutReference,
                command.Amount.ToString("N2"),
                command.Currency,
                command.ProcessedAt.ToString("MMMM dd, yyyy 'at' h:mm tt"),
                command.PaymentMethod ?? "Your registered payment method");

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            _logger.LogInformation(
                "Payout confirmation email prepared for {Email}, Reference {Reference}. Subject: {Subject}. Body length: {BodyLength} chars",
                command.BuyerEmail, command.PayoutReference, subject, body.Length);

            return Task.FromResult(BuyerEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payout confirmation email to {Email} for reference {Reference}",
                command.BuyerEmail, command.PayoutReference);
            return Task.FromResult(BuyerEmailResult.Failure("Failed to send payout confirmation email. Please contact support."));
        }
    }

    /// <inheritdoc />
    public Task<BuyerEmailResult> SendRefundConfirmationEmailAsync(SendRefundEmailCommand command)
    {
        var validationErrors = ValidateRefundCommand(command);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(BuyerEmailResult.Failure(validationErrors));
        }

        try
        {
            var displayName = !string.IsNullOrWhiteSpace(command.BuyerName) ? command.BuyerName : "Valued Customer";
            var refundType = command.IsFullRefund ? "Full" : "Partial";

            var subject = string.Format(
                _emailSettings.RefundConfirmationSubjectTemplate,
                command.OrderNumber);

            var body = string.Format(
                _emailSettings.RefundConfirmationBodyTemplate,
                displayName,
                refundType,
                command.RefundReference,
                command.OrderNumber,
                command.Amount.ToString("N2"),
                command.Currency,
                command.Reason,
                command.ProcessedAt.ToString("MMMM dd, yyyy 'at' h:mm tt"),
                _emailSettings.RefundProcessingDays);

            // In a real implementation, this would send via SMTP, SendGrid, etc.
            _logger.LogInformation(
                "Refund confirmation email prepared for {Email}, Reference {Reference}, Order {OrderNumber}. Subject: {Subject}. Body length: {BodyLength} chars",
                command.BuyerEmail, command.RefundReference, command.OrderNumber, subject, body.Length);

            return Task.FromResult(BuyerEmailResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send refund confirmation email to {Email} for order {OrderNumber}",
                command.BuyerEmail, command.OrderNumber);
            return Task.FromResult(BuyerEmailResult.Failure("Failed to send refund confirmation email. Please contact support."));
        }
    }

    private static List<string> ValidatePayoutCommand(SendPayoutEmailCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BuyerEmail))
        {
            errors.Add("Buyer email is required.");
        }

        if (command.Amount <= 0)
        {
            errors.Add("Payout amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.PayoutReference))
        {
            errors.Add("Payout reference is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            errors.Add("Currency is required.");
        }

        return errors;
    }

    private static List<string> ValidateRefundCommand(SendRefundEmailCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BuyerEmail))
        {
            errors.Add("Buyer email is required.");
        }

        if (command.Amount <= 0)
        {
            errors.Add("Refund amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.RefundReference))
        {
            errors.Add("Refund reference is required.");
        }

        if (string.IsNullOrWhiteSpace(command.OrderNumber))
        {
            errors.Add("Order number is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            errors.Add("Currency is required.");
        }

        return errors;
    }
}

/// <summary>
/// Configuration settings for buyer email notifications.
/// </summary>
public class BuyerEmailSettings
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
    /// Gets or sets the registration welcome email subject template.
    /// </summary>
    public string RegistrationWelcomeSubjectTemplate { get; set; } = "Welcome to Mercato Marketplace!";

    /// <summary>
    /// Gets or sets the registration welcome email body template.
    /// {0} = Buyer Name
    /// </summary>
    public string RegistrationWelcomeBodyTemplate { get; set; } = @"
Welcome to Mercato Marketplace, {0}!

Thank you for joining our community of buyers and sellers.

Your account has been successfully created. You can now:
- Browse thousands of products from independent sellers
- Save items to your wishlist
- Track your orders in real-time
- Leave reviews for products you've purchased

Getting Started:
1. Complete your profile to personalize your experience
2. Add a delivery address to speed up checkout
3. Browse our curated collections and discover unique products

If you have any questions, our support team is here to help.

Happy shopping!

The Mercato Team
";

    /// <summary>
    /// Gets or sets the payout confirmation email subject template.
    /// {0} = Payout Reference
    /// </summary>
    public string PayoutConfirmationSubjectTemplate { get; set; } = "Payout Confirmation - {0}";

    /// <summary>
    /// Gets or sets the payout confirmation email body template.
    /// {0} = Buyer Name
    /// {1} = Payout Reference
    /// {2} = Amount
    /// {3} = Currency
    /// {4} = Processed Date
    /// {5} = Payment Method
    /// </summary>
    public string PayoutConfirmationBodyTemplate { get; set; } = @"
Dear {0},

Your payout has been processed successfully.

Payout Details:
  Reference: {1}
  Amount: {2} {3}
  Processed: {4}
  Payment Method: {5}

The funds should appear in your account within 3-5 business days, depending on your financial institution.

If you have any questions about this payout, please contact our support team with the reference number above.

Thank you for using Mercato!

The Mercato Team
";

    /// <summary>
    /// Gets or sets the refund confirmation email subject template.
    /// {0} = Order Number
    /// </summary>
    public string RefundConfirmationSubjectTemplate { get; set; } = "Refund Confirmation - Order {0}";

    /// <summary>
    /// Gets or sets the refund confirmation email body template.
    /// {0} = Buyer Name
    /// {1} = Refund Type (Full/Partial)
    /// {2} = Refund Reference
    /// {3} = Order Number
    /// {4} = Amount
    /// {5} = Currency
    /// {6} = Reason
    /// {7} = Processed Date
    /// {8} = Refund Processing Days
    /// </summary>
    public string RefundConfirmationBodyTemplate { get; set; } = @"
Dear {0},

Your refund has been processed successfully.

Refund Details:
  Type: {1} Refund
  Reference: {2}
  Original Order: {3}
  Amount: {4} {5}
  Reason: {6}
  Processed: {7}

The refund should appear on your original payment method within {8} business days.

If you have any questions about this refund, please contact our support team with the reference number above.

Thank you for shopping with Mercato!

The Mercato Team
";

    /// <summary>
    /// Gets or sets the typical number of days for refund processing.
    /// </summary>
    public string RefundProcessingDays { get; set; } = "5-10";
}
