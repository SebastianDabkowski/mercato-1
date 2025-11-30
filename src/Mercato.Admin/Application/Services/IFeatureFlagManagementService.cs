using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing feature flags from the admin panel.
/// </summary>
public interface IFeatureFlagManagementService
{
    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    /// <returns>The result containing all feature flags.</returns>
    Task<GetFeatureFlagsResult> GetAllFlagsAsync();

    /// <summary>
    /// Gets all feature flags for a specific environment.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <returns>The result containing feature flags for the specified environment.</returns>
    Task<GetFeatureFlagsResult> GetFlagsByEnvironmentAsync(FeatureFlagEnvironment environment);

    /// <summary>
    /// Gets a specific feature flag by ID.
    /// </summary>
    /// <param name="id">The feature flag identifier.</param>
    /// <returns>The result containing the feature flag if found.</returns>
    Task<GetFeatureFlagResult> GetFlagByIdAsync(Guid id);

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    /// <param name="command">The command containing flag details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateFeatureFlagResult> CreateFlagAsync(CreateFeatureFlagCommand command);

    /// <summary>
    /// Updates an existing feature flag.
    /// </summary>
    /// <param name="command">The command containing updated flag details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateFeatureFlagResult> UpdateFlagAsync(UpdateFeatureFlagCommand command);

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    /// <param name="id">The feature flag ID to delete.</param>
    /// <param name="deletedByUserId">The user ID performing the deletion.</param>
    /// <param name="deletedByUserEmail">The email of the user performing the deletion.</param>
    /// <returns>The result of the deletion operation.</returns>
    Task<DeleteFeatureFlagResult> DeleteFlagAsync(Guid id, string deletedByUserId, string? deletedByUserEmail = null);

    /// <summary>
    /// Toggles a feature flag on or off.
    /// </summary>
    /// <param name="id">The feature flag ID to toggle.</param>
    /// <param name="isEnabled">The new enabled state.</param>
    /// <param name="updatedByUserId">The user ID performing the toggle.</param>
    /// <param name="updatedByUserEmail">The email of the user performing the toggle.</param>
    /// <returns>The result of the toggle operation.</returns>
    Task<ToggleFeatureFlagResult> ToggleFlagAsync(Guid id, bool isEnabled, string updatedByUserId, string? updatedByUserEmail = null);

    /// <summary>
    /// Evaluates a feature flag for a specific user context.
    /// </summary>
    /// <param name="key">The feature flag key.</param>
    /// <param name="environment">The target environment.</param>
    /// <param name="userId">The optional user ID for targeting evaluation.</param>
    /// <param name="sellerId">The optional seller ID for seller-specific targeting.</param>
    /// <returns>The result of the evaluation.</returns>
    Task<EvaluateFeatureFlagResult> EvaluateFlagAsync(string key, FeatureFlagEnvironment environment, string? userId = null, string? sellerId = null);

    /// <summary>
    /// Gets the history of changes for a specific feature flag.
    /// </summary>
    /// <param name="featureFlagId">The feature flag ID.</param>
    /// <returns>The result containing the flag history.</returns>
    Task<GetFeatureFlagHistoryResult> GetFlagHistoryAsync(Guid featureFlagId);
}

