using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing feature flags from the admin panel.
/// </summary>
public class FeatureFlagManagementService : IFeatureFlagManagementService
{
    /// <summary>
    /// Maximum length for target value JSON to prevent denial of service attacks.
    /// </summary>
    private const int MaxTargetValueLength = 10000;

    private static readonly JsonSerializerOptions SafeJsonOptions = new()
    {
        MaxDepth = 5,
        PropertyNameCaseInsensitive = true
    };

    private readonly IFeatureFlagRepository _featureFlagRepository;
    private readonly IFeatureFlagHistoryRepository _historyRepository;
    private readonly ILogger<FeatureFlagManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagManagementService"/> class.
    /// </summary>
    /// <param name="featureFlagRepository">The feature flag repository.</param>
    /// <param name="historyRepository">The feature flag history repository.</param>
    /// <param name="logger">The logger.</param>
    public FeatureFlagManagementService(
        IFeatureFlagRepository featureFlagRepository,
        IFeatureFlagHistoryRepository historyRepository,
        ILogger<FeatureFlagManagementService> logger)
    {
        ArgumentNullException.ThrowIfNull(featureFlagRepository);
        ArgumentNullException.ThrowIfNull(historyRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _featureFlagRepository = featureFlagRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetFeatureFlagsResult> GetAllFlagsAsync()
    {
        var flags = await _featureFlagRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} feature flags", flags.Count);

        return GetFeatureFlagsResult.Success(flags);
    }

    /// <inheritdoc />
    public async Task<GetFeatureFlagsResult> GetFlagsByEnvironmentAsync(FeatureFlagEnvironment environment)
    {
        var flags = await _featureFlagRepository.GetByEnvironmentAsync(environment);

        _logger.LogInformation("Retrieved {Count} feature flags for environment {Environment}", flags.Count, environment);

        return GetFeatureFlagsResult.Success(flags);
    }

    /// <inheritdoc />
    public async Task<GetFeatureFlagResult> GetFlagByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetFeatureFlagResult.Failure("Feature flag ID is required.");
        }

        var flag = await _featureFlagRepository.GetByIdAsync(id);

        if (flag == null)
        {
            return GetFeatureFlagResult.Failure("Feature flag not found.");
        }

