using System.Text;
using System.Text.Json;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing the GDPR data processing registry.
/// </summary>
public class DataProcessingRegistryService : IDataProcessingRegistryService
{
    private readonly IDataProcessingActivityRepository _activityRepository;
    private readonly IDataProcessingActivityHistoryRepository _historyRepository;
    private readonly ILogger<DataProcessingRegistryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProcessingRegistryService"/> class.
    /// </summary>
    /// <param name="activityRepository">The data processing activity repository.</param>
    /// <param name="historyRepository">The data processing activity history repository.</param>
    /// <param name="logger">The logger.</param>
    public DataProcessingRegistryService(
        IDataProcessingActivityRepository activityRepository,
        IDataProcessingActivityHistoryRepository historyRepository,
        ILogger<DataProcessingRegistryService> logger)
    {
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<GetDataProcessingActivitiesResult> GetAllActivitiesAsync()
    {
        try
        {
            var activities = await _activityRepository.GetAllAsync();
            return GetDataProcessingActivitiesResult.Success(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all data processing activities");
            return GetDataProcessingActivitiesResult.Failure("An error occurred while retrieving data processing activities.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetDataProcessingActivitiesResult> GetActiveActivitiesAsync()
    {
        try
        {
            var activities = await _activityRepository.GetActiveAsync();
            return GetDataProcessingActivitiesResult.Success(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active data processing activities");
            return GetDataProcessingActivitiesResult.Failure("An error occurred while retrieving active data processing activities.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetDataProcessingActivityResult> GetActivityByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetDataProcessingActivityResult.Failure("Activity ID is required.");
        }

        try
        {
            var activity = await _activityRepository.GetByIdAsync(id);
            if (activity == null)
            {
                return GetDataProcessingActivityResult.Failure($"Data processing activity with ID {id} not found.");
            }

            return GetDataProcessingActivityResult.Success(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data processing activity by ID {Id}", id);
            return GetDataProcessingActivityResult.Failure("An error occurred while retrieving the data processing activity.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetDataProcessingActivityHistoryResult> GetActivityHistoryAsync(Guid activityId)
    {
        if (activityId == Guid.Empty)
        {
            return GetDataProcessingActivityHistoryResult.Failure("Activity ID is required.");
        }

        try
        {
            var activity = await _activityRepository.GetByIdAsync(activityId);
            var history = await _historyRepository.GetByActivityIdAsync(activityId);
            return GetDataProcessingActivityHistoryResult.Success(history, activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for data processing activity {ActivityId}", activityId);
            return GetDataProcessingActivityHistoryResult.Failure("An error occurred while retrieving the activity history.");
        }
    }

    /// <inheritdoc/>
    public async Task<CreateDataProcessingActivityResult> CreateActivityAsync(CreateDataProcessingActivityCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateDataProcessingActivityResult.Failure(validationErrors);
        }

        try
        {
            var activity = new DataProcessingActivity
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Purpose = command.Purpose,
                LegalBasis = command.LegalBasis,
                DataCategories = command.DataCategories,
                DataSubjectCategories = command.DataSubjectCategories,
                Recipients = command.Recipients,
                ThirdCountryTransfers = command.ThirdCountryTransfers,
                RetentionPeriod = command.RetentionPeriod,
                TechnicalMeasures = command.TechnicalMeasures,
                OrganizationalMeasures = command.OrganizationalMeasures,
                ProcessorName = command.ProcessorName,
                ProcessorContact = command.ProcessorContact,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = command.CreatedByUserId
            };

            var created = await _activityRepository.AddAsync(activity);

            // Record history
            var historyRecord = new DataProcessingActivityHistory
            {
                Id = Guid.NewGuid(),
                DataProcessingActivityId = created.Id,
                ChangeType = "Created",
                PreviousValues = null,
                NewValues = SerializeActivity(created),
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedByUserId = command.CreatedByUserId,
                ChangedByUserEmail = command.CreatedByUserEmail,
                Reason = "Initial creation"
            };

            await _historyRepository.AddAsync(historyRecord);

            _logger.LogInformation("Created data processing activity {ActivityId} '{Name}' by user {UserId}",
                created.Id, created.Name, command.CreatedByUserId);

            return CreateDataProcessingActivityResult.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data processing activity");
            return CreateDataProcessingActivityResult.Failure("An error occurred while creating the data processing activity.");
        }
    }

    /// <inheritdoc/>
    public async Task<UpdateDataProcessingActivityResult> UpdateActivityAsync(UpdateDataProcessingActivityCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateDataProcessingActivityResult.Failure(validationErrors);
        }

        try
        {
            var activity = await _activityRepository.GetByIdAsync(command.Id);
            if (activity == null)
            {
                return UpdateDataProcessingActivityResult.Failure($"Data processing activity with ID {command.Id} not found.");
            }

            // Capture previous state
            var previousValues = SerializeActivity(activity);

            // Update activity
            activity.Name = command.Name;
            activity.Purpose = command.Purpose;
            activity.LegalBasis = command.LegalBasis;
            activity.DataCategories = command.DataCategories;
            activity.DataSubjectCategories = command.DataSubjectCategories;
            activity.Recipients = command.Recipients;
            activity.ThirdCountryTransfers = command.ThirdCountryTransfers;
            activity.RetentionPeriod = command.RetentionPeriod;
            activity.TechnicalMeasures = command.TechnicalMeasures;
            activity.OrganizationalMeasures = command.OrganizationalMeasures;
            activity.ProcessorName = command.ProcessorName;
            activity.ProcessorContact = command.ProcessorContact;
            activity.UpdatedAt = DateTimeOffset.UtcNow;
            activity.UpdatedByUserId = command.UpdatedByUserId;

            await _activityRepository.UpdateAsync(activity);

            // Record history
            var historyRecord = new DataProcessingActivityHistory
            {
                Id = Guid.NewGuid(),
                DataProcessingActivityId = activity.Id,
                ChangeType = "Updated",
                PreviousValues = previousValues,
                NewValues = SerializeActivity(activity),
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedByUserId = command.UpdatedByUserId,
                ChangedByUserEmail = command.UpdatedByUserEmail,
                Reason = command.Reason
            };

            await _historyRepository.AddAsync(historyRecord);

            _logger.LogInformation("Updated data processing activity {ActivityId} by user {UserId}",
                activity.Id, command.UpdatedByUserId);

            return UpdateDataProcessingActivityResult.Success(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data processing activity {ActivityId}", command.Id);
            return UpdateDataProcessingActivityResult.Failure("An error occurred while updating the data processing activity.");
        }
    }

    /// <inheritdoc/>
    public async Task<DeactivateDataProcessingActivityResult> DeactivateActivityAsync(Guid id, string userId, string? reason = null)
    {
        if (id == Guid.Empty)
        {
            return DeactivateDataProcessingActivityResult.Failure("Activity ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return DeactivateDataProcessingActivityResult.Failure("User ID is required.");
        }

        try
        {
            var activity = await _activityRepository.GetByIdAsync(id);
            if (activity == null)
            {
                return DeactivateDataProcessingActivityResult.Failure($"Data processing activity with ID {id} not found.");
            }

            if (!activity.IsActive)
            {
                return DeactivateDataProcessingActivityResult.Failure("Activity is already deactivated.");
            }

            // Capture previous state
            var previousValues = SerializeActivity(activity);

            // Deactivate
            activity.IsActive = false;
            activity.UpdatedAt = DateTimeOffset.UtcNow;
            activity.UpdatedByUserId = userId;

            await _activityRepository.UpdateAsync(activity);

            // Record history
            var historyRecord = new DataProcessingActivityHistory
            {
                Id = Guid.NewGuid(),
                DataProcessingActivityId = activity.Id,
                ChangeType = "Deactivated",
                PreviousValues = previousValues,
                NewValues = SerializeActivity(activity),
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedByUserId = userId,
                Reason = reason ?? "Deactivated by admin"
            };

            await _historyRepository.AddAsync(historyRecord);

            _logger.LogInformation("Deactivated data processing activity {ActivityId} by user {UserId}",
                activity.Id, userId);

            return DeactivateDataProcessingActivityResult.Success(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating data processing activity {ActivityId}", id);
            return DeactivateDataProcessingActivityResult.Failure("An error occurred while deactivating the data processing activity.");
        }
    }

    /// <inheritdoc/>
    public async Task<ExportDataProcessingActivitiesResult> ExportToCsvAsync()
    {
        try
        {
            var activities = await _activityRepository.GetAllAsync();

            var csv = new StringBuilder();

            // Header row
            csv.AppendLine("\"ID\",\"Name\",\"Purpose\",\"Legal Basis\",\"Data Categories\",\"Data Subject Categories\",\"Recipients\",\"Third Country Transfers\",\"Retention Period\",\"Technical Measures\",\"Organizational Measures\",\"Processor Name\",\"Processor Contact\",\"Is Active\",\"Created At\",\"Created By\",\"Updated At\",\"Updated By\"");

            // Data rows
            foreach (var activity in activities)
            {
                var fields = new[]
                {
                    EscapeCsvField(activity.Id.ToString()),
                    EscapeCsvField(activity.Name),
                    EscapeCsvField(activity.Purpose),
                    EscapeCsvField(activity.LegalBasis),
                    EscapeCsvField(activity.DataCategories),
                    EscapeCsvField(activity.DataSubjectCategories),
                    EscapeCsvField(activity.Recipients),
                    EscapeCsvField(activity.ThirdCountryTransfers ?? string.Empty),
                    EscapeCsvField(activity.RetentionPeriod),
                    EscapeCsvField(activity.TechnicalMeasures ?? string.Empty),
                    EscapeCsvField(activity.OrganizationalMeasures ?? string.Empty),
                    EscapeCsvField(activity.ProcessorName ?? string.Empty),
                    EscapeCsvField(activity.ProcessorContact ?? string.Empty),
                    EscapeCsvField(activity.IsActive ? "Yes" : "No"),
                    EscapeCsvField(activity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsvField(activity.CreatedByUserId),
                    EscapeCsvField(activity.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                    EscapeCsvField(activity.UpdatedByUserId ?? string.Empty)
                };

                csv.AppendLine(string.Join(",", fields));
            }

            var fileName = $"data_processing_registry_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.csv";

            _logger.LogInformation("Exported {Count} data processing activities to CSV", activities.Count);

            return ExportDataProcessingActivitiesResult.Success(csv.ToString(), fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data processing activities to CSV");
            return ExportDataProcessingActivitiesResult.Failure("An error occurred while exporting data processing activities.");
        }
    }

    private static List<string> ValidateCreateCommand(CreateDataProcessingActivityCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Purpose))
        {
            errors.Add("Purpose is required.");
        }
        else if (command.Purpose.Length > 2000)
        {
            errors.Add("Purpose must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.LegalBasis))
        {
            errors.Add("Legal basis is required.");
        }
        else if (command.LegalBasis.Length > 500)
        {
            errors.Add("Legal basis must not exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataCategories))
        {
            errors.Add("Data categories is required.");
        }
        else if (command.DataCategories.Length > 2000)
        {
            errors.Add("Data categories must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataSubjectCategories))
        {
            errors.Add("Data subject categories is required.");
        }
        else if (command.DataSubjectCategories.Length > 2000)
        {
            errors.Add("Data subject categories must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Recipients))
        {
            errors.Add("Recipients is required.");
        }
        else if (command.Recipients.Length > 2000)
        {
            errors.Add("Recipients must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.RetentionPeriod))
        {
            errors.Add("Retention period is required.");
        }
        else if (command.RetentionPeriod.Length > 500)
        {
            errors.Add("Retention period must not exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        // Optional field length validations
        if (command.ThirdCountryTransfers?.Length > 2000)
        {
            errors.Add("Third country transfers must not exceed 2000 characters.");
        }

        if (command.TechnicalMeasures?.Length > 2000)
        {
            errors.Add("Technical measures must not exceed 2000 characters.");
        }

        if (command.OrganizationalMeasures?.Length > 2000)
        {
            errors.Add("Organizational measures must not exceed 2000 characters.");
        }

        if (command.ProcessorName?.Length > 200)
        {
            errors.Add("Processor name must not exceed 200 characters.");
        }

        if (command.ProcessorContact?.Length > 500)
        {
            errors.Add("Processor contact must not exceed 500 characters.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateDataProcessingActivityCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Activity ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Purpose))
        {
            errors.Add("Purpose is required.");
        }
        else if (command.Purpose.Length > 2000)
        {
            errors.Add("Purpose must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.LegalBasis))
        {
            errors.Add("Legal basis is required.");
        }
        else if (command.LegalBasis.Length > 500)
        {
            errors.Add("Legal basis must not exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataCategories))
        {
            errors.Add("Data categories is required.");
        }
        else if (command.DataCategories.Length > 2000)
        {
            errors.Add("Data categories must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataSubjectCategories))
        {
            errors.Add("Data subject categories is required.");
        }
        else if (command.DataSubjectCategories.Length > 2000)
        {
            errors.Add("Data subject categories must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Recipients))
        {
            errors.Add("Recipients is required.");
        }
        else if (command.Recipients.Length > 2000)
        {
            errors.Add("Recipients must not exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.RetentionPeriod))
        {
            errors.Add("Retention period is required.");
        }
        else if (command.RetentionPeriod.Length > 500)
        {
            errors.Add("Retention period must not exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.UpdatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        // Optional field length validations
        if (command.ThirdCountryTransfers?.Length > 2000)
        {
            errors.Add("Third country transfers must not exceed 2000 characters.");
        }

        if (command.TechnicalMeasures?.Length > 2000)
        {
            errors.Add("Technical measures must not exceed 2000 characters.");
        }

        if (command.OrganizationalMeasures?.Length > 2000)
        {
            errors.Add("Organizational measures must not exceed 2000 characters.");
        }

        if (command.ProcessorName?.Length > 200)
        {
            errors.Add("Processor name must not exceed 200 characters.");
        }

        if (command.ProcessorContact?.Length > 500)
        {
            errors.Add("Processor contact must not exceed 500 characters.");
        }

        return errors;
    }

    private static string SerializeActivity(DataProcessingActivity activity)
    {
        return JsonSerializer.Serialize(new
        {
            activity.Name,
            activity.Purpose,
            activity.LegalBasis,
            activity.DataCategories,
            activity.DataSubjectCategories,
            activity.Recipients,
            activity.ThirdCountryTransfers,
            activity.RetentionPeriod,
            activity.TechnicalMeasures,
            activity.OrganizationalMeasures,
            activity.ProcessorName,
            activity.ProcessorContact,
            activity.IsActive
        });
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "\"\"";
        }

        // Escape double quotes by doubling them, and wrap in quotes
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
