using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing the GDPR data processing registry.
/// </summary>
public interface IDataProcessingRegistryService
{
    /// <summary>
    /// Gets all data processing activities.
    /// </summary>
    /// <returns>The result containing all activities.</returns>
    Task<GetDataProcessingActivitiesResult> GetAllActivitiesAsync();

    /// <summary>
    /// Gets all active data processing activities.
    /// </summary>
    /// <returns>The result containing active activities.</returns>
    Task<GetDataProcessingActivitiesResult> GetActiveActivitiesAsync();

    /// <summary>
    /// Gets a specific data processing activity by ID.
    /// </summary>
    /// <param name="id">The activity identifier.</param>
    /// <returns>The result containing the activity if found.</returns>
    Task<GetDataProcessingActivityResult> GetActivityByIdAsync(Guid id);

    /// <summary>
    /// Gets the change history for a specific data processing activity.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <returns>The result containing the activity history.</returns>
    Task<GetDataProcessingActivityHistoryResult> GetActivityHistoryAsync(Guid activityId);

    /// <summary>
    /// Creates a new data processing activity.
    /// </summary>
    /// <param name="command">The command containing activity details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateDataProcessingActivityResult> CreateActivityAsync(CreateDataProcessingActivityCommand command);

    /// <summary>
    /// Updates an existing data processing activity.
    /// </summary>
    /// <param name="command">The command containing updated activity details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateDataProcessingActivityResult> UpdateActivityAsync(UpdateDataProcessingActivityCommand command);

    /// <summary>
    /// Deactivates a data processing activity.
    /// </summary>
    /// <param name="id">The activity identifier.</param>
    /// <param name="userId">The user ID performing the deactivation.</param>
    /// <param name="reason">Optional reason for deactivation.</param>
    /// <returns>The result of the deactivation operation.</returns>
    Task<DeactivateDataProcessingActivityResult> DeactivateActivityAsync(Guid id, string userId, string? reason = null);

    /// <summary>
    /// Exports all data processing activities to CSV format.
    /// </summary>
    /// <returns>The result containing the CSV content.</returns>
    Task<ExportDataProcessingActivitiesResult> ExportToCsvAsync();
}

