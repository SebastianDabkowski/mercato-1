using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class FeatureFlagManagementServiceTests
{
    private readonly Mock<IFeatureFlagRepository> _mockFeatureFlagRepository;
    private readonly Mock<IFeatureFlagHistoryRepository> _mockHistoryRepository;
    private readonly Mock<ILogger<FeatureFlagManagementService>> _mockLogger;

    public FeatureFlagManagementServiceTests()
    {
        _mockFeatureFlagRepository = new Mock<IFeatureFlagRepository>(MockBehavior.Strict);
        _mockHistoryRepository = new Mock<IFeatureFlagHistoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<FeatureFlagManagementService>>();
    }

    private FeatureFlagManagementService CreateService()
    {
        return new FeatureFlagManagementService(
            _mockFeatureFlagRepository.Object,
            _mockHistoryRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullFeatureFlagRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FeatureFlagManagementService(null!, _mockHistoryRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHistoryRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FeatureFlagManagementService(_mockFeatureFlagRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FeatureFlagManagementService(_mockFeatureFlagRepository.Object, _mockHistoryRepository.Object, null!));
    }

    #endregion

    #region GetAllFlagsAsync Tests

    [Fact]
    public async Task GetAllFlagsAsync_ReturnsAllFlags()
    {
        // Arrange
        var service = CreateService();
        var flags = new List<FeatureFlag>
        {
            new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "flag_1",
                Name = "Flag 1",
                IsEnabled = true,
                Environment = FeatureFlagEnvironment.Development
            },
            new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "flag_2",
                Name = "Flag 2",
                IsEnabled = false,
                Environment = FeatureFlagEnvironment.Production
            }
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await service.GetAllFlagsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Flags.Count);
        _mockFeatureFlagRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllFlagsAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockFeatureFlagRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await service.GetAllFlagsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Flags);
    }

    #endregion

    #region GetFlagsByEnvironmentAsync Tests

    [Fact]
    public async Task GetFlagsByEnvironmentAsync_ReturnsFlagsForEnvironment()
    {
        // Arrange
        var service = CreateService();
        var flags = new List<FeatureFlag>
        {
            new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "prod_flag",
                Name = "Production Flag",
                IsEnabled = true,
                Environment = FeatureFlagEnvironment.Production
            }
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByEnvironmentAsync(FeatureFlagEnvironment.Production, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await service.GetFlagsByEnvironmentAsync(FeatureFlagEnvironment.Production);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Flags);
        Assert.Equal(FeatureFlagEnvironment.Production, result.Flags[0].Environment);
    }

    #endregion

    #region GetFlagByIdAsync Tests

    [Fact]
    public async Task GetFlagByIdAsync_ExistingFlag_ReturnsFlag()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var flag = new FeatureFlag
        {
            Id = flagId,
            Key = "test_flag",
            Name = "Test Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.GetFlagByIdAsync(flagId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Flag);
        Assert.Equal(flagId, result.Flag.Id);
        Assert.Equal("test_flag", result.Flag.Key);
    }

    [Fact]
    public async Task GetFlagByIdAsync_NonExistingFlag_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await service.GetFlagByIdAsync(flagId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task GetFlagByIdAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetFlagByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Feature flag ID is required"));
    }

    #endregion

    #region CreateFlagAsync Tests

    [Fact]
    public async Task CreateFlagAsync_ValidCommand_CreatesFlag()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "new_feature",
            Name = "New Feature",
            Description = "A new feature",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.AllUsers,
            CreatedByUserId = "admin-user-1"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("new_feature", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        _mockFeatureFlagRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag flag, CancellationToken _) => flag);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Flag);
        Assert.Equal("new_feature", result.Flag.Key);
        Assert.Equal("New Feature", result.Flag.Name);
        Assert.Equal("admin-user-1", result.Flag.CreatedByUserId);
        _mockFeatureFlagRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateFlagAsync_DuplicateKey_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "existing_flag",
            Name = "Existing Flag",
            Environment = FeatureFlagEnvironment.Development,
            CreatedByUserId = "admin-user-1"
        };

        var existingFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "existing_flag",
            Name = "Already Exists",
            Environment = FeatureFlagEnvironment.Development
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("existing_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task CreateFlagAsync_EmptyKey_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "",
            Name = "Test Flag",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Flag key is required"));
    }

    [Fact]
    public async Task CreateFlagAsync_EmptyName_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "test_flag",
            Name = "",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Flag name is required"));
    }

    [Fact]
    public async Task CreateFlagAsync_InvalidKey_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "Invalid Key With Spaces",
            Name = "Test Flag",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("lowercase letters, numbers, underscores, and hyphens"));
    }

    [Fact]
    public async Task CreateFlagAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "test_flag",
            Name = "Test Flag",
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task CreateFlagAsync_SpecificSellersWithoutTargetValue_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "test_flag",
            Name = "Test Flag",
            TargetType = FeatureFlagTargetType.SpecificSellers,
            TargetValue = null,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Target value is required for SpecificSellers"));
    }

    [Fact]
    public async Task CreateFlagAsync_PercentageRolloutWithInvalidValue_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "test_flag",
            Name = "Test Flag",
            TargetType = FeatureFlagTargetType.PercentageRollout,
            TargetValue = "150",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("number between 0 and 100"));
    }

    [Fact]
    public async Task CreateFlagAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateFeatureFlagCommand
        {
            Key = "test_flag",
            Name = "Test Flag",
            Environment = FeatureFlagEnvironment.Development,
            CreatedByUserId = "admin-user-1",
            CreatedByUserEmail = "admin@test.com"
        };

        FeatureFlagHistory? capturedHistory = null;

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("test_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        _mockFeatureFlagRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag flag, CancellationToken _) => flag);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .Callback<FeatureFlagHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        // Act
        var result = await service.CreateFlagAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Created", capturedHistory.ChangeType);
        Assert.Null(capturedHistory.PreviousValues);
        Assert.NotEmpty(capturedHistory.NewValues);
        Assert.Equal("admin-user-1", capturedHistory.ChangedByUserId);
        Assert.Equal("admin@test.com", capturedHistory.ChangedByUserEmail);
    }

    #endregion

    #region UpdateFlagAsync Tests

    [Fact]
    public async Task UpdateFlagAsync_ValidCommand_UpdatesFlag()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "old_key",
            Name = "Old Name",
            IsEnabled = false,
            Environment = FeatureFlagEnvironment.Development,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateFeatureFlagCommand
        {
            Id = flagId,
            Key = "new_key",
            Name = "New Name",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.AllUsers,
            UpdatedByUserId = "admin-user-2"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("new_key", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        _mockFeatureFlagRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        // Act
        var result = await service.UpdateFlagAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Flag);
        Assert.Equal("new_key", result.Flag.Key);
        Assert.Equal("New Name", result.Flag.Name);
        Assert.True(result.Flag.IsEnabled);
        Assert.Equal("admin-user-2", result.Flag.UpdatedByUserId);
        _mockFeatureFlagRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateFlagAsync_NonExistingFlag_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var command = new UpdateFeatureFlagCommand
        {
            Id = flagId,
            Key = "test_flag",
            Name = "Test Flag",
            Environment = FeatureFlagEnvironment.Development,
            UpdatedByUserId = "admin-user-1"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await service.UpdateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateFlagAsync_EmptyId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateFeatureFlagCommand
        {
            Id = Guid.Empty,
            Key = "test_flag",
            Name = "Test Flag",
            UpdatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Feature flag ID is required"));
    }

    [Fact]
    public async Task UpdateFlagAsync_DuplicateKeyDifferentId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "original_key",
            Name = "Original",
            Environment = FeatureFlagEnvironment.Development
        };

        var anotherFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "duplicate_key",
            Name = "Another",
            Environment = FeatureFlagEnvironment.Development
        };

        var command = new UpdateFeatureFlagCommand
        {
            Id = flagId,
            Key = "duplicate_key",
            Name = "Updated",
            Environment = FeatureFlagEnvironment.Development,
            UpdatedByUserId = "admin-user-1"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("duplicate_key", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anotherFlag);

        // Act
        var result = await service.UpdateFlagAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task UpdateFlagAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "test_flag",
            Name = "Old Name",
            Environment = FeatureFlagEnvironment.Development,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateFeatureFlagCommand
        {
            Id = flagId,
            Key = "test_flag",
            Name = "New Name",
            Environment = FeatureFlagEnvironment.Development,
            UpdatedByUserId = "admin-user-2",
            UpdatedByUserEmail = "admin2@test.com"
        };

        FeatureFlagHistory? capturedHistory = null;

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("test_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockFeatureFlagRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .Callback<FeatureFlagHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        // Act
        var result = await service.UpdateFlagAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Updated", capturedHistory.ChangeType);
        Assert.NotNull(capturedHistory.PreviousValues);
        Assert.NotEmpty(capturedHistory.NewValues);
        Assert.Equal("admin-user-2", capturedHistory.ChangedByUserId);
        Assert.Equal("admin2@test.com", capturedHistory.ChangedByUserEmail);
    }

    #endregion

    #region DeleteFlagAsync Tests

    [Fact]
    public async Task DeleteFlagAsync_ExistingFlag_DeletesFlag()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "flag_to_delete",
            Name = "To Delete",
            Environment = FeatureFlagEnvironment.Development
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        _mockFeatureFlagRepository
            .Setup(r => r.DeleteAsync(flagId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteFlagAsync(flagId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        _mockFeatureFlagRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteFlagAsync_NonExistingFlag_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await service.DeleteFlagAsync(flagId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeleteFlagAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteFlagAsync(Guid.Empty, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Feature flag ID is required"));
    }

    [Fact]
    public async Task DeleteFlagAsync_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();

        // Act
        var result = await service.DeleteFlagAsync(flagId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task DeleteFlagAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "flag_to_delete",
            Name = "To Delete",
            Environment = FeatureFlagEnvironment.Development
        };

        FeatureFlagHistory? capturedHistory = null;

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .Callback<FeatureFlagHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        _mockFeatureFlagRepository
            .Setup(r => r.DeleteAsync(flagId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteFlagAsync(flagId, "admin-user-1", "admin@test.com");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Deleted", capturedHistory.ChangeType);
        Assert.NotNull(capturedHistory.PreviousValues);
        Assert.Equal("{}", capturedHistory.NewValues);
        Assert.Equal("admin-user-1", capturedHistory.ChangedByUserId);
        Assert.Equal("admin@test.com", capturedHistory.ChangedByUserEmail);
    }

    #endregion

    #region ToggleFlagAsync Tests

    [Fact]
    public async Task ToggleFlagAsync_EnableFlag_UpdatesAndCreatesHistory()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "test_flag",
            Name = "Test Flag",
            IsEnabled = false,
            Environment = FeatureFlagEnvironment.Development
        };

        FeatureFlagHistory? capturedHistory = null;

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockFeatureFlagRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .Callback<FeatureFlagHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        // Act
        var result = await service.ToggleFlagAsync(flagId, true, "admin-user-1", "admin@test.com");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Flag);
        Assert.True(result.Flag.IsEnabled);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Toggled", capturedHistory.ChangeType);
    }

    [Fact]
    public async Task ToggleFlagAsync_DisableFlag_UpdatesAndCreatesHistory()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "test_flag",
            Name = "Test Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        _mockFeatureFlagRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<FeatureFlagHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlagHistory history, CancellationToken _) => history);

        // Act
        var result = await service.ToggleFlagAsync(flagId, false, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Flag);
        Assert.False(result.Flag.IsEnabled);
    }

    [Fact]
    public async Task ToggleFlagAsync_NonExistingFlag_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await service.ToggleFlagAsync(flagId, true, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task ToggleFlagAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ToggleFlagAsync(Guid.Empty, true, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Feature flag ID is required"));
    }

    [Fact]
    public async Task ToggleFlagAsync_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();

        // Act
        var result = await service.ToggleFlagAsync(flagId, true, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region EvaluateFlagAsync Tests

    [Fact]
    public async Task EvaluateFlagAsync_FlagNotFound_ReturnsFlagNotFound()
    {
        // Arrange
        var service = CreateService();

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("nonexistent_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await service.EvaluateFlagAsync("nonexistent_flag", FeatureFlagEnvironment.Development);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.FlagFound);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_DisabledFlag_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "disabled_flag",
            Name = "Disabled Flag",
            IsEnabled = false,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.AllUsers
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("disabled_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("disabled_flag", FeatureFlagEnvironment.Development);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_AllUsersTarget_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "all_users_flag",
            Name = "All Users Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.AllUsers
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("all_users_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("all_users_flag", FeatureFlagEnvironment.Development, "user-123");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_InternalUsersTarget_WithInternalUser_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "internal_flag",
            Name = "Internal Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.InternalUsers
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("internal_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("internal_flag", FeatureFlagEnvironment.Development, "internal-user-123");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_InternalUsersTarget_WithExternalUser_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "internal_flag",
            Name = "Internal Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.InternalUsers
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("internal_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("internal_flag", FeatureFlagEnvironment.Development, "external-user-123");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_SpecificSellersTarget_WithMatchingSeller_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "sellers_flag",
            Name = "Sellers Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.SpecificSellers,
            TargetValue = "[\"seller-1\", \"seller-2\", \"seller-3\"]"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("sellers_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("sellers_flag", FeatureFlagEnvironment.Development, null, "seller-2");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_SpecificSellersTarget_WithNonMatchingSeller_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "sellers_flag",
            Name = "Sellers Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.SpecificSellers,
            TargetValue = "[\"seller-1\", \"seller-2\", \"seller-3\"]"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("sellers_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("sellers_flag", FeatureFlagEnvironment.Development, null, "seller-99");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_PercentageRollout_ConsistentForSameUser()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "rollout_flag",
            Name = "Rollout Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.PercentageRollout,
            TargetValue = "50"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("rollout_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act - call multiple times for the same user
        var result1 = await service.EvaluateFlagAsync("rollout_flag", FeatureFlagEnvironment.Development, "consistent-user-123");
        var result2 = await service.EvaluateFlagAsync("rollout_flag", FeatureFlagEnvironment.Development, "consistent-user-123");
        var result3 = await service.EvaluateFlagAsync("rollout_flag", FeatureFlagEnvironment.Development, "consistent-user-123");

        // Assert - should get the same result every time
        Assert.True(result1.Succeeded);
        Assert.True(result1.FlagFound);
        Assert.Equal(result1.IsEnabled, result2.IsEnabled);
        Assert.Equal(result2.IsEnabled, result3.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_PercentageRollout_100Percent_AlwaysTrue()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "full_rollout_flag",
            Name = "Full Rollout Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.PercentageRollout,
            TargetValue = "100"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("full_rollout_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("full_rollout_flag", FeatureFlagEnvironment.Development, "any-user");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_PercentageRollout_0Percent_AlwaysFalse()
    {
        // Arrange
        var service = CreateService();
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "no_rollout_flag",
            Name = "No Rollout Flag",
            IsEnabled = true,
            Environment = FeatureFlagEnvironment.Development,
            TargetType = FeatureFlagTargetType.PercentageRollout,
            TargetValue = "0"
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByKeyAndEnvironmentAsync("no_rollout_flag", FeatureFlagEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await service.EvaluateFlagAsync("no_rollout_flag", FeatureFlagEnvironment.Development, "any-user");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.FlagFound);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateFlagAsync_EmptyKey_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.EvaluateFlagAsync("", FeatureFlagEnvironment.Development);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Feature flag key is required"));
    }

    #endregion

    #region GetFlagHistoryAsync Tests

    [Fact]
    public async Task GetFlagHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var flag = new FeatureFlag
        {
            Id = flagId,
            Key = "test_flag",
            Name = "Test Flag",
            Environment = FeatureFlagEnvironment.Development
        };
        var history = new List<FeatureFlagHistory>
        {
            new FeatureFlagHistory
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = flagId,
                ChangeType = "Created",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ChangedByUserId = "admin-1"
            },
            new FeatureFlagHistory
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = flagId,
                ChangeType = "Updated",
                PreviousValues = "{}",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-15),
                ChangedByUserId = "admin-2"
            }
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        _mockHistoryRepository
            .Setup(r => r.GetByFeatureFlagIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetFlagHistoryAsync(flagId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.History.Count);
        Assert.NotNull(result.Flag);
        Assert.Equal(flagId, result.Flag.Id);
    }

    [Fact]
    public async Task GetFlagHistoryAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetFlagHistoryAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Feature flag ID is required"));
    }

    [Fact]
    public async Task GetFlagHistoryAsync_DeletedFlag_ReturnsHistoryWithoutFlag()
    {
        // Arrange
        var service = CreateService();
        var flagId = Guid.NewGuid();
        var history = new List<FeatureFlagHistory>
        {
            new FeatureFlagHistory
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = flagId,
                ChangeType = "Created",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ChangedByUserId = "admin-1"
            },
            new FeatureFlagHistory
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = flagId,
                ChangeType = "Deleted",
                PreviousValues = "{}",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-1),
                ChangedByUserId = "admin-2"
            }
        };

        _mockFeatureFlagRepository
            .Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        _mockHistoryRepository
            .Setup(r => r.GetByFeatureFlagIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetFlagHistoryAsync(flagId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.History.Count);
        Assert.Null(result.Flag);
    }

    #endregion
}
