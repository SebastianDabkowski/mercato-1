using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Implementation of payout settings service.
/// </summary>
public class PayoutSettingsService : IPayoutSettingsService
{
    private readonly IPayoutSettingsRepository _repository;
    private readonly ILogger<PayoutSettingsService> _logger;

    public PayoutSettingsService(
        IPayoutSettingsRepository repository,
        ILogger<PayoutSettingsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PayoutSettings?> GetPayoutSettingsAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);
        return await _repository.GetBySellerIdAsync(sellerId);
    }

    /// <inheritdoc />
    public async Task<PayoutSettings> GetOrCreatePayoutSettingsAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        var payoutSettings = await _repository.GetBySellerIdAsync(sellerId);
        if (payoutSettings != null)
        {
            return payoutSettings;
        }

        payoutSettings = new PayoutSettings
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await _repository.CreateAsync(payoutSettings);
        _logger.LogInformation("Created new payout settings for seller {SellerId}", sellerId);

        return payoutSettings;
    }

    /// <inheritdoc />
    public async Task<SavePayoutSettingsResult> SavePayoutSettingsAsync(SavePayoutSettingsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var payoutSettings = await GetOrCreatePayoutSettingsAsync(command.SellerId);

        // Validate command based on payout method
        var errors = ValidatePayoutSettingsCommand(command);
        if (errors.Count > 0)
        {
            return SavePayoutSettingsResult.Failure(errors);
        }

        // Save the data
        payoutSettings.PreferredPayoutMethod = command.PreferredPayoutMethod;
        payoutSettings.LastUpdatedAt = DateTimeOffset.UtcNow;

        // Save fields based on payout method and clear fields not applicable for data isolation
        if (command.PreferredPayoutMethod == PayoutMethod.BankTransfer)
        {
            payoutSettings.BankName = command.BankName;
            payoutSettings.BankAccountNumber = command.BankAccountNumber;
            payoutSettings.BankRoutingNumber = command.BankRoutingNumber;
            payoutSettings.AccountHolderName = command.AccountHolderName;
            payoutSettings.SwiftCode = command.SwiftCode;
            payoutSettings.Iban = command.Iban;
            // Clear payment account fields for data isolation
            payoutSettings.PaymentAccountEmail = null;
            payoutSettings.PaymentAccountId = null;
        }
        else
        {
            payoutSettings.PaymentAccountEmail = command.PaymentAccountEmail;
            payoutSettings.PaymentAccountId = command.PaymentAccountId;
            // Clear bank transfer fields for data isolation
            payoutSettings.BankName = null;
            payoutSettings.BankAccountNumber = null;
            payoutSettings.BankRoutingNumber = null;
            payoutSettings.AccountHolderName = null;
            payoutSettings.SwiftCode = null;
            payoutSettings.Iban = null;
        }

        // Update completion status
        payoutSettings.IsComplete = command.PreferredPayoutMethod == PayoutMethod.BankTransfer
            ? payoutSettings.IsBankTransferComplete
            : payoutSettings.IsPaymentAccountComplete;

        await _repository.UpdateAsync(payoutSettings);
        _logger.LogInformation("Saved payout settings for seller {SellerId} using {PayoutMethod}", 
            command.SellerId, command.PreferredPayoutMethod);

        return SavePayoutSettingsResult.Success();
    }

    /// <inheritdoc />
    public async Task<bool> HasCompletePayoutSettingsAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        var payoutSettings = await _repository.GetBySellerIdAsync(sellerId);
        return payoutSettings?.IsComplete ?? false;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPayoutSettingsValidationErrors(PayoutSettings payoutSettings)
    {
        ArgumentNullException.ThrowIfNull(payoutSettings);

        var errors = new List<string>();

        if (payoutSettings.PreferredPayoutMethod == PayoutMethod.BankTransfer)
        {
            if (string.IsNullOrWhiteSpace(payoutSettings.BankName))
            {
                errors.Add("Bank name is required for bank transfer payout.");
            }
            if (string.IsNullOrWhiteSpace(payoutSettings.BankAccountNumber))
            {
                errors.Add("Bank account number is required for bank transfer payout.");
            }
            if (string.IsNullOrWhiteSpace(payoutSettings.AccountHolderName))
            {
                errors.Add("Account holder name is required for bank transfer payout.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(payoutSettings.PaymentAccountEmail) &&
                string.IsNullOrWhiteSpace(payoutSettings.PaymentAccountId))
            {
                errors.Add("Payment account email or ID is required for payment account payout.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Validates the payout settings command based on payout method.
    /// </summary>
    private static List<string> ValidatePayoutSettingsCommand(SavePayoutSettingsCommand command)
    {
        var errors = new List<string>();

        if (command.PreferredPayoutMethod == PayoutMethod.BankTransfer)
        {
            ValidateBankTransferFields(command, errors);
        }
        else
        {
            ValidatePaymentAccountFields(command, errors);
        }

        return errors;
    }

    private static void ValidateBankTransferFields(SavePayoutSettingsCommand command, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(command.BankName))
        {
            errors.Add("Bank name is required.");
        }
        else if (command.BankName.Length < 2 || command.BankName.Length > 200)
        {
            errors.Add("Bank name must be between 2 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.BankAccountNumber))
        {
            errors.Add("Bank account number is required.");
        }
        else if (command.BankAccountNumber.Length < 5 || command.BankAccountNumber.Length > 50)
        {
            errors.Add("Bank account number must be between 5 and 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.AccountHolderName))
        {
            errors.Add("Account holder name is required.");
        }
        else if (command.AccountHolderName.Length < 2 || command.AccountHolderName.Length > 200)
        {
            errors.Add("Account holder name must be between 2 and 200 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.BankRoutingNumber) && command.BankRoutingNumber.Length > 20)
        {
            errors.Add("Bank routing number must be at most 20 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.SwiftCode) && command.SwiftCode.Length > 11)
        {
            errors.Add("SWIFT code must be at most 11 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.Iban) && command.Iban.Length > 34)
        {
            errors.Add("IBAN must be at most 34 characters.");
        }
    }

    private static void ValidatePaymentAccountFields(SavePayoutSettingsCommand command, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(command.PaymentAccountEmail) &&
            string.IsNullOrWhiteSpace(command.PaymentAccountId))
        {
            errors.Add("Payment account email or ID is required.");
        }

        if (!string.IsNullOrWhiteSpace(command.PaymentAccountEmail) && command.PaymentAccountEmail.Length > 254)
        {
            errors.Add("Payment account email must be at most 254 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.PaymentAccountId) && command.PaymentAccountId.Length > 100)
        {
            errors.Add("Payment account ID must be at most 100 characters.");
        }
    }
}
