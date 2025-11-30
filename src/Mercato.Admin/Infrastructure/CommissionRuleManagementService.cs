using Mercato.Admin.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing commission rules from the admin panel.
/// </summary>
public class CommissionRuleManagementService : ICommissionRuleManagementService
{
    private readonly ICommissionRuleRepository _ruleRepository;
    private readonly ILogger<CommissionRuleManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionRuleManagementService"/> class.
    /// </summary>
    /// <param name="ruleRepository">The commission rule repository.</param>
    /// <param name="logger">The logger.</param>
    public CommissionRuleManagementService(
        ICommissionRuleRepository ruleRepository,
        ILogger<CommissionRuleManagementService> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetCommissionRulesResult> GetAllRulesAsync()
    {
        var rules = await _ruleRepository.GetAllRulesAsync();

        _logger.LogInformation("Retrieved {Count} commission rules", rules.Count);

        return GetCommissionRulesResult.Success(rules);
    }

    /// <inheritdoc />
    public async Task<GetCommissionRuleResult> GetRuleByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetCommissionRuleResult.Failure("Rule ID is required.");
        }

        var rule = await _ruleRepository.GetByIdAsync(id);

        if (rule == null)
        {
            return GetCommissionRuleResult.Failure("Commission rule not found.");
        }

        return GetCommissionRuleResult.Success(rule);
    }

    /// <inheritdoc />
    public async Task<CreateCommissionRuleResult> CreateRuleAsync(CreateCommissionRuleCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateCommissionRuleResult.Failure(validationErrors);
        }

        // Check for conflicting rules
        var conflictingRules = await _ruleRepository.GetConflictingRulesAsync(
            command.SellerId,
            command.CategoryId,
            command.EffectiveDate);

        if (conflictingRules.Count > 0)
        {
            _logger.LogWarning(
                "Conflict detected when creating commission rule '{Name}': {ConflictCount} conflicting rules found",
                command.Name,
                conflictingRules.Count);

            return CreateCommissionRuleResult.ConflictFailure(conflictingRules);
        }

        var now = DateTimeOffset.UtcNow;
        var rule = new CommissionRule
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            SellerId = command.SellerId,
            CategoryId = command.CategoryId,
            CommissionRate = command.CommissionRate,
            FixedFee = command.FixedFee,
            MinCommission = command.MinCommission,
            MaxCommission = command.MaxCommission,
            Priority = command.Priority,
            EffectiveDate = command.EffectiveDate,
            IsActive = command.IsActive,
            CreatedAt = now,
            LastUpdatedAt = now,
            CreatedByUserId = command.CreatedByUserId,
            LastModifiedByUserId = command.CreatedByUserId,
            Version = 1
        };

        await _ruleRepository.AddAsync(rule);

        _logger.LogInformation(
            "Created commission rule '{Name}' (ID: {Id}) by user {UserId}. Rate: {Rate}%, FixedFee: {FixedFee}, EffectiveDate: {EffectiveDate}",
            rule.Name,
            rule.Id,
            command.CreatedByUserId,
            rule.CommissionRate,
            rule.FixedFee,
            rule.EffectiveDate);

        return CreateCommissionRuleResult.Success(rule);
    }

    /// <inheritdoc />
    public async Task<UpdateCommissionRuleResult> UpdateRuleAsync(UpdateCommissionRuleCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateCommissionRuleResult.Failure(validationErrors);
        }

        var existingRule = await _ruleRepository.GetByIdAsync(command.Id);
        if (existingRule == null)
        {
            return UpdateCommissionRuleResult.Failure("Commission rule not found.");
        }

        // Check for conflicting rules (excluding the current rule)
        var conflictingRules = await _ruleRepository.GetConflictingRulesAsync(
            command.SellerId,
            command.CategoryId,
            command.EffectiveDate,
            command.Id);

        if (conflictingRules.Count > 0)
        {
            _logger.LogWarning(
                "Conflict detected when updating commission rule '{Name}' (ID: {Id}): {ConflictCount} conflicting rules found",
                command.Name,
                command.Id,
                conflictingRules.Count);

            return UpdateCommissionRuleResult.ConflictFailure(conflictingRules);
        }

        var now = DateTimeOffset.UtcNow;

        existingRule.Name = command.Name;
        existingRule.SellerId = command.SellerId;
        existingRule.CategoryId = command.CategoryId;
        existingRule.CommissionRate = command.CommissionRate;
        existingRule.FixedFee = command.FixedFee;
        existingRule.MinCommission = command.MinCommission;
        existingRule.MaxCommission = command.MaxCommission;
        existingRule.Priority = command.Priority;
        existingRule.EffectiveDate = command.EffectiveDate;
        existingRule.IsActive = command.IsActive;
        existingRule.LastUpdatedAt = now;
        existingRule.LastModifiedByUserId = command.ModifiedByUserId;
        existingRule.Version++;

        await _ruleRepository.UpdateAsync(existingRule);

        _logger.LogInformation(
            "Updated commission rule '{Name}' (ID: {Id}) by user {UserId}. Version: {Version}. Rate: {Rate}%, FixedFee: {FixedFee}, EffectiveDate: {EffectiveDate}",
            existingRule.Name,
            existingRule.Id,
            command.ModifiedByUserId,
            existingRule.Version,
            existingRule.CommissionRate,
            existingRule.FixedFee,
            existingRule.EffectiveDate);

        return UpdateCommissionRuleResult.Success(existingRule);
    }

    private static List<string> ValidateCreateCommand(CreateCommissionRuleCommand command)
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

        if (command.CommissionRate < 0 || command.CommissionRate > 100)
        {
            errors.Add("Commission rate must be between 0 and 100.");
        }

        if (command.FixedFee < 0)
        {
            errors.Add("Fixed fee cannot be negative.");
        }

        if (command.MinCommission.HasValue && command.MinCommission.Value < 0)
        {
            errors.Add("Minimum commission cannot be negative.");
        }

        if (command.MaxCommission.HasValue && command.MaxCommission.Value < 0)
        {
            errors.Add("Maximum commission cannot be negative.");
        }

        if (command.MinCommission.HasValue && command.MaxCommission.HasValue &&
            command.MinCommission.Value > command.MaxCommission.Value)
        {
            errors.Add("Minimum commission cannot be greater than maximum commission.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateCommissionRuleCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Rule ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Rule name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Rule name must not exceed 200 characters.");
        }

        if (command.CommissionRate < 0 || command.CommissionRate > 100)
        {
            errors.Add("Commission rate must be between 0 and 100.");
        }

        if (command.FixedFee < 0)
        {
            errors.Add("Fixed fee cannot be negative.");
        }

        if (command.MinCommission.HasValue && command.MinCommission.Value < 0)
        {
            errors.Add("Minimum commission cannot be negative.");
        }

        if (command.MaxCommission.HasValue && command.MaxCommission.Value < 0)
        {
            errors.Add("Maximum commission cannot be negative.");
        }

        if (command.MinCommission.HasValue && command.MaxCommission.HasValue &&
            command.MinCommission.Value > command.MaxCommission.Value)
        {
            errors.Add("Minimum commission cannot be greater than maximum commission.");
        }

        if (string.IsNullOrWhiteSpace(command.ModifiedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }
}
