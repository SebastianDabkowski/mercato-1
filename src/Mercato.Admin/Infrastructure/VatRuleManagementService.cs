using System.Text.Json;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing VAT rules from the admin panel.
/// </summary>
public class VatRuleManagementService : IVatRuleManagementService
{
    private readonly IVatRuleRepository _vatRuleRepository;
    private readonly IVatRuleHistoryRepository _historyRepository;
    private readonly ILogger<VatRuleManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VatRuleManagementService"/> class.
    /// </summary>
    /// <param name="vatRuleRepository">The VAT rule repository.</param>
    /// <param name="historyRepository">The VAT rule history repository.</param>
    /// <param name="logger">The logger.</param>
    public VatRuleManagementService(
        IVatRuleRepository vatRuleRepository,
        IVatRuleHistoryRepository historyRepository,
        ILogger<VatRuleManagementService> logger)
    {
        ArgumentNullException.ThrowIfNull(vatRuleRepository);
        ArgumentNullException.ThrowIfNull(historyRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _vatRuleRepository = vatRuleRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetVatRulesResult> GetAllRulesAsync()
    {
        var rules = await _vatRuleRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} VAT rules", rules.Count);

        return GetVatRulesResult.Success(rules);
    }

    /// <inheritdoc />
    public async Task<GetVatRuleResult> GetRuleByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetVatRuleResult.Failure("VAT rule ID is required.");
        }

        var rule = await _vatRuleRepository.GetByIdAsync(id);

        if (rule == null)
        {
            return GetVatRuleResult.Failure("VAT rule not found.");
        }

