using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Implementation of seller onboarding service.
/// </summary>
public class SellerOnboardingService : ISellerOnboardingService
{
    private readonly ISellerOnboardingRepository _repository;
    private readonly ILogger<SellerOnboardingService> _logger;

    public SellerOnboardingService(
        ISellerOnboardingRepository repository,
        ILogger<SellerOnboardingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SellerOnboarding> GetOrCreateOnboardingAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        var onboarding = await _repository.GetBySellerIdAsync(sellerId);
        if (onboarding != null)
        {
            return onboarding;
        }

        onboarding = new SellerOnboarding
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            CurrentStep = OnboardingStep.StoreProfile,
            Status = OnboardingStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await _repository.CreateAsync(onboarding);
        _logger.LogInformation("Created new onboarding record for seller {SellerId}", sellerId);

        return onboarding;
    }

    /// <inheritdoc />
    public async Task<SellerOnboarding?> GetOnboardingAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);
        return await _repository.GetBySellerIdAsync(sellerId);
    }

    /// <inheritdoc />
    public async Task<SaveOnboardingStepResult> SaveStoreProfileAsync(SaveStoreProfileCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var onboarding = await GetOrCreateOnboardingAsync(command.SellerId);

        if (onboarding.Status == OnboardingStatus.PendingVerification ||
            onboarding.Status == OnboardingStatus.Verified)
        {
            return SaveOnboardingStepResult.Failure("Onboarding has already been completed.");
        }

        // Validate command
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.StoreName))
        {
            errors.Add("Store name is required.");
        }
        else if (command.StoreName.Length < 2 || command.StoreName.Length > 200)
        {
            errors.Add("Store name must be between 2 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.StoreDescription))
        {
            errors.Add("Store description is required.");
        }
        else if (command.StoreDescription.Length < 10 || command.StoreDescription.Length > 2000)
        {
            errors.Add("Store description must be between 10 and 2000 characters.");
        }

        if (errors.Count > 0)
        {
            return SaveOnboardingStepResult.Failure(errors);
        }

        // Save the data
        onboarding.StoreName = command.StoreName;
        onboarding.StoreDescription = command.StoreDescription;
        onboarding.StoreLogoUrl = command.StoreLogoUrl;
        onboarding.LastUpdatedAt = DateTimeOffset.UtcNow;

        // Advance to next step if store profile is complete
        AdvanceStepIfComplete(onboarding, OnboardingStep.StoreProfile, OnboardingStep.VerificationData, onboarding.IsStoreProfileComplete);

        await _repository.UpdateAsync(onboarding);
        _logger.LogInformation("Saved store profile for seller {SellerId}", command.SellerId);

        return SaveOnboardingStepResult.Success();
    }

    /// <inheritdoc />
    public async Task<SaveOnboardingStepResult> SaveVerificationDataAsync(SaveVerificationDataCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var onboarding = await _repository.GetBySellerIdAsync(command.SellerId);
        if (onboarding == null)
        {
            return SaveOnboardingStepResult.Failure("Onboarding record not found. Please start from the beginning.");
        }

        if (onboarding.Status == OnboardingStatus.PendingVerification ||
            onboarding.Status == OnboardingStatus.Verified)
        {
            return SaveOnboardingStepResult.Failure("Onboarding has already been completed.");
        }

        // Validate that previous step is complete
        if (!onboarding.IsStoreProfileComplete)
        {
            return SaveOnboardingStepResult.Failure("Please complete the store profile step first.");
        }

        // Validate command based on seller type
        var errors = ValidateVerificationDataCommand(command);

        if (errors.Count > 0)
        {
            return SaveOnboardingStepResult.Failure(errors);
        }

        // Save the data
        onboarding.SellerType = command.SellerType;
        onboarding.BusinessAddress = command.BusinessAddress;
        onboarding.TaxId = command.TaxId;

        if (command.SellerType == SellerType.Company)
        {
            onboarding.BusinessName = command.BusinessName;
            onboarding.BusinessRegistrationNumber = command.BusinessRegistrationNumber;
            onboarding.ContactPersonName = command.ContactPersonName;
            onboarding.ContactPersonEmail = command.ContactPersonEmail;
            onboarding.ContactPersonPhone = command.ContactPersonPhone;
            // Clear individual fields
            onboarding.FullName = null;
            onboarding.PersonalIdNumber = null;
        }
        else
        {
            onboarding.FullName = command.FullName;
            onboarding.PersonalIdNumber = command.PersonalIdNumber;
            // Clear company fields
            onboarding.BusinessName = null;
            onboarding.BusinessRegistrationNumber = null;
            onboarding.ContactPersonName = null;
            onboarding.ContactPersonEmail = null;
            onboarding.ContactPersonPhone = null;
        }

        onboarding.LastUpdatedAt = DateTimeOffset.UtcNow;

        // Advance to next step if verification data is complete
        AdvanceStepIfComplete(onboarding, OnboardingStep.VerificationData, OnboardingStep.PayoutBasics, onboarding.IsVerificationDataComplete);

        await _repository.UpdateAsync(onboarding);
        _logger.LogInformation("Saved verification data for seller {SellerId} as {SellerType}", command.SellerId, command.SellerType);

        return SaveOnboardingStepResult.Success();
    }

    /// <summary>
    /// Validates the verification data command based on seller type.
    /// </summary>
    private static List<string> ValidateVerificationDataCommand(SaveVerificationDataCommand command)
    {
        var errors = new List<string>();

        // Common validations
        if (string.IsNullOrWhiteSpace(command.BusinessAddress))
        {
            errors.Add("Address is required.");
        }
        else if (command.BusinessAddress.Length < 5 || command.BusinessAddress.Length > 500)
        {
            errors.Add("Address must be between 5 and 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.TaxId))
        {
            errors.Add("Tax ID is required.");
        }
        else if (command.TaxId.Length < 5 || command.TaxId.Length > 50)
        {
            errors.Add("Tax ID must be between 5 and 50 characters.");
        }

        if (command.SellerType == SellerType.Company)
        {
            ValidateCompanyFields(command, errors);
        }
        else
        {
            ValidateIndividualFields(command, errors);
        }

        return errors;
    }

    private static void ValidateCompanyFields(SaveVerificationDataCommand command, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(command.BusinessName))
        {
            errors.Add("Business name is required.");
        }
        else if (command.BusinessName.Length < 2 || command.BusinessName.Length > 200)
        {
            errors.Add("Business name must be between 2 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.BusinessRegistrationNumber))
        {
            errors.Add("Business registration number is required.");
        }
        else if (command.BusinessRegistrationNumber.Length > 50)
        {
            errors.Add("Business registration number must be at most 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.ContactPersonName))
        {
            errors.Add("Contact person name is required.");
        }
        else if (command.ContactPersonName.Length < 2 || command.ContactPersonName.Length > 200)
        {
            errors.Add("Contact person name must be between 2 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.ContactPersonEmail))
        {
            errors.Add("Contact person email is required.");
        }
        else if (command.ContactPersonEmail.Length > 254)
        {
            errors.Add("Contact person email must be at most 254 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.ContactPersonPhone))
        {
            errors.Add("Contact person phone is required.");
        }
        else if (command.ContactPersonPhone.Length > 20)
        {
            errors.Add("Contact person phone must be at most 20 characters.");
        }
    }

    private static void ValidateIndividualFields(SaveVerificationDataCommand command, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add("Full name is required.");
        }
        else if (command.FullName.Length < 2 || command.FullName.Length > 200)
        {
            errors.Add("Full name must be between 2 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.PersonalIdNumber))
        {
            errors.Add("Personal ID number is required.");
        }
        else if (command.PersonalIdNumber.Length < 5 || command.PersonalIdNumber.Length > 50)
        {
            errors.Add("Personal ID number must be between 5 and 50 characters.");
        }
    }

    /// <inheritdoc />
    public async Task<SaveOnboardingStepResult> SavePayoutBasicsAsync(SavePayoutBasicsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var onboarding = await _repository.GetBySellerIdAsync(command.SellerId);
        if (onboarding == null)
        {
            return SaveOnboardingStepResult.Failure("Onboarding record not found. Please start from the beginning.");
        }

        if (onboarding.Status == OnboardingStatus.PendingVerification ||
            onboarding.Status == OnboardingStatus.Verified)
        {
            return SaveOnboardingStepResult.Failure("Onboarding has already been completed.");
        }

        // Validate that previous steps are complete
        if (!onboarding.IsStoreProfileComplete)
        {
            return SaveOnboardingStepResult.Failure("Please complete the store profile step first.");
        }

        if (!onboarding.IsVerificationDataComplete)
        {
            return SaveOnboardingStepResult.Failure("Please complete the verification data step first.");
        }

        // Validate command
        var errors = new List<string>();
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

        if (errors.Count > 0)
        {
            return SaveOnboardingStepResult.Failure(errors);
        }

        // Save the data
        onboarding.BankName = command.BankName;
        onboarding.BankAccountNumber = command.BankAccountNumber;
        onboarding.BankRoutingNumber = command.BankRoutingNumber;
        onboarding.AccountHolderName = command.AccountHolderName;
        onboarding.LastUpdatedAt = DateTimeOffset.UtcNow;

        // Advance to completed step if payout basics is complete
        AdvanceStepIfComplete(onboarding, OnboardingStep.PayoutBasics, OnboardingStep.Completed, onboarding.IsPayoutBasicsComplete);

        await _repository.UpdateAsync(onboarding);
        _logger.LogInformation("Saved payout basics for seller {SellerId}", command.SellerId);

        return SaveOnboardingStepResult.Success();
    }

    /// <inheritdoc />
    public async Task<CompleteOnboardingResult> CompleteOnboardingAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        var onboarding = await _repository.GetBySellerIdAsync(sellerId);
        if (onboarding == null)
        {
            return CompleteOnboardingResult.Failure("Onboarding record not found.");
        }

        if (onboarding.Status == OnboardingStatus.PendingVerification)
        {
            return CompleteOnboardingResult.Failure("Onboarding is already pending verification.");
        }

        if (onboarding.Status == OnboardingStatus.Verified)
        {
            return CompleteOnboardingResult.Failure("Onboarding has already been verified.");
        }

        // Validate all steps are complete
        var errors = new List<string>();
        errors.AddRange(GetStoreProfileValidationErrors(onboarding));
        errors.AddRange(GetVerificationDataValidationErrors(onboarding));
        errors.AddRange(GetPayoutBasicsValidationErrors(onboarding));

        if (errors.Count > 0)
        {
            return CompleteOnboardingResult.Failure(errors);
        }

        // Complete the onboarding
        onboarding.Status = OnboardingStatus.PendingVerification;
        onboarding.CurrentStep = OnboardingStep.Completed;
        onboarding.LastUpdatedAt = DateTimeOffset.UtcNow;
        onboarding.CompletedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(onboarding);
        _logger.LogInformation("Completed onboarding for seller {SellerId}, status set to pending verification", sellerId);

        return CompleteOnboardingResult.Success(onboarding.Id);
    }

    /// <inheritdoc />
    public async Task<bool> IsOnboardingCompleteAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        var onboarding = await _repository.GetBySellerIdAsync(sellerId);
        if (onboarding == null)
        {
            return false;
        }

        return onboarding.Status == OnboardingStatus.PendingVerification ||
               onboarding.Status == OnboardingStatus.Verified;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetStoreProfileValidationErrors(SellerOnboarding onboarding)
    {
        ArgumentNullException.ThrowIfNull(onboarding);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(onboarding.StoreName))
        {
            errors.Add("Store name is required.");
        }
        if (string.IsNullOrWhiteSpace(onboarding.StoreDescription))
        {
            errors.Add("Store description is required.");
        }
        return errors;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetVerificationDataValidationErrors(SellerOnboarding onboarding)
    {
        ArgumentNullException.ThrowIfNull(onboarding);

        var errors = new List<string>();

        // Common validations
        if (string.IsNullOrWhiteSpace(onboarding.BusinessAddress))
        {
            errors.Add("Address is required.");
        }
        if (string.IsNullOrWhiteSpace(onboarding.TaxId))
        {
            errors.Add("Tax ID is required.");
        }

        if (onboarding.SellerType == SellerType.Company)
        {
            if (string.IsNullOrWhiteSpace(onboarding.BusinessName))
            {
                errors.Add("Business name is required.");
            }
            if (string.IsNullOrWhiteSpace(onboarding.BusinessRegistrationNumber))
            {
                errors.Add("Business registration number is required.");
            }
            if (string.IsNullOrWhiteSpace(onboarding.ContactPersonName))
            {
                errors.Add("Contact person name is required.");
            }
            if (string.IsNullOrWhiteSpace(onboarding.ContactPersonEmail))
            {
                errors.Add("Contact person email is required.");
            }
            if (string.IsNullOrWhiteSpace(onboarding.ContactPersonPhone))
            {
                errors.Add("Contact person phone is required.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(onboarding.FullName))
            {
                errors.Add("Full name is required.");
            }
            if (string.IsNullOrWhiteSpace(onboarding.PersonalIdNumber))
            {
                errors.Add("Personal ID number is required.");
            }
        }

        return errors;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPayoutBasicsValidationErrors(SellerOnboarding onboarding)
    {
        ArgumentNullException.ThrowIfNull(onboarding);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(onboarding.BankName))
        {
            errors.Add("Bank name is required.");
        }
        if (string.IsNullOrWhiteSpace(onboarding.BankAccountNumber))
        {
            errors.Add("Bank account number is required.");
        }
        if (string.IsNullOrWhiteSpace(onboarding.AccountHolderName))
        {
            errors.Add("Account holder name is required.");
        }
        return errors;
    }

    /// <summary>
    /// Advances the onboarding to the next step if the current step is complete.
    /// </summary>
    /// <param name="onboarding">The onboarding record to update.</param>
    /// <param name="currentStep">The step that was just completed.</param>
    /// <param name="nextStep">The next step to advance to.</param>
    /// <param name="isCurrentStepComplete">Whether the current step data is complete.</param>
    private static void AdvanceStepIfComplete(
        SellerOnboarding onboarding,
        OnboardingStep currentStep,
        OnboardingStep nextStep,
        bool isCurrentStepComplete)
    {
        if (onboarding.CurrentStep == currentStep && isCurrentStepComplete)
        {
            onboarding.CurrentStep = nextStep;
        }
    }
}
