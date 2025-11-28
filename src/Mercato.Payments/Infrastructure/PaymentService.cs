using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for payment operations with simulated payment processing.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// In-memory store for payment transactions (simulated persistence).
    /// In a real implementation, this would use a repository and database.
    /// </summary>
    private static readonly Dictionary<Guid, PaymentTransaction> _transactions = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Credit card payment method ID.
    /// </summary>
    private const string CreditCardMethodId = "credit_card";

    /// <summary>
    /// PayPal payment method ID.
    /// </summary>
    private const string PayPalMethodId = "paypal";

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<GetPaymentMethodsResult> GetPaymentMethodsAsync()
    {
        var methods = new List<PaymentMethod>
        {
            new PaymentMethod
            {
                Id = CreditCardMethodId,
                Name = "Credit Card",
                Description = "Pay securely with your credit or debit card",
                IconClass = "bi-credit-card",
                IsEnabled = true,
                IsDefault = true,
                SortOrder = 1
            },
            new PaymentMethod
            {
                Id = PayPalMethodId,
                Name = "PayPal",
                Description = "Pay with your PayPal account",
                IconClass = "bi-paypal",
                IsEnabled = true,
                IsDefault = false,
                SortOrder = 2
            }
        };

        return Task.FromResult(GetPaymentMethodsResult.Success(methods));
    }

    /// <inheritdoc />
    public Task<InitiatePaymentResult> InitiatePaymentAsync(InitiatePaymentCommand command)
    {
        var errors = ValidateInitiatePaymentCommand(command);
        if (errors.Count > 0)
        {
            return Task.FromResult(InitiatePaymentResult.Failure(errors));
        }

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            BuyerId = command.BuyerId,
            PaymentMethodId = command.PaymentMethodId,
            Amount = command.Amount,
            Currency = "USD",
            Status = PaymentStatus.Pending,
            CallbackUrl = command.ReturnUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        // For simulation, we'll create a redirect URL that includes the transaction ID
        // In a real implementation, this would call the payment provider's API
        // NOTE: The redirect URL only contains the transaction ID. Success/failure is
        // determined by the ProcessPaymentCallbackAsync method based on the provider's response.
        var redirectUrl = $"{command.ReturnUrl}?transactionId={transaction.Id}";
        transaction.RedirectUrl = redirectUrl;

        lock (_lock)
        {
            _transactions[transaction.Id] = transaction;
        }

        _logger.LogInformation(
            "Payment initiated: TransactionId={TransactionId}, Amount={Amount}, Method={Method}",
            transaction.Id, command.Amount, command.PaymentMethodId);

        // Simulate redirect-based payment flow
        return Task.FromResult(InitiatePaymentResult.SuccessWithRedirect(transaction.Id, redirectUrl));
    }

    /// <inheritdoc />
    public Task<ProcessPaymentCallbackResult> ProcessPaymentCallbackAsync(ProcessPaymentCallbackCommand command)
    {
        PaymentTransaction? transaction;

        lock (_lock)
        {
            if (!_transactions.TryGetValue(command.TransactionId, out transaction))
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", command.TransactionId);
                return Task.FromResult(ProcessPaymentCallbackResult.Failure("Transaction not found."));
            }
        }

        // Verify the buyer owns this transaction
        if (transaction.BuyerId != command.BuyerId)
        {
            _logger.LogWarning(
                "Unauthorized callback attempt: TransactionId={TransactionId}, ExpectedBuyer={ExpectedBuyer}, ActualBuyer={ActualBuyer}",
                command.TransactionId, transaction.BuyerId, command.BuyerId);
            return Task.FromResult(ProcessPaymentCallbackResult.NotAuthorized());
        }

        // In a real implementation, we would verify the payment with the provider.
        // For simulation, we determine success based on:
        // 1. If IsSuccess is explicitly provided (e.g., from provider callback)
        // 2. Otherwise, if the transaction is still pending, we simulate a successful payment
        var isPaymentSuccessful = command.IsSuccess || transaction.Status == PaymentStatus.Pending;

        // Update transaction status based on verification
        if (isPaymentSuccessful)
        {
            transaction.Status = PaymentStatus.Completed;
            transaction.CompletedAt = DateTimeOffset.UtcNow;
            transaction.ExternalReferenceId = command.ExternalReferenceId ?? $"SIM-{Guid.NewGuid():N}"[..20];
        }
        else
        {
            transaction.Status = PaymentStatus.Failed;
            transaction.ErrorMessage = "Payment was not completed.";
        }

        transaction.LastUpdatedAt = DateTimeOffset.UtcNow;

        lock (_lock)
        {
            _transactions[transaction.Id] = transaction;
        }

        _logger.LogInformation(
            "Payment callback processed: TransactionId={TransactionId}, Status={Status}",
            transaction.Id, transaction.Status);

        return Task.FromResult(ProcessPaymentCallbackResult.Success(transaction));
    }

    /// <inheritdoc />
    public Task<GetPaymentTransactionResult> GetTransactionAsync(Guid transactionId, string buyerId)
    {
        PaymentTransaction? transaction;

        lock (_lock)
        {
            if (!_transactions.TryGetValue(transactionId, out transaction))
            {
                return Task.FromResult(GetPaymentTransactionResult.Failure("Transaction not found."));
            }
        }

        // Verify the buyer owns this transaction
        if (transaction.BuyerId != buyerId)
        {
            return Task.FromResult(GetPaymentTransactionResult.NotAuthorized());
        }

        return Task.FromResult(GetPaymentTransactionResult.Success(transaction));
    }

    private static List<string> ValidateInitiatePaymentCommand(InitiatePaymentCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.PaymentMethodId))
        {
            errors.Add("Payment method is required.");
        }
        else if (command.PaymentMethodId != CreditCardMethodId && command.PaymentMethodId != PayPalMethodId)
        {
            errors.Add($"Invalid payment method: {command.PaymentMethodId}");
        }

        if (command.Amount <= 0)
        {
            errors.Add("Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.ReturnUrl))
        {
            errors.Add("Return URL is required.");
        }

        return errors;
    }
}