        return GetVatRuleResult.Success(rule);
    }

    /// <inheritdoc />
    public async Task<CreateVatRuleResult> CreateRuleAsync(CreateVatRuleCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateVatRuleResult.Failure(validationErrors);
        }

        var now = DateTimeOffset.UtcNow;
        var rule = new VatRule
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            CountryCode = command.CountryCode.ToUpperInvariant(),
            TaxRate = command.TaxRate,
            CategoryId = command.CategoryId,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = command.EffectiveTo,
            Priority = command.Priority,
            IsActive = command.IsActive,
            CreatedAt = now,
            CreatedByUserId = command.CreatedByUserId
        };

        await _vatRuleRepository.AddAsync(rule);

        // Create history record
        var history = new VatRuleHistory
        {
            Id = Guid.NewGuid(),
            VatRuleId = rule.Id,
            ChangeType = "Created",
            PreviousValues = null,
            NewValues = SerializeVatRule(rule),
            ChangedAt = now,
            ChangedByUserId = command.CreatedByUserId,
            ChangedByUserEmail = command.CreatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Created VAT rule '{Name}' (ID: {Id}) for country {CountryCode} by user {UserId}. Rate: {Rate}%, EffectiveFrom: {EffectiveFrom}",
            rule.Name,
            rule.Id,
            rule.CountryCode,
            command.CreatedByUserId,
            rule.TaxRate,
            rule.EffectiveFrom);

        return CreateVatRuleResult.Success(rule);
    }

    /// <inheritdoc />
    public async Task<UpdateVatRuleResult> UpdateRuleAsync(UpdateVatRuleCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateVatRuleResult.Failure(validationErrors);
        }

        var existingRule = await _vatRuleRepository.GetByIdAsync(command.Id);
        if (existingRule == null)
        {
            return UpdateVatRuleResult.Failure("VAT rule not found.");
        }

        var previousValues = SerializeVatRule(existingRule);
        var now = DateTimeOffset.UtcNow;

        existingRule.Name = command.Name;
        existingRule.CountryCode = command.CountryCode.ToUpperInvariant();
        existingRule.TaxRate = command.TaxRate;
        existingRule.CategoryId = command.CategoryId;
        existingRule.EffectiveFrom = command.EffectiveFrom;
        existingRule.EffectiveTo = command.EffectiveTo;
        existingRule.Priority = command.Priority;
        existingRule.IsActive = command.IsActive;
        existingRule.UpdatedAt = now;
        existingRule.UpdatedByUserId = command.UpdatedByUserId;

        await _vatRuleRepository.UpdateAsync(existingRule);

        // Create history record
        var history = new VatRuleHistory
        {
            Id = Guid.NewGuid(),
            VatRuleId = existingRule.Id,
            ChangeType = "Updated",
            PreviousValues = previousValues,
            NewValues = SerializeVatRule(existingRule),
            ChangedAt = now,
            ChangedByUserId = command.UpdatedByUserId,
            ChangedByUserEmail = command.UpdatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Updated VAT rule '{Name}' (ID: {Id}) for country {CountryCode} by user {UserId}. Rate: {Rate}%, EffectiveFrom: {EffectiveFrom}",
            existingRule.Name,
            existingRule.Id,
            existingRule.CountryCode,
            command.UpdatedByUserId,
            existingRule.TaxRate,
            existingRule.EffectiveFrom);

        return UpdateVatRuleResult.Success(existingRule);
    }

    /// <inheritdoc />
    public async Task<DeleteVatRuleResult> DeleteRuleAsync(Guid id, string deletedByUserId, string? deletedByUserEmail = null)
    {
        if (id == Guid.Empty)
        {
            return DeleteVatRuleResult.Failure("VAT rule ID is required.");
        }

        if (string.IsNullOrWhiteSpace(deletedByUserId))
        {
            return DeleteVatRuleResult.Failure("User ID is required.");
        }

        var existingRule = await _vatRuleRepository.GetByIdAsync(id);
        if (existingRule == null)
        {
            return DeleteVatRuleResult.Failure("VAT rule not found.");
        }

        var previousValues = SerializeVatRule(existingRule);
        var now = DateTimeOffset.UtcNow;

        // Create history record before deletion
        var history = new VatRuleHistory
        {
            Id = Guid.NewGuid(),
            VatRuleId = existingRule.Id,
            ChangeType = "Deleted",
            PreviousValues = previousValues,
            NewValues = "{}",
            ChangedAt = now,
            ChangedByUserId = deletedByUserId,
            ChangedByUserEmail = deletedByUserEmail
        };

        await _historyRepository.AddAsync(history);
        await _vatRuleRepository.DeleteAsync(id);

        _logger.LogInformation(
            "Deleted VAT rule '{Name}' (ID: {Id}) by user {UserId}",
            existingRule.Name,
            existingRule.Id,
            deletedByUserId);

        return DeleteVatRuleResult.Success();
    }

    /// <inheritdoc />
    public async Task<GetApplicableRateResult> GetApplicableRateAsync(string countryCode, Guid? categoryId, DateTimeOffset asOfDate)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return GetApplicableRateResult.Failure("Country code is required.");
        }

        if (countryCode.Length != 2)
        {
            return GetApplicableRateResult.Failure("Country code must be a 2-letter ISO 3166-1 alpha-2 code.");
        }

        var normalizedCountryCode = countryCode.ToUpperInvariant();

        // First try to find a category-specific rule
        if (categoryId.HasValue)
        {
            var categoryRule = await _vatRuleRepository.GetActiveByCountryAsync(
                normalizedCountryCode,
                categoryId,
                asOfDate);

            if (categoryRule != null)
            {
                _logger.LogDebug(
                    "Found category-specific VAT rule '{Name}' for country {CountryCode}, category {CategoryId}: {Rate}%",
                    categoryRule.Name,
                    normalizedCountryCode,
                    categoryId,
                    categoryRule.TaxRate);

                return GetApplicableRateResult.Success(categoryRule.TaxRate, categoryRule);
            }
        }

        // Fall back to country-level rule (no category)
        var countryRule = await _vatRuleRepository.GetActiveByCountryAsync(
            normalizedCountryCode,
            null,
            asOfDate);

        if (countryRule != null)
        {
            _logger.LogDebug(
                "Found country-level VAT rule '{Name}' for country {CountryCode}: {Rate}%",
                countryRule.Name,
                normalizedCountryCode,
                countryRule.TaxRate);

            return GetApplicableRateResult.Success(countryRule.TaxRate, countryRule);
        }

        _logger.LogDebug(
            "No applicable VAT rule found for country {CountryCode}, category {CategoryId} as of {AsOfDate}",
            normalizedCountryCode,
            categoryId,
            asOfDate);

        return GetApplicableRateResult.NoRateFound();
    }

    /// <inheritdoc />
    public async Task<GetVatRuleHistoryResult> GetRuleHistoryAsync(Guid vatRuleId)
    {
        if (vatRuleId == Guid.Empty)
        {
            return GetVatRuleHistoryResult.Failure("VAT rule ID is required.");
        }

        var rule = await _vatRuleRepository.GetByIdAsync(vatRuleId);
        var history = await _historyRepository.GetByVatRuleIdAsync(vatRuleId);

        _logger.LogInformation(
            "Retrieved {Count} history records for VAT rule {VatRuleId}",
            history.Count,
            vatRuleId);

        return GetVatRuleHistoryResult.Success(history, rule);
    }

    private static List<string> ValidateCreateCommand(CreateVatRuleCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Rule name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Rule name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.CountryCode))
        {
            errors.Add("Country code is required.");
        }
        else if (command.CountryCode.Length != 2)
        {
            errors.Add("Country code must be a 2-letter ISO 3166-1 alpha-2 code.");
        }

        if (command.TaxRate < 0 || command.TaxRate > 100)
        {
            errors.Add("Tax rate must be between 0 and 100.");
        }

        if (command.EffectiveTo.HasValue && command.EffectiveTo.Value <= command.EffectiveFrom)
        {
            errors.Add("Effective end date must be after effective start date.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateVatRuleCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("VAT rule ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Rule name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Rule name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.CountryCode))
        {
            errors.Add("Country code is required.");
        }
        else if (command.CountryCode.Length != 2)
        {
            errors.Add("Country code must be a 2-letter ISO 3166-1 alpha-2 code.");
        }

        if (command.TaxRate < 0 || command.TaxRate > 100)
        {
            errors.Add("Tax rate must be between 0 and 100.");
        }

        if (command.EffectiveTo.HasValue && command.EffectiveTo.Value <= command.EffectiveFrom)
        {
            errors.Add("Effective end date must be after effective start date.");
        }

        if (string.IsNullOrWhiteSpace(command.UpdatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static string SerializeVatRule(VatRule rule)
    {
        var data = new
        {
            rule.Id,
            rule.Name,
            rule.CountryCode,
            rule.TaxRate,
            rule.CategoryId,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.Priority,
            rule.IsActive,
            rule.CreatedAt,
            rule.CreatedByUserId,
            rule.UpdatedAt,
            rule.UpdatedByUserId
        };

        return JsonSerializer.Serialize(data);
    }
}
