using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Helper class for displaying payment status information to users.
/// </summary>
public static class PaymentStatusDisplay
{
    /// <summary>
    /// Gets the display text for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The display text.</returns>
    public static string GetDisplayText(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Pending",
        PaymentStatus.Processing => "Processing",
        PaymentStatus.Paid => "Paid",
        PaymentStatus.Failed => "Failed",
        PaymentStatus.Cancelled => "Cancelled",
        PaymentStatus.Refunded => "Refunded",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the Bootstrap badge CSS class for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetBadgeClass(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "bg-warning text-dark",
        PaymentStatus.Processing => "bg-info text-white",
        PaymentStatus.Paid => "bg-success",
        PaymentStatus.Failed => "bg-danger",
        PaymentStatus.Cancelled => "bg-secondary",
        PaymentStatus.Refunded => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the Bootstrap icon class for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The icon class name.</returns>
    public static string GetIconClass(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "bi-hourglass-split",
        PaymentStatus.Processing => "bi-arrow-repeat",
        PaymentStatus.Paid => "bi-check-circle-fill",
        PaymentStatus.Failed => "bi-x-circle-fill",
        PaymentStatus.Cancelled => "bi-slash-circle",
        PaymentStatus.Refunded => "bi-arrow-counterclockwise",
        _ => "bi-question-circle"
    };

    /// <summary>
    /// Gets a buyer-friendly message for the payment status.
    /// Does not expose technical error details.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <param name="refundedAmount">The refunded amount (if applicable).</param>
    /// <param name="currencySymbol">The currency symbol (default: $).</param>
    /// <returns>A buyer-friendly message.</returns>
    public static string GetBuyerMessage(PaymentStatus status, decimal refundedAmount = 0, string currencySymbol = "$")
    {
        return status switch
        {
            PaymentStatus.Pending => "Your payment is being processed. Please wait for confirmation.",
            PaymentStatus.Processing => "Your payment is currently being verified.",
            PaymentStatus.Paid => "Your payment was successful! Thank you for your purchase.",
            PaymentStatus.Failed => "We were unable to process your payment. Please try again or use a different payment method.",
            PaymentStatus.Cancelled => "Your payment was cancelled.",
            PaymentStatus.Refunded when refundedAmount > 0 => $"Your payment has been refunded. Refund amount: {currencySymbol}{refundedAmount:N2}. The refunded amount should appear in your account within 5-10 business days.",
            PaymentStatus.Refunded => "Your payment has been refunded. The refunded amount should appear in your account within 5-10 business days.",
            _ => "Payment status is being updated."
        };
    }

    /// <summary>
    /// Formats the refunded amount for display.
    /// </summary>
    /// <param name="refundedAmount">The refunded amount.</param>
    /// <param name="totalAmount">The total payment amount.</param>
    /// <returns>Formatted refund display string.</returns>
    public static string FormatRefundDisplay(decimal refundedAmount, decimal totalAmount)
    {
        if (refundedAmount <= 0)
        {
            return string.Empty;
        }

        if (refundedAmount >= totalAmount)
        {
            return $"Full refund: {refundedAmount:C}";
        }

        return $"Partial refund: {refundedAmount:C} of {totalAmount:C}";
    }
}