/// <summary>
/// Result of getting all data processing activities.
/// </summary>
public class GetDataProcessingActivitiesResult
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
    /// Gets the data processing activities.
    /// </summary>
    public IReadOnlyList<DataProcessingActivity> Activities { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with activities.
    /// </summary>
    /// <param name="activities">The data processing activities.</param>
    /// <returns>A successful result.</returns>
    public static GetDataProcessingActivitiesResult Success(IReadOnlyList<DataProcessingActivity> activities) => new()
    {
        Succeeded = true,
        Errors = [],
        Activities = activities
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetDataProcessingActivitiesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetDataProcessingActivitiesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetDataProcessingActivitiesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single data processing activity.
/// </summary>
public class GetDataProcessingActivityResult
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
    /// Gets the data processing activity.
    /// </summary>
    public DataProcessingActivity? Activity { get; private init; }

    /// <summary>
    /// Creates a successful result with an activity.
    /// </summary>
    /// <param name="activity">The data processing activity.</param>
    /// <returns>A successful result.</returns>
    public static GetDataProcessingActivityResult Success(DataProcessingActivity activity) => new()
    {
        Succeeded = true,
        Errors = [],
        Activity = activity
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetDataProcessingActivityResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetDataProcessingActivityResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetDataProcessingActivityResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting data processing activity history.
/// </summary>
public class GetDataProcessingActivityHistoryResult
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
    /// Gets the activity history records.
    /// </summary>
    public IReadOnlyList<DataProcessingActivityHistory> History { get; private init; } = [];

    /// <summary>
    /// Gets the related data processing activity.
    /// </summary>
    public DataProcessingActivity? Activity { get; private init; }

    /// <summary>
    /// Creates a successful result with history.
    /// </summary>
    /// <param name="history">The history records.</param>
    /// <param name="activity">The related activity.</param>
    /// <returns>A successful result.</returns>
    public static GetDataProcessingActivityHistoryResult Success(IReadOnlyList<DataProcessingActivityHistory> history, DataProcessingActivity? activity) => new()
    {
        Succeeded = true,
        Errors = [],
        History = history,
        Activity = activity
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetDataProcessingActivityHistoryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetDataProcessingActivityHistoryResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetDataProcessingActivityHistoryResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new data processing activity.
/// </summary>
public class CreateDataProcessingActivityCommand
{
    /// <summary>
    /// Gets or sets the name of the processing activity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose(s) of the processing.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal basis for processing.
    /// </summary>
    public string LegalBasis { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of personal data being processed.
    /// </summary>
    public string DataCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of data subjects.
    /// </summary>
    public string DataSubjectCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients to whom data is disclosed.
    /// </summary>
    public string Recipients { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets information about third country transfers.
    /// </summary>
    public string? ThirdCountryTransfers { get; set; }

    /// <summary>
    /// Gets or sets the retention period description.
    /// </summary>
    public string RetentionPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the technical security measures.
    /// </summary>
    public string? TechnicalMeasures { get; set; }

    /// <summary>
    /// Gets or sets the organizational security measures.
    /// </summary>
    public string? OrganizationalMeasures { get; set; }

    /// <summary>
    /// Gets or sets the data processor name.
    /// </summary>
    public string? ProcessorName { get; set; }

    /// <summary>
    /// Gets or sets the data processor contact information.
    /// </summary>
    public string? ProcessorContact { get; set; }

    /// <summary>
    /// Gets or sets the user ID creating this activity.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user creating this activity.
    /// </summary>
    public string? CreatedByUserEmail { get; set; }
}

/// <summary>
/// Result of creating a data processing activity.
/// </summary>
public class CreateDataProcessingActivityResult
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
    /// Gets the created activity.
    /// </summary>
    public DataProcessingActivity? Activity { get; private init; }

    /// <summary>
    /// Creates a successful result with the created activity.
    /// </summary>
    /// <param name="activity">The created activity.</param>
    /// <returns>A successful result.</returns>
    public static CreateDataProcessingActivityResult Success(DataProcessingActivity activity) => new()
    {
        Succeeded = true,
        Errors = [],
        Activity = activity
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateDataProcessingActivityResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateDataProcessingActivityResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateDataProcessingActivityResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing data processing activity.
/// </summary>
public class UpdateDataProcessingActivityCommand
{
    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the processing activity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose(s) of the processing.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal basis for processing.
    /// </summary>
    public string LegalBasis { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of personal data being processed.
    /// </summary>
    public string DataCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of data subjects.
    /// </summary>
    public string DataSubjectCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients to whom data is disclosed.
    /// </summary>
    public string Recipients { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets information about third country transfers.
    /// </summary>
    public string? ThirdCountryTransfers { get; set; }

    /// <summary>
    /// Gets or sets the retention period description.
    /// </summary>
    public string RetentionPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the technical security measures.
    /// </summary>
    public string? TechnicalMeasures { get; set; }

    /// <summary>
    /// Gets or sets the organizational security measures.
    /// </summary>
    public string? OrganizationalMeasures { get; set; }

    /// <summary>
    /// Gets or sets the data processor name.
    /// </summary>
    public string? ProcessorName { get; set; }

    /// <summary>
    /// Gets or sets the data processor contact information.
    /// </summary>
    public string? ProcessorContact { get; set; }

    /// <summary>
    /// Gets or sets the user ID updating this activity.
    /// </summary>
    public string UpdatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user updating this activity.
    /// </summary>
    public string? UpdatedByUserEmail { get; set; }

    /// <summary>
    /// Gets or sets an optional reason for the update.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Result of updating a data processing activity.
/// </summary>
public class UpdateDataProcessingActivityResult
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
    /// Gets the updated activity.
    /// </summary>
    public DataProcessingActivity? Activity { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated activity.
    /// </summary>
    /// <param name="activity">The updated activity.</param>
    /// <returns>A successful result.</returns>
    public static UpdateDataProcessingActivityResult Success(DataProcessingActivity activity) => new()
    {
        Succeeded = true,
        Errors = [],
        Activity = activity
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateDataProcessingActivityResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateDataProcessingActivityResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateDataProcessingActivityResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of deactivating a data processing activity.
/// </summary>
public class DeactivateDataProcessingActivityResult
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
    /// Gets the deactivated activity.
    /// </summary>
    public DataProcessingActivity? Activity { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="activity">The deactivated activity.</param>
    /// <returns>A successful result.</returns>
    public static DeactivateDataProcessingActivityResult Success(DataProcessingActivity activity) => new()
    {
        Succeeded = true,
        Errors = [],
        Activity = activity
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DeactivateDataProcessingActivityResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DeactivateDataProcessingActivityResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DeactivateDataProcessingActivityResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of exporting data processing activities.
/// </summary>
public class ExportDataProcessingActivitiesResult
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
    /// Gets the CSV content.
    /// </summary>
    public string CsvContent { get; private init; } = string.Empty;

    /// <summary>
    /// Gets the suggested file name.
    /// </summary>
    public string FileName { get; private init; } = string.Empty;

    /// <summary>
    /// Creates a successful result with CSV content.
    /// </summary>
    /// <param name="csvContent">The CSV content.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>A successful result.</returns>
    public static ExportDataProcessingActivitiesResult Success(string csvContent, string fileName) => new()
    {
        Succeeded = true,
        Errors = [],
        CsvContent = csvContent,
        FileName = fileName
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ExportDataProcessingActivitiesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ExportDataProcessingActivitiesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ExportDataProcessingActivitiesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
