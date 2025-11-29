using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for mapping external payment provider status codes to internal payment statuses.
/// Maps external provider codes in one central place as per requirements.
/// </summary>
public class PaymentStatusMapper : IPaymentStatusMapper
{
    /// <summary>
    /// Maps common provider status codes to internal payment statuses.
    /// This is the single source of truth for status mapping.
    /// </summary>
    private static readonly Dictionary<string, PaymentStatus> ProviderStatusMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common success statuses
        { "succeeded", PaymentStatus.Paid },
        { "success", PaymentStatus.Paid },
        { "paid", PaymentStatus.Paid },
        { "completed", PaymentStatus.Paid },
        { "approved", PaymentStatus.Paid },
        { "captured", PaymentStatus.Paid },
        { "settled", PaymentStatus.Paid },

        // Pending statuses
        { "pending", PaymentStatus.Pending },
        { "pending_capture", PaymentStatus.Pending },
        { "pending_authorization", PaymentStatus.Pending },
        { "awaiting_payment", PaymentStatus.Pending },
        { "created", PaymentStatus.Pending },
        { "initiated", PaymentStatus.Pending },
        { "authorized", PaymentStatus.Pending },

        // Processing statuses
        { "processing", PaymentStatus.Processing },
        { "in_progress", PaymentStatus.Processing },
        { "requires_action", PaymentStatus.Processing },
        { "requires_confirmation", PaymentStatus.Processing },

        // Failed statuses
        { "failed", PaymentStatus.Failed },
        { "failure", PaymentStatus.Failed },
        { "declined", PaymentStatus.Failed },
        { "rejected", PaymentStatus.Failed },
        { "expired", PaymentStatus.Failed },
        { "error", PaymentStatus.Failed },
        { "payment_failed", PaymentStatus.Failed },
        { "insufficient_funds", PaymentStatus.Failed },
        { "card_declined", PaymentStatus.Failed },

        // Cancelled statuses
        { "cancelled", PaymentStatus.Cancelled },
        { "canceled", PaymentStatus.Cancelled },
        { "voided", PaymentStatus.Cancelled },
        { "void", PaymentStatus.Cancelled },
        { "abandoned", PaymentStatus.Cancelled },

        // Refunded statuses
        { "refunded", PaymentStatus.Refunded },
        { "refund", PaymentStatus.Refunded },
        { "partially_refunded", PaymentStatus.Refunded },
        { "chargeback", PaymentStatus.Refunded }
    };

    /// <summary>
    /// Status codes that indicate an error condition.
    /// </summary>
    private static readonly HashSet<PaymentStatus> ErrorStatuses = new()
    {
        PaymentStatus.Failed,
        PaymentStatus.Cancelled
    };

    /// <inheritdoc />
    public PaymentStatusMappingResult MapProviderStatus(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            return PaymentStatusMappingResult.Unknown("(empty)");
        }

        // Normalize the provider code
        var normalizedCode = providerCode.Trim().ToLowerInvariant();

        if (ProviderStatusMappings.TryGetValue(normalizedCode, out var status))
        {
            return PaymentStatusMappingResult.Success(status, ErrorStatuses.Contains(status));
        }

        return PaymentStatusMappingResult.Unknown(providerCode);
    }

    /// <inheritdoc />
    public string GetBuyerFriendlyMessage(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Your payment is being processed. Please wait for confirmation.",
            PaymentStatus.Processing => "Your payment is currently being processed. This may take a few moments.",
            PaymentStatus.Paid => "Your payment was successful! Thank you for your purchase.",
            PaymentStatus.Failed => "Unfortunately, your payment could not be processed. Please try again or use a different payment method.",
            PaymentStatus.Cancelled => "Your payment was cancelled.",
            PaymentStatus.Refunded => "Your payment has been refunded. The refunded amount should appear in your account within 5-10 business days.",
            _ => "Payment status is being updated."
        };
    }

    /// <inheritdoc />
    public string GetBuyerFriendlyErrorMessage(PaymentStatus status, string? internalError = null)
    {
        // As per requirements, do not expose technical errors to buyers
        return status switch
        {
            PaymentStatus.Failed => "We were unable to process your payment. Please check your payment details and try again. If the problem persists, please contact your bank or try a different payment method.",
            PaymentStatus.Cancelled => "Your payment was cancelled. If you did not cancel this payment, please try again.",
            _ => "An issue occurred with your payment. Please try again or contact customer support for assistance."
        };
    }
}