/// <summary>
/// Result of getting all feature flags.
/// </summary>
public class GetFeatureFlagsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the feature flags.
    /// </summary>
    public IReadOnlyList<FeatureFlag> Flags { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with feature flags.
    /// </summary>
    /// <param name="flags">The feature flags.</param>
    /// <returns>A successful result.</returns>
    public static GetFeatureFlagsResult Success(IReadOnlyList<FeatureFlag> flags) => new()
    {
        Succeeded = true,
        Errors = [],
        Flags = flags
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetFeatureFlagsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetFeatureFlagsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetFeatureFlagsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single feature flag.
/// </summary>
public class GetFeatureFlagResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the feature flag.
    /// </summary>
    public FeatureFlag? Flag { get; private init; }

    /// <summary>
    /// Creates a successful result with a feature flag.
    /// </summary>
    /// <param name="flag">The feature flag.</param>
    /// <returns>A successful result.</returns>
    public static GetFeatureFlagResult Success(FeatureFlag flag) => new()
    {
        Succeeded = true,
        Errors = [],
        Flag = flag
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetFeatureFlagResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetFeatureFlagResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetFeatureFlagResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new feature flag.
/// </summary>
public class CreateFeatureFlagCommand
{
    /// <summary>
    /// Gets or sets the unique key identifier for the flag.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the flag.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the flag is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the target environment.
    /// </summary>
    public FeatureFlagEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the targeting type.
    /// </summary>
    public FeatureFlagTargetType TargetType { get; set; }

    /// <summary>
    /// Gets or sets the target value (JSON for specific sellers or percentage).
    /// </summary>
    public string? TargetValue { get; set; }

    /// <summary>
    /// Gets or sets the user ID creating this flag.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user creating this flag.
    /// </summary>
    public string? CreatedByUserEmail { get; set; }
}

/// <summary>
/// Result of creating a feature flag.
/// </summary>
public class CreateFeatureFlagResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the created feature flag.
    /// </summary>
    public FeatureFlag? Flag { get; private init; }

    /// <summary>
    /// Creates a successful result with the created flag.
    /// </summary>
    /// <param name="flag">The created feature flag.</param>
    /// <returns>A successful result.</returns>
    public static CreateFeatureFlagResult Success(FeatureFlag flag) => new()
    {
        Succeeded = true,
        Errors = [],
        Flag = flag
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateFeatureFlagResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateFeatureFlagResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateFeatureFlagResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing feature flag.
/// </summary>
public class UpdateFeatureFlagCommand
{
    /// <summary>
    /// Gets or sets the ID of the flag to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique key identifier for the flag.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the flag.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the flag is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the target environment.
    /// </summary>
    public FeatureFlagEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the targeting type.
    /// </summary>
    public FeatureFlagTargetType TargetType { get; set; }

    /// <summary>
    /// Gets or sets the target value (JSON for specific sellers or percentage).
    /// </summary>
    public string? TargetValue { get; set; }

    /// <summary>
    /// Gets or sets the user ID updating this flag.
    /// </summary>
    public string UpdatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user updating this flag.
    /// </summary>
    public string? UpdatedByUserEmail { get; set; }
}

/// <summary>
/// Result of updating a feature flag.
/// </summary>
public class UpdateFeatureFlagResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the updated feature flag.
    /// </summary>
    public FeatureFlag? Flag { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated flag.
    /// </summary>
    /// <param name="flag">The updated feature flag.</param>
    /// <returns>A successful result.</returns>
    public static UpdateFeatureFlagResult Success(FeatureFlag flag) => new()
    {
        Succeeded = true,
        Errors = [],
        Flag = flag
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateFeatureFlagResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateFeatureFlagResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateFeatureFlagResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of deleting a feature flag.
/// </summary>
public class DeleteFeatureFlagResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static DeleteFeatureFlagResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DeleteFeatureFlagResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DeleteFeatureFlagResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DeleteFeatureFlagResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of toggling a feature flag.
/// </summary>
public class ToggleFeatureFlagResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the toggled feature flag.
    /// </summary>
    public FeatureFlag? Flag { get; private init; }

    /// <summary>
    /// Creates a successful result with the toggled flag.
    /// </summary>
    /// <param name="flag">The toggled feature flag.</param>
    /// <returns>A successful result.</returns>
    public static ToggleFeatureFlagResult Success(FeatureFlag flag) => new()
    {
        Succeeded = true,
        Errors = [],
        Flag = flag
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ToggleFeatureFlagResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ToggleFeatureFlagResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ToggleFeatureFlagResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of evaluating a feature flag.
/// </summary>
public class EvaluateFeatureFlagResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the flag is enabled for the given context.
    /// </summary>
    public bool IsEnabled { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the flag was found.
    /// </summary>
    public bool FlagFound { get; private init; }

    /// <summary>
    /// Gets the evaluated feature flag.
    /// </summary>
    public FeatureFlag? Flag { get; private init; }

    /// <summary>
    /// Creates a successful result with the evaluation outcome.
    /// </summary>
    /// <param name="isEnabled">Whether the flag is enabled for the context.</param>
    /// <param name="flag">The evaluated feature flag.</param>
    /// <returns>A successful result.</returns>
    public static EvaluateFeatureFlagResult Success(bool isEnabled, FeatureFlag flag) => new()
    {
        Succeeded = true,
        Errors = [],
        IsEnabled = isEnabled,
        FlagFound = true,
        Flag = flag
    };

    /// <summary>
    /// Creates a successful result indicating the flag was not found.
    /// </summary>
    /// <returns>A successful result with flag not found.</returns>
    public static EvaluateFeatureFlagResult FlagNotFound() => new()
    {
        Succeeded = true,
        Errors = [],
        IsEnabled = false,
        FlagFound = false,
        Flag = null
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static EvaluateFeatureFlagResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static EvaluateFeatureFlagResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static EvaluateFeatureFlagResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting feature flag history.
/// </summary>
public class GetFeatureFlagHistoryResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the feature flag history records.
    /// </summary>
    public IReadOnlyList<FeatureFlagHistory> History { get; private init; } = [];

    /// <summary>
    /// Gets the feature flag for context.
    /// </summary>
    public FeatureFlag? Flag { get; private init; }

    /// <summary>
    /// Creates a successful result with history records.
    /// </summary>
    /// <param name="history">The history records.</param>
    /// <param name="flag">The feature flag.</param>
    /// <returns>A successful result.</returns>
    public static GetFeatureFlagHistoryResult Success(IReadOnlyList<FeatureFlagHistory> history, FeatureFlag? flag) => new()
    {
        Succeeded = true,
        Errors = [],
        History = history,
        Flag = flag
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetFeatureFlagHistoryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetFeatureFlagHistoryResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetFeatureFlagHistoryResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
