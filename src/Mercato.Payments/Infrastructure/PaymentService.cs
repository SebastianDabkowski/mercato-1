using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for payment operations with simulated payment processing.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentSettings _paymentSettings;

    /// <summary>
    /// In-memory store for payment transactions (simulated persistence).
    /// In a real implementation, this would use a repository and database.
    /// </summary>
    private static readonly Dictionary<Guid, PaymentTransaction> _transactions = new();

    /// <summary>
    /// In-memory store for idempotency keys mapped to transaction IDs.
    /// </summary>
    private static readonly Dictionary<string, Guid> _idempotencyKeys = new();
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
    /// Bank transfer payment method ID.
    /// </summary>
    private const string BankTransferMethodId = "bank_transfer";

    /// <summary>
    /// BLIK payment method ID.
    /// </summary>
    private const string BlikMethodId = "blik";

    /// <summary>
    /// Maximum length for external reference IDs.
    /// </summary>
    private const int MaxReferenceIdLength = 20;

    /// <summary>
    /// Required length for BLIK codes.
    /// </summary>
    private const int BlikCodeLength = 6;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="paymentSettings">The payment settings.</param>
    public PaymentService(ILogger<PaymentService> logger, IOptions<PaymentSettings> paymentSettings)
    {
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
    }

    /// <inheritdoc />
    public Task<GetPaymentMethodsResult> GetPaymentMethodsAsync()
    {
        var methods = new List<PaymentMethod>();

        if (_paymentSettings.EnableCreditCard)
        {
            methods.Add(new PaymentMethod
            {
                Id = CreditCardMethodId,
                Name = "Credit Card",
                Description = "Pay securely with your credit or debit card",
                IconClass = "bi-credit-card",
                IsEnabled = true,
                IsDefault = true,
                SortOrder = 1
            });
        }

        if (_paymentSettings.EnablePayPal)
        {
            methods.Add(new PaymentMethod
            {
                Id = PayPalMethodId,
                Name = "PayPal",
                Description = "Pay with your PayPal account",
                IconClass = "bi-paypal",
                IsEnabled = true,
                IsDefault = false,
                SortOrder = 2
            });
        }

        if (_paymentSettings.EnableBankTransfer)
        {
            methods.Add(new PaymentMethod
            {
                Id = BankTransferMethodId,
                Name = "Bank Transfer",
                Description = "Pay directly from your bank account",
                IconClass = "bi-bank",
                IsEnabled = true,
                IsDefault = false,
                SortOrder = 3
            });
        }

        if (_paymentSettings.EnableBlik)
        {
            methods.Add(new PaymentMethod
            {
                Id = BlikMethodId,
                Name = "BLIK",
                Description = "Pay instantly with your 6-digit BLIK code from your mobile banking app",
                IconClass = "bi-phone",
                IsEnabled = true,
                IsDefault = false,
                SortOrder = 4
            });
        }

        // If no methods are enabled, show all by default
        if (methods.Count == 0)
        {
            methods = new List<PaymentMethod>
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
                }
            };
        }

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

        // Handle idempotency - if same key was used before, return existing transaction
        if (!string.IsNullOrEmpty(command.IdempotencyKey))
        {
            lock (_lock)
            {
                if (_idempotencyKeys.TryGetValue(command.IdempotencyKey, out var existingTransactionId))
                {
                    if (_transactions.TryGetValue(existingTransactionId, out var existingTransaction))
                    {
                        _logger.LogInformation(
                            "Returning existing transaction for idempotency key: TransactionId={TransactionId}, IdempotencyKey={IdempotencyKey}",
                            existingTransactionId, command.IdempotencyKey);

                        if (existingTransaction.PaymentMethodId == BlikMethodId)
                        {
                            return Task.FromResult(InitiatePaymentResult.RequiresBlikCodeEntry(existingTransaction.Id));
                        }

                        return Task.FromResult(InitiatePaymentResult.SuccessWithRedirect(
                            existingTransaction.Id,
                            existingTransaction.RedirectUrl ?? string.Empty));
                    }
                }
            }
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

            // Store idempotency key if provided
            if (!string.IsNullOrEmpty(command.IdempotencyKey))
            {
                _idempotencyKeys[command.IdempotencyKey] = transaction.Id;
            }
        }

        _logger.LogInformation(
            "Payment initiated: TransactionId={TransactionId}, Amount={Amount}, Method={Method}",
            transaction.Id, command.Amount, command.PaymentMethodId);

        // Handle BLIK payment method - requires BLIK code entry
        if (command.PaymentMethodId == BlikMethodId)
        {
            // If BLIK code is provided, validate and process immediately
            if (!string.IsNullOrEmpty(command.BlikCode))
            {
                var blikErrors = ValidateBlikCode(command.BlikCode);
                if (blikErrors.Count > 0)
                {
                    return Task.FromResult(InitiatePaymentResult.Failure(blikErrors));
                }

                // Simulate BLIK payment processing (always succeeds for valid 6-digit code)
                transaction.Status = PaymentStatus.Completed;
                transaction.CompletedAt = DateTimeOffset.UtcNow;
                transaction.ExternalReferenceId = GenerateExternalReferenceId("BLIK");
                transaction.LastUpdatedAt = DateTimeOffset.UtcNow;

                lock (_lock)
                {
                    _transactions[transaction.Id] = transaction;
                }

                _logger.LogInformation(
                    "BLIK payment completed: TransactionId={TransactionId}",
                    transaction.Id);

                return Task.FromResult(InitiatePaymentResult.SuccessWithoutRedirect(transaction.Id));
            }

            // BLIK code not provided - require BLIK code entry
            return Task.FromResult(InitiatePaymentResult.RequiresBlikCodeEntry(transaction.Id));
        }

        // Simulate redirect-based payment flow for other methods
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
            transaction.ExternalReferenceId = command.ExternalReferenceId ?? GenerateExternalReferenceId("SIM");
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
        else if (command.PaymentMethodId != CreditCardMethodId &&
                 command.PaymentMethodId != PayPalMethodId &&
                 command.PaymentMethodId != BankTransferMethodId &&
                 command.PaymentMethodId != BlikMethodId)
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

    private static List<string> ValidateBlikCode(string blikCode)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(blikCode))
        {
            errors.Add("BLIK code is required.");
            return errors;
        }

        if (blikCode.Length != BlikCodeLength)
        {
            errors.Add($"BLIK code must be exactly {BlikCodeLength} digits.");
            return errors;
        }

        if (!blikCode.All(char.IsDigit))
        {
            errors.Add("BLIK code must contain only digits.");
        }

        return errors;
    }

    /// <inheritdoc />
    public Task<SubmitBlikCodeResult> SubmitBlikCodeAsync(SubmitBlikCodeCommand command)
    {
        // Validate BLIK code
        var blikErrors = ValidateBlikCode(command.BlikCode);
        if (blikErrors.Count > 0)
        {
            return Task.FromResult(SubmitBlikCodeResult.Failure(blikErrors));
        }

        PaymentTransaction? transaction;

        lock (_lock)
        {
            if (!_transactions.TryGetValue(command.TransactionId, out transaction))
            {
                _logger.LogWarning("Transaction not found for BLIK submission: {TransactionId}", command.TransactionId);
                return Task.FromResult(SubmitBlikCodeResult.Failure("Transaction not found."));
            }
        }

        // Verify the buyer owns this transaction
        if (transaction.BuyerId != command.BuyerId)
        {
            _logger.LogWarning(
                "Unauthorized BLIK submission attempt: TransactionId={TransactionId}, ExpectedBuyer={ExpectedBuyer}, ActualBuyer={ActualBuyer}",
                command.TransactionId, transaction.BuyerId, command.BuyerId);
            return Task.FromResult(SubmitBlikCodeResult.NotAuthorized());
        }

        // Verify transaction is pending
        if (transaction.Status != PaymentStatus.Pending)
        {
            return Task.FromResult(SubmitBlikCodeResult.Failure("Transaction is not in pending state."));
        }

        // Verify payment method is BLIK
        if (transaction.PaymentMethodId != BlikMethodId)
        {
            return Task.FromResult(SubmitBlikCodeResult.Failure("Transaction is not a BLIK payment."));
        }

        // Simulate BLIK payment processing (always succeeds for valid 6-digit code)
        transaction.Status = PaymentStatus.Completed;
        transaction.CompletedAt = DateTimeOffset.UtcNow;
        transaction.ExternalReferenceId = GenerateExternalReferenceId("BLIK");
        transaction.LastUpdatedAt = DateTimeOffset.UtcNow;

        lock (_lock)
        {
            _transactions[transaction.Id] = transaction;
        }

        _logger.LogInformation(
            "BLIK code submitted successfully: TransactionId={TransactionId}",
            transaction.Id);

        return Task.FromResult(SubmitBlikCodeResult.Success(transaction));
    }

    /// <summary>
    /// Generates a unique external reference ID with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix for the reference ID (e.g., "BLIK", "SIM").</param>
    /// <returns>A unique reference ID in the format "PREFIX-XXXXXXXX".</returns>
    private static string GenerateExternalReferenceId(string prefix)
    {
        var guid = Guid.NewGuid().ToString("N");
        var maxGuidLength = MaxReferenceIdLength - prefix.Length - 1; // -1 for the hyphen
        return $"{prefix}-{guid.Substring(0, Math.Min(guid.Length, maxGuidLength))}";
    }
}
