using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class DataProcessingRegistryServiceTests
{
    private readonly Mock<IDataProcessingActivityRepository> _mockActivityRepository;
    private readonly Mock<IDataProcessingActivityHistoryRepository> _mockHistoryRepository;
    private readonly Mock<ILogger<DataProcessingRegistryService>> _mockLogger;

    public DataProcessingRegistryServiceTests()
    {
        _mockActivityRepository = new Mock<IDataProcessingActivityRepository>(MockBehavior.Strict);
        _mockHistoryRepository = new Mock<IDataProcessingActivityHistoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<DataProcessingRegistryService>>();
    }

    private DataProcessingRegistryService CreateService()
    {
        return new DataProcessingRegistryService(
            _mockActivityRepository.Object,
            _mockHistoryRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullActivityRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DataProcessingRegistryService(null!, _mockHistoryRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHistoryRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DataProcessingRegistryService(_mockActivityRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DataProcessingRegistryService(_mockActivityRepository.Object, _mockHistoryRepository.Object, null!));
    }

    #endregion

    #region GetAllActivitiesAsync Tests

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsAllActivities()
    {
        // Arrange
        var service = CreateService();
        var activities = new List<DataProcessingActivity>
        {
            new DataProcessingActivity { Id = Guid.NewGuid(), Name = "Activity 1", IsActive = true },
            new DataProcessingActivity { Id = Guid.NewGuid(), Name = "Activity 2", IsActive = false }
        };

        _mockActivityRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await service.GetAllActivitiesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Activities.Count);
        _mockActivityRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockActivityRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataProcessingActivity>());

        // Act
        var result = await service.GetAllActivitiesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Activities);
    }

    #endregion

    #region GetActiveActivitiesAsync Tests

    [Fact]
    public async Task GetActiveActivitiesAsync_ReturnsOnlyActiveActivities()
    {
        // Arrange
        var service = CreateService();
        var activities = new List<DataProcessingActivity>
        {
            new DataProcessingActivity { Id = Guid.NewGuid(), Name = "Active Activity", IsActive = true }
        };

        _mockActivityRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await service.GetActiveActivitiesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Activities);
        Assert.True(result.Activities[0].IsActive);
        _mockActivityRepository.VerifyAll();
    }

    #endregion

    #region GetActivityByIdAsync Tests

    [Fact]
    public async Task GetActivityByIdAsync_ExistingActivity_ReturnsActivity()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();
        var activity = new DataProcessingActivity { Id = activityId, Name = "Test Activity" };

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await service.GetActivityByIdAsync(activityId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Activity);
        Assert.Equal(activityId, result.Activity.Id);
    }

    [Fact]
    public async Task GetActivityByIdAsync_NonExistingActivity_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivity?)null);

        // Act
        var result = await service.GetActivityByIdAsync(activityId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task GetActivityByIdAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetActivityByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Activity ID is required"));
    }

    #endregion

    #region GetActivityHistoryAsync Tests

    [Fact]
    public async Task GetActivityHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();
        var activity = new DataProcessingActivity { Id = activityId, Name = "Test Activity" };
        var history = new List<DataProcessingActivityHistory>
        {
            new DataProcessingActivityHistory { Id = Guid.NewGuid(), DataProcessingActivityId = activityId, ChangeType = "Created" }
        };

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        _mockHistoryRepository
            .Setup(r => r.GetByActivityIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetActivityHistoryAsync(activityId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.History);
        Assert.NotNull(result.Activity);
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task GetActivityHistoryAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetActivityHistoryAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Activity ID is required"));
    }

    #endregion

    #region CreateActivityAsync Tests

    [Fact]
    public async Task CreateActivityAsync_ValidCommand_CreatesActivity()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateDataProcessingActivityCommand
        {
            Name = "Customer Data Processing",
            Purpose = "Process customer orders",
            LegalBasis = "Contract",
            DataCategories = "Name, Email, Address",
            DataSubjectCategories = "Customers",
            Recipients = "Shipping Partners",
            RetentionPeriod = "7 years",
            CreatedByUserId = "admin-1"
        };

        _mockActivityRepository
            .Setup(r => r.AddAsync(It.IsAny<DataProcessingActivity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivity a, CancellationToken _) => a);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<DataProcessingActivityHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivityHistory h, CancellationToken _) => h);

        // Act
        var result = await service.CreateActivityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Activity);
        Assert.Equal(command.Name, result.Activity.Name);
        Assert.Equal(command.Purpose, result.Activity.Purpose);
        Assert.True(result.Activity.IsActive);
        _mockActivityRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateActivityAsync_MissingName_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateDataProcessingActivityCommand
        {
            Name = "",
            Purpose = "Process customer orders",
            LegalBasis = "Contract",
            DataCategories = "Name, Email, Address",
            DataSubjectCategories = "Customers",
            Recipients = "Shipping Partners",
            RetentionPeriod = "7 years",
            CreatedByUserId = "admin-1"
        };

        // Act
        var result = await service.CreateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Name is required"));
    }

    [Fact]
    public async Task CreateActivityAsync_MissingPurpose_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateDataProcessingActivityCommand
        {
            Name = "Customer Data Processing",
            Purpose = "",
            LegalBasis = "Contract",
            DataCategories = "Name, Email, Address",
            DataSubjectCategories = "Customers",
            Recipients = "Shipping Partners",
            RetentionPeriod = "7 years",
            CreatedByUserId = "admin-1"
        };

        // Act
        var result = await service.CreateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Purpose is required"));
    }

    [Fact]
    public async Task CreateActivityAsync_MissingLegalBasis_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateDataProcessingActivityCommand
        {
            Name = "Customer Data Processing",
            Purpose = "Process customer orders",
            LegalBasis = "",
            DataCategories = "Name, Email, Address",
            DataSubjectCategories = "Customers",
            Recipients = "Shipping Partners",
            RetentionPeriod = "7 years",
            CreatedByUserId = "admin-1"
        };

        // Act
        var result = await service.CreateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Legal basis is required"));
    }

    [Fact]
    public async Task CreateActivityAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateDataProcessingActivityCommand
        {
            Name = "Customer Data Processing",
            Purpose = "Process customer orders",
            LegalBasis = "Contract",
            DataCategories = "Name, Email, Address",
            DataSubjectCategories = "Customers",
            Recipients = "Shipping Partners",
            RetentionPeriod = "7 years",
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task CreateActivityAsync_NameTooLong_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateDataProcessingActivityCommand
        {
            Name = new string('x', 201),
            Purpose = "Process customer orders",
            LegalBasis = "Contract",
            DataCategories = "Name, Email, Address",
            DataSubjectCategories = "Customers",
            Recipients = "Shipping Partners",
            RetentionPeriod = "7 years",
            CreatedByUserId = "admin-1"
        };

        // Act
        var result = await service.CreateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Name must not exceed 200 characters"));
    }

    #endregion

    #region UpdateActivityAsync Tests

    [Fact]
    public async Task UpdateActivityAsync_ValidCommand_UpdatesActivity()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();
        var existingActivity = new DataProcessingActivity
        {
            Id = activityId,
            Name = "Old Name",
            Purpose = "Old Purpose",
            LegalBasis = "Consent",
            DataCategories = "Old Categories",
            DataSubjectCategories = "Old Subjects",
            Recipients = "Old Recipients",
            RetentionPeriod = "5 years",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedByUserId = "admin-1"
        };

        var command = new UpdateDataProcessingActivityCommand
        {
            Id = activityId,
            Name = "New Name",
            Purpose = "New Purpose",
            LegalBasis = "Contract",
            DataCategories = "New Categories",
            DataSubjectCategories = "New Subjects",
            Recipients = "New Recipients",
            RetentionPeriod = "7 years",
            UpdatedByUserId = "admin-2",
            Reason = "Updated for compliance"
        };

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingActivity);

        _mockActivityRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DataProcessingActivity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<DataProcessingActivityHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivityHistory h, CancellationToken _) => h);

        // Act
        var result = await service.UpdateActivityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Activity);
        Assert.Equal(command.Name, result.Activity.Name);
        Assert.Equal(command.Purpose, result.Activity.Purpose);
        _mockActivityRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateActivityAsync_NonExistingActivity_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();
        var command = new UpdateDataProcessingActivityCommand
        {
            Id = activityId,
            Name = "New Name",
            Purpose = "New Purpose",
            LegalBasis = "Contract",
            DataCategories = "New Categories",
            DataSubjectCategories = "New Subjects",
            Recipients = "New Recipients",
            RetentionPeriod = "7 years",
            UpdatedByUserId = "admin-2"
        };

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivity?)null);

        // Act
        var result = await service.UpdateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateActivityAsync_EmptyId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateDataProcessingActivityCommand
        {
            Id = Guid.Empty,
            Name = "New Name",
            Purpose = "New Purpose",
            LegalBasis = "Contract",
            DataCategories = "New Categories",
            DataSubjectCategories = "New Subjects",
            Recipients = "New Recipients",
            RetentionPeriod = "7 years",
            UpdatedByUserId = "admin-2"
        };

        // Act
        var result = await service.UpdateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Activity ID is required"));
    }

    [Fact]
    public async Task UpdateActivityAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateDataProcessingActivityCommand
        {
            Id = Guid.NewGuid(),
            Name = "New Name",
            Purpose = "New Purpose",
            LegalBasis = "Contract",
            DataCategories = "New Categories",
            DataSubjectCategories = "New Subjects",
            Recipients = "New Recipients",
            RetentionPeriod = "7 years",
            UpdatedByUserId = ""
        };

        // Act
        var result = await service.UpdateActivityAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region DeactivateActivityAsync Tests

    [Fact]
    public async Task DeactivateActivityAsync_ActiveActivity_DeactivatesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();
        var activity = new DataProcessingActivity
        {
            Id = activityId,
            Name = "Test Activity",
            Purpose = "Test Purpose",
            LegalBasis = "Contract",
            DataCategories = "Categories",
            DataSubjectCategories = "Subjects",
            Recipients = "Recipients",
            RetentionPeriod = "7 years",
            IsActive = true
        };

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        _mockActivityRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DataProcessingActivity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<DataProcessingActivityHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivityHistory h, CancellationToken _) => h);

        // Act
        var result = await service.DeactivateActivityAsync(activityId, "admin-1", "No longer needed");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Activity);
        Assert.False(result.Activity.IsActive);
        _mockActivityRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task DeactivateActivityAsync_AlreadyInactiveActivity_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();
        var activity = new DataProcessingActivity
        {
            Id = activityId,
            Name = "Test Activity",
            IsActive = false
        };

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await service.DeactivateActivityAsync(activityId, "admin-1", "Test reason");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already deactivated"));
    }

    [Fact]
    public async Task DeactivateActivityAsync_NonExistingActivity_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var activityId = Guid.NewGuid();

        _mockActivityRepository
            .Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataProcessingActivity?)null);

        // Act
        var result = await service.DeactivateActivityAsync(activityId, "admin-1", "Test reason");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeactivateActivityAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeactivateActivityAsync(Guid.Empty, "admin-1", "Test reason");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Activity ID is required"));
    }

    [Fact]
    public async Task DeactivateActivityAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeactivateActivityAsync(Guid.NewGuid(), "", "Test reason");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region ExportToCsvAsync Tests

    [Fact]
    public async Task ExportToCsvAsync_WithActivities_ReturnsCsvContent()
    {
        // Arrange
        var service = CreateService();
        var activities = new List<DataProcessingActivity>
        {
            new DataProcessingActivity
            {
                Id = Guid.NewGuid(),
                Name = "Activity 1",
                Purpose = "Purpose 1",
                LegalBasis = "Contract",
                DataCategories = "Categories 1",
                DataSubjectCategories = "Subjects 1",
                Recipients = "Recipients 1",
                RetentionPeriod = "7 years",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = "admin-1"
            }
        };

        _mockActivityRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.CsvContent);
        Assert.Contains("Activity 1", result.CsvContent);
        Assert.Contains(".csv", result.FileName);
        _mockActivityRepository.VerifyAll();
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyList_ReturnsCsvWithHeadersOnly()
    {
        // Arrange
        var service = CreateService();

        _mockActivityRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataProcessingActivity>());

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("ID", result.CsvContent);
        Assert.Contains("Name", result.CsvContent);
        Assert.Contains("Purpose", result.CsvContent);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var activities = new List<DataProcessingActivity>
        {
            new DataProcessingActivity
            {
                Id = Guid.NewGuid(),
                Name = "Activity with \"quotes\"",
                Purpose = "Purpose with, comma",
                LegalBasis = "Contract",
                DataCategories = "Categories",
                DataSubjectCategories = "Subjects",
                Recipients = "Recipients",
                RetentionPeriod = "7 years",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = "admin-1"
            }
        };

        _mockActivityRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        Assert.True(result.Succeeded);
        // Quotes should be doubled for CSV escaping
        Assert.Contains("\"\"quotes\"\"", result.CsvContent);
    }

    #endregion
}
