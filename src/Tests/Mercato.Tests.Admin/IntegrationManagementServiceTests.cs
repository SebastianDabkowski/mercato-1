using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class IntegrationManagementServiceTests
{
    private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
    private readonly Mock<ILogger<IntegrationManagementService>> _mockLogger;

    public IntegrationManagementServiceTests()
    {
        _mockIntegrationRepository = new Mock<IIntegrationRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<IntegrationManagementService>>();
    }

    private IntegrationManagementService CreateService()
    {
        return new IntegrationManagementService(
            _mockIntegrationRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullIntegrationRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IntegrationManagementService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IntegrationManagementService(_mockIntegrationRepository.Object, null!));
    }

    #endregion

    #region GetAllIntegrationsAsync Tests

    [Fact]
    public async Task GetAllIntegrationsAsync_ReturnsAllIntegrations()
    {
        // Arrange
        var service = CreateService();
        var integrations = new List<Integration>
        {
            new Integration
            {
                Id = Guid.NewGuid(),
                Name = "Stripe Payment",
                IntegrationType = IntegrationType.Payment,
                Environment = IntegrationEnvironment.Production,
                Status = IntegrationStatus.Active,
                IsEnabled = true
            },
            new Integration
            {
                Id = Guid.NewGuid(),
                Name = "FedEx Shipping",
                IntegrationType = IntegrationType.Shipping,
                Environment = IntegrationEnvironment.Sandbox,
                Status = IntegrationStatus.Active,
                IsEnabled = true
            }
        };

        _mockIntegrationRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(integrations);

        // Act
        var result = await service.GetAllIntegrationsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Integrations.Count);
        _mockIntegrationRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllIntegrationsAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockIntegrationRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Integration>());

        // Act
        var result = await service.GetAllIntegrationsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Integrations);
    }

    #endregion

    #region GetIntegrationByIdAsync Tests

    [Fact]
    public async Task GetIntegrationByIdAsync_ExistingIntegration_ReturnsIntegration()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            IsEnabled = true
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        // Act
        var result = await service.GetIntegrationByIdAsync(integrationId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.Equal(integrationId, result.Integration.Id);
        Assert.Equal("Stripe Payment", result.Integration.Name);
    }

    [Fact]
    public async Task GetIntegrationByIdAsync_NonExistingIntegration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await service.GetIntegrationByIdAsync(integrationId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task GetIntegrationByIdAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetIntegrationByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Integration ID is required"));
    }

    #endregion

    #region GetIntegrationsByTypeAsync Tests

    [Fact]
    public async Task GetIntegrationsByTypeAsync_ReturnsIntegrationsOfType()
    {
        // Arrange
        var service = CreateService();
        var integrations = new List<Integration>
        {
            new Integration
            {
                Id = Guid.NewGuid(),
                Name = "Stripe Payment",
                IntegrationType = IntegrationType.Payment,
                IsEnabled = true
            },
            new Integration
            {
                Id = Guid.NewGuid(),
                Name = "PayPal",
                IntegrationType = IntegrationType.Payment,
                IsEnabled = true
            }
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByTypeAsync(IntegrationType.Payment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integrations);

        // Act
        var result = await service.GetIntegrationsByTypeAsync(IntegrationType.Payment);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Integrations.Count);
        Assert.All(result.Integrations, i => Assert.Equal(IntegrationType.Payment, i.IntegrationType));
    }

    #endregion

    #region CreateIntegrationAsync Tests

    [Fact]
    public async Task CreateIntegrationAsync_ValidCommand_CreatesIntegration()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateIntegrationCommand
        {
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            ApiEndpoint = "https://api.stripe.com",
            ApiKey = "sk_test_1234567890abcdef",
            MerchantId = "MERCH-123",
            IsEnabled = true,
            CreatedByUserId = "admin-user-1"
        };

        _mockIntegrationRepository
            .Setup(r => r.AddAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration i, CancellationToken _) => i);

        // Act
        var result = await service.CreateIntegrationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.Equal("Stripe Payment", result.Integration.Name);
        Assert.Equal(IntegrationType.Payment, result.Integration.IntegrationType);
        Assert.Equal(IntegrationEnvironment.Production, result.Integration.Environment);
        Assert.Equal("admin-user-1", result.Integration.CreatedByUserId);
        _mockIntegrationRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateIntegrationAsync_MasksApiKey()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateIntegrationCommand
        {
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            ApiKey = "sk_test_1234567890abcdef",
            CreatedByUserId = "admin-user-1"
        };

        Integration? capturedIntegration = null;

        _mockIntegrationRepository
            .Setup(r => r.AddAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Callback<Integration, CancellationToken>((i, _) => capturedIntegration = i)
            .ReturnsAsync((Integration i, CancellationToken _) => i);

        // Act
        var result = await service.CreateIntegrationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedIntegration);
        Assert.NotNull(capturedIntegration.ApiKeyMasked);
        Assert.EndsWith("cdef", capturedIntegration.ApiKeyMasked);
        // Security: Fixed-length mask prevents inferring the original key length
        Assert.StartsWith("********", capturedIntegration.ApiKeyMasked);
        Assert.DoesNotContain("sk_test_1234567890ab", capturedIntegration.ApiKeyMasked);
    }

    [Fact]
    public async Task CreateIntegrationAsync_EmptyName_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateIntegrationCommand
        {
            Name = "",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateIntegrationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Integration name is required"));
    }

    [Fact]
    public async Task CreateIntegrationAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateIntegrationCommand
        {
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateIntegrationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task CreateIntegrationAsync_NameTooLong_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateIntegrationCommand
        {
            Name = new string('a', 201),
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateIntegrationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("must not exceed 200 characters"));
    }

    #endregion

    #region UpdateIntegrationAsync Tests

    [Fact]
    public async Task UpdateIntegrationAsync_ValidCommand_UpdatesIntegration()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var existingIntegration = new Integration
        {
            Id = integrationId,
            Name = "Old Name",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Sandbox,
            ApiKeyMasked = "****5678",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateIntegrationCommand
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            ApiEndpoint = "https://api.stripe.com",
            UpdatedByUserId = "admin-user-2"
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIntegration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateIntegrationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.Equal("Stripe Payment", result.Integration.Name);
        Assert.Equal(IntegrationEnvironment.Production, result.Integration.Environment);
        Assert.Equal("admin-user-2", result.Integration.UpdatedByUserId);
        _mockIntegrationRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateIntegrationAsync_PreservesApiKeyWhenNotProvided()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var existingIntegration = new Integration
        {
            Id = integrationId,
            Name = "Old Name",
            IntegrationType = IntegrationType.Payment,
            ApiKeyMasked = "****5678",
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateIntegrationCommand
        {
            Id = integrationId,
            Name = "New Name",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            ApiKey = null,  // Not providing new API key
            UpdatedByUserId = "admin-user-2"
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIntegration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateIntegrationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.Equal("****5678", result.Integration.ApiKeyMasked);
    }

    [Fact]
    public async Task UpdateIntegrationAsync_UpdatesApiKeyWhenProvided()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var existingIntegration = new Integration
        {
            Id = integrationId,
            Name = "Old Name",
            IntegrationType = IntegrationType.Payment,
            ApiKeyMasked = "****5678",
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateIntegrationCommand
        {
            Id = integrationId,
            Name = "New Name",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            ApiKey = "new_api_key_12345",
            UpdatedByUserId = "admin-user-2"
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIntegration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateIntegrationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.EndsWith("2345", result.Integration.ApiKeyMasked);
    }

    [Fact]
    public async Task UpdateIntegrationAsync_NonExistingIntegration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var command = new UpdateIntegrationCommand
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            UpdatedByUserId = "admin-user-1"
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await service.UpdateIntegrationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateIntegrationAsync_EmptyId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateIntegrationCommand
        {
            Id = Guid.Empty,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            Environment = IntegrationEnvironment.Production,
            UpdatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateIntegrationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Integration ID is required"));
    }

    #endregion

    #region EnableIntegrationAsync Tests

    [Fact]
    public async Task EnableIntegrationAsync_DisabledIntegration_EnablesIntegration()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            IsEnabled = false,
            Status = IntegrationStatus.Inactive
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.EnableIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.True(result.Integration.IsEnabled);
        Assert.Equal(IntegrationStatus.Active, result.Integration.Status);
        _mockIntegrationRepository.VerifyAll();
    }

    [Fact]
    public async Task EnableIntegrationAsync_AlreadyEnabled_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IsEnabled = true
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        // Act
        var result = await service.EnableIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already enabled"));
    }

    [Fact]
    public async Task EnableIntegrationAsync_NonExistingIntegration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await service.EnableIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task EnableIntegrationAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.EnableIntegrationAsync(Guid.Empty, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Integration ID is required"));
    }

    [Fact]
    public async Task EnableIntegrationAsync_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        // Act
        var result = await service.EnableIntegrationAsync(integrationId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region DisableIntegrationAsync Tests

    [Fact]
    public async Task DisableIntegrationAsync_EnabledIntegration_DisablesIntegration()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            IsEnabled = true,
            Status = IntegrationStatus.Active
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DisableIntegrationAsync(integrationId, "admin-user-1", null, "Maintenance");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Integration);
        Assert.False(result.Integration.IsEnabled);
        Assert.Equal(IntegrationStatus.Inactive, result.Integration.Status);
        _mockIntegrationRepository.VerifyAll();
    }

    [Fact]
    public async Task DisableIntegrationAsync_AlreadyDisabled_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IsEnabled = false
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        // Act
        var result = await service.DisableIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already disabled"));
    }

    [Fact]
    public async Task DisableIntegrationAsync_NonExistingIntegration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await service.DisableIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_WithValidEndpoint_ReturnsHealthy()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            ApiEndpoint = "https://api.stripe.com",
            ApiKeyMasked = "****5678",
            IsEnabled = true,
            Status = IntegrationStatus.Active
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.TestConnectionAsync(integrationId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsHealthy);
        Assert.NotNull(result.Message);
        Assert.NotNull(result.Integration);
        Assert.NotNull(result.Integration.LastHealthCheckAt);
        Assert.True(result.Integration.LastHealthCheckStatus);
    }

    [Fact]
    public async Task TestConnectionAsync_WithEmptyEndpoint_ReturnsUnhealthy()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            ApiEndpoint = "",
            ApiKeyMasked = "****5678",
            IsEnabled = true
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.TestConnectionAsync(integrationId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsHealthy);
        Assert.Contains("endpoint", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestConnectionAsync_WithNoApiKey_ReturnsUnhealthy()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            ApiEndpoint = "https://api.stripe.com",
            ApiKeyMasked = null,
            IsEnabled = true
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.TestConnectionAsync(integrationId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsHealthy);
        Assert.Contains("API key", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestConnectionAsync_NonExistingIntegration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await service.TestConnectionAsync(integrationId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task TestConnectionAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.TestConnectionAsync(Guid.Empty, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Integration ID is required"));
    }

    [Fact]
    public async Task TestConnectionAsync_UpdatesIntegrationStatus()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment",
            IntegrationType = IntegrationType.Payment,
            ApiEndpoint = "https://api.stripe.com",
            ApiKeyMasked = "****5678",
            IsEnabled = true,
            Status = IntegrationStatus.Active
        };

        Integration? updatedIntegration = null;

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Integration>(), It.IsAny<CancellationToken>()))
            .Callback<Integration, CancellationToken>((i, _) => updatedIntegration = i)
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.TestConnectionAsync(integrationId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(updatedIntegration);
        Assert.NotNull(updatedIntegration.LastHealthCheckAt);
        Assert.NotNull(updatedIntegration.LastHealthCheckStatus);
        Assert.NotNull(updatedIntegration.LastHealthCheckMessage);
        Assert.Equal("admin-user-1", updatedIntegration.UpdatedByUserId);
    }

    #endregion

    #region DeleteIntegrationAsync Tests

    [Fact]
    public async Task DeleteIntegrationAsync_ExistingIntegration_DeletesIntegration()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = integrationId,
            Name = "Stripe Payment"
        };

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        _mockIntegrationRepository
            .Setup(r => r.DeleteAsync(integrationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        _mockIntegrationRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteIntegrationAsync_NonExistingIntegration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        _mockIntegrationRepository
            .Setup(r => r.GetByIdAsync(integrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await service.DeleteIntegrationAsync(integrationId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeleteIntegrationAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteIntegrationAsync(Guid.Empty, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Integration ID is required"));
    }

    [Fact]
    public async Task DeleteIntegrationAsync_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var integrationId = Guid.NewGuid();

        // Act
        var result = await service.DeleteIntegrationAsync(integrationId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion
}