        return GetFeatureFlagResult.Success(flag);
    }

    /// <inheritdoc />
    public async Task<CreateFeatureFlagResult> CreateFlagAsync(CreateFeatureFlagCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateFeatureFlagResult.Failure(validationErrors);
        }

        // Check for duplicate key in the same environment
        var existingFlag = await _featureFlagRepository.GetByKeyAndEnvironmentAsync(command.Key, command.Environment);
        if (existingFlag != null)
        {
            return CreateFeatureFlagResult.Failure($"A feature flag with key '{command.Key}' already exists for environment '{command.Environment}'.");
        }

        var now = DateTimeOffset.UtcNow;
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = command.Key,
            Name = command.Name,
            Description = command.Description,
            IsEnabled = command.IsEnabled,
            Environment = command.Environment,
            TargetType = command.TargetType,
            TargetValue = command.TargetValue,
            CreatedAt = now,
            CreatedByUserId = command.CreatedByUserId
        };

        await _featureFlagRepository.AddAsync(flag);

        // Create history record
        var history = new FeatureFlagHistory
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = flag.Id,
            ChangeType = "Created",
            PreviousValues = null,
            NewValues = SerializeFeatureFlag(flag),
            ChangedAt = now,
            ChangedByUserId = command.CreatedByUserId,
            ChangedByUserEmail = command.CreatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Created feature flag '{Key}' (ID: {Id}) for environment {Environment} by user {UserId}. Enabled: {IsEnabled}",
            flag.Key,
            flag.Id,
            flag.Environment,
            command.CreatedByUserId,
            flag.IsEnabled);

        return CreateFeatureFlagResult.Success(flag);
    }

    /// <inheritdoc />
    public async Task<UpdateFeatureFlagResult> UpdateFlagAsync(UpdateFeatureFlagCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateFeatureFlagResult.Failure(validationErrors);
        }

        var existingFlag = await _featureFlagRepository.GetByIdAsync(command.Id);
        if (existingFlag == null)
        {
            return UpdateFeatureFlagResult.Failure("Feature flag not found.");
        }

        // Check for duplicate key in the same environment (exclude current flag)
        var duplicateFlag = await _featureFlagRepository.GetByKeyAndEnvironmentAsync(command.Key, command.Environment);
        if (duplicateFlag != null && duplicateFlag.Id != command.Id)
        {
            return UpdateFeatureFlagResult.Failure($"A feature flag with key '{command.Key}' already exists for environment '{command.Environment}'.");
        }

        var previousValues = SerializeFeatureFlag(existingFlag);
        var now = DateTimeOffset.UtcNow;

        existingFlag.Key = command.Key;
        existingFlag.Name = command.Name;
        existingFlag.Description = command.Description;
        existingFlag.IsEnabled = command.IsEnabled;
        existingFlag.Environment = command.Environment;
        existingFlag.TargetType = command.TargetType;
        existingFlag.TargetValue = command.TargetValue;
        existingFlag.UpdatedAt = now;
        existingFlag.UpdatedByUserId = command.UpdatedByUserId;

        await _featureFlagRepository.UpdateAsync(existingFlag);

        // Create history record
        var history = new FeatureFlagHistory
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = existingFlag.Id,
            ChangeType = "Updated",
            PreviousValues = previousValues,
            NewValues = SerializeFeatureFlag(existingFlag),
            ChangedAt = now,
            ChangedByUserId = command.UpdatedByUserId,
            ChangedByUserEmail = command.UpdatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Updated feature flag '{Key}' (ID: {Id}) for environment {Environment} by user {UserId}. Enabled: {IsEnabled}",
            existingFlag.Key,
            existingFlag.Id,
            existingFlag.Environment,
            command.UpdatedByUserId,
            existingFlag.IsEnabled);

        return UpdateFeatureFlagResult.Success(existingFlag);
    }

    /// <inheritdoc />
    public async Task<DeleteFeatureFlagResult> DeleteFlagAsync(Guid id, string deletedByUserId, string? deletedByUserEmail = null)
    {
        if (id == Guid.Empty)
        {
            return DeleteFeatureFlagResult.Failure("Feature flag ID is required.");
        }

        if (string.IsNullOrWhiteSpace(deletedByUserId))
        {
            return DeleteFeatureFlagResult.Failure("User ID is required.");
        }

        var existingFlag = await _featureFlagRepository.GetByIdAsync(id);
        if (existingFlag == null)
        {
            return DeleteFeatureFlagResult.Failure("Feature flag not found.");
        }

        var previousValues = SerializeFeatureFlag(existingFlag);
        var now = DateTimeOffset.UtcNow;

        // Create history record before deletion
        var history = new FeatureFlagHistory
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = existingFlag.Id,
            ChangeType = "Deleted",
            PreviousValues = previousValues,
            NewValues = "{}",
            ChangedAt = now,
            ChangedByUserId = deletedByUserId,
            ChangedByUserEmail = deletedByUserEmail
        };

        await _historyRepository.AddAsync(history);
        await _featureFlagRepository.DeleteAsync(id);

        _logger.LogInformation(
            "Deleted feature flag '{Key}' (ID: {Id}) by user {UserId}",
            existingFlag.Key,
            existingFlag.Id,
            deletedByUserId);

        return DeleteFeatureFlagResult.Success();
    }

    /// <inheritdoc />
    public async Task<ToggleFeatureFlagResult> ToggleFlagAsync(Guid id, bool isEnabled, string updatedByUserId, string? updatedByUserEmail = null)
    {
        if (id == Guid.Empty)
        {
            return ToggleFeatureFlagResult.Failure("Feature flag ID is required.");
        }

        if (string.IsNullOrWhiteSpace(updatedByUserId))
        {
            return ToggleFeatureFlagResult.Failure("User ID is required.");
        }

        var existingFlag = await _featureFlagRepository.GetByIdAsync(id);
        if (existingFlag == null)
        {
            return ToggleFeatureFlagResult.Failure("Feature flag not found.");
        }

        var previousValues = SerializeFeatureFlag(existingFlag);
        var now = DateTimeOffset.UtcNow;

        existingFlag.IsEnabled = isEnabled;
        existingFlag.UpdatedAt = now;
        existingFlag.UpdatedByUserId = updatedByUserId;

        await _featureFlagRepository.UpdateAsync(existingFlag);

        // Create history record
        var history = new FeatureFlagHistory
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = existingFlag.Id,
            ChangeType = "Toggled",
            PreviousValues = previousValues,
            NewValues = SerializeFeatureFlag(existingFlag),
            ChangedAt = now,
            ChangedByUserId = updatedByUserId,
            ChangedByUserEmail = updatedByUserEmail
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Toggled feature flag '{Key}' (ID: {Id}) to {IsEnabled} by user {UserId}",
            existingFlag.Key,
            existingFlag.Id,
            isEnabled ? "enabled" : "disabled",
            updatedByUserId);

        return ToggleFeatureFlagResult.Success(existingFlag);
    }

    /// <inheritdoc />
    public async Task<EvaluateFeatureFlagResult> EvaluateFlagAsync(string key, FeatureFlagEnvironment environment, string? userId = null, string? sellerId = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return EvaluateFeatureFlagResult.Failure("Feature flag key is required.");
        }

        var flag = await _featureFlagRepository.GetByKeyAndEnvironmentAsync(key, environment);

        if (flag == null)
        {
            _logger.LogDebug("Feature flag '{Key}' not found for environment {Environment}", key, environment);
            return EvaluateFeatureFlagResult.FlagNotFound();
        }

        if (!flag.IsEnabled)
        {
            _logger.LogDebug("Feature flag '{Key}' is disabled for environment {Environment}", key, environment);
            return EvaluateFeatureFlagResult.Success(false, flag);
        }

        var isEnabledForContext = EvaluateTargeting(flag, userId, sellerId);

        _logger.LogDebug(
            "Evaluated feature flag '{Key}' for environment {Environment}: {IsEnabled} (TargetType: {TargetType})",
            key,
            environment,
            isEnabledForContext,
            flag.TargetType);

        return EvaluateFeatureFlagResult.Success(isEnabledForContext, flag);
    }

    /// <inheritdoc />
    public async Task<GetFeatureFlagHistoryResult> GetFlagHistoryAsync(Guid featureFlagId)
    {
        if (featureFlagId == Guid.Empty)
        {
            return GetFeatureFlagHistoryResult.Failure("Feature flag ID is required.");
        }

        var flag = await _featureFlagRepository.GetByIdAsync(featureFlagId);
        var history = await _historyRepository.GetByFeatureFlagIdAsync(featureFlagId);

        _logger.LogInformation(
            "Retrieved {Count} history records for feature flag {FeatureFlagId}",
            history.Count,
            featureFlagId);

        return GetFeatureFlagHistoryResult.Success(history, flag);
    }

    private bool EvaluateTargeting(FeatureFlag flag, string? userId, string? sellerId)
    {
        return flag.TargetType switch
        {
            FeatureFlagTargetType.None => true,
            FeatureFlagTargetType.AllUsers => true,
            FeatureFlagTargetType.InternalUsers => EvaluateInternalUsers(userId),
            FeatureFlagTargetType.SpecificSellers => EvaluateSpecificSellers(flag.TargetValue, sellerId),
            FeatureFlagTargetType.PercentageRollout => EvaluatePercentageRollout(flag.TargetValue, userId),
            _ => false
        };
    }

    private static bool EvaluateInternalUsers(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        // Internal users are identified by having a userId starting with "internal-" or containing "@internal"
        return userId.StartsWith("internal-", StringComparison.OrdinalIgnoreCase) ||
               userId.Contains("@internal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateSpecificSellers(string? targetValue, string? sellerId)
    {
        if (string.IsNullOrWhiteSpace(targetValue) || string.IsNullOrWhiteSpace(sellerId))
        {
            return false;
        }

        // Guard against excessively large input
        if (targetValue.Length > MaxTargetValueLength)
        {
            return false;
        }

        try
        {
            var sellerIds = JsonSerializer.Deserialize<List<string>>(targetValue, SafeJsonOptions);
            return sellerIds != null && sellerIds.Contains(sellerId, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool EvaluatePercentageRollout(string? targetValue, string? userId)
    {
        if (string.IsNullOrWhiteSpace(targetValue) || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        if (!int.TryParse(targetValue, out var percentage) || percentage < 0 || percentage > 100)
        {
            return false;
        }

        // Use a hash of the userId to consistently assign users to the same bucket
        var hash = ComputeUserHash(userId);
        var bucket = hash % 100;

        return bucket < percentage;
    }

    private static int ComputeUserHash(string userId)
    {
        var bytes = Encoding.UTF8.GetBytes(userId);
        var hash = SHA256.HashData(bytes);

        // Take the first 4 bytes and convert to a positive integer
        var value = BitConverter.ToInt32(hash, 0);
        return Math.Abs(value);
    }

    private static List<string> ValidateCreateCommand(CreateFeatureFlagCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Key))
        {
            errors.Add("Flag key is required.");
        }
        else if (command.Key.Length > 100)
        {
            errors.Add("Flag key must not exceed 100 characters.");
        }
        else if (!IsValidKey(command.Key))
        {
            errors.Add("Flag key must contain only lowercase letters, numbers, underscores, and hyphens.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Flag name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Flag name must not exceed 200 characters.");
        }

        if (command.Description != null && command.Description.Length > 1000)
        {
            errors.Add("Flag description must not exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        var targetValueErrors = ValidateTargetValue(command.TargetType, command.TargetValue);
        errors.AddRange(targetValueErrors);

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateFeatureFlagCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Feature flag ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Key))
        {
            errors.Add("Flag key is required.");
        }
        else if (command.Key.Length > 100)
        {
            errors.Add("Flag key must not exceed 100 characters.");
        }
        else if (!IsValidKey(command.Key))
        {
            errors.Add("Flag key must contain only lowercase letters, numbers, underscores, and hyphens.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Flag name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Flag name must not exceed 200 characters.");
        }

        if (command.Description != null && command.Description.Length > 1000)
        {
            errors.Add("Flag description must not exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.UpdatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        var targetValueErrors = ValidateTargetValue(command.TargetType, command.TargetValue);
        errors.AddRange(targetValueErrors);

        return errors;
    }

    private static List<string> ValidateTargetValue(FeatureFlagTargetType targetType, string? targetValue)
    {
        var errors = new List<string>();

        switch (targetType)
        {
            case FeatureFlagTargetType.SpecificSellers:
                if (string.IsNullOrWhiteSpace(targetValue))
                {
                    errors.Add("Target value is required for SpecificSellers target type.");
                }
                else
                {
                    try
                    {
                        var sellerIds = JsonSerializer.Deserialize<List<string>>(targetValue);
                        if (sellerIds == null || sellerIds.Count == 0)
                        {
                            errors.Add("Target value must be a non-empty JSON array of seller IDs.");
                        }
                    }
                    catch (JsonException)
                    {
                        errors.Add("Target value must be a valid JSON array of seller IDs.");
                    }
                }
                break;

            case FeatureFlagTargetType.PercentageRollout:
                if (string.IsNullOrWhiteSpace(targetValue))
                {
                    errors.Add("Target value is required for PercentageRollout target type.");
                }
                else if (!int.TryParse(targetValue, out var percentage) || percentage < 0 || percentage > 100)
                {
                    errors.Add("Target value must be a number between 0 and 100 for percentage rollout.");
                }
                break;
        }

        return errors;
    }

    private static bool IsValidKey(string key)
    {
        return key.All(c => char.IsLower(c) || char.IsDigit(c) || c == '_' || c == '-');
    }

    private static string SerializeFeatureFlag(FeatureFlag flag)
    {
        var data = new
        {
            flag.Id,
            flag.Key,
            flag.Name,
            flag.Description,
            flag.IsEnabled,
            Environment = flag.Environment.ToString(),
            TargetType = flag.TargetType.ToString(),
            flag.TargetValue,
            flag.CreatedAt,
            flag.CreatedByUserId,
            flag.UpdatedAt,
            flag.UpdatedByUserId
        };

        return JsonSerializer.Serialize(data);
    }
}
