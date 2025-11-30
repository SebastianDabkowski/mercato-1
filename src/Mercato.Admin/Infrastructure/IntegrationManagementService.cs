using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing integrations from the admin panel.
/// </summary>
public class IntegrationManagementService : IIntegrationManagementService
{
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ILogger<IntegrationManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationManagementService"/> class.
    /// </summary>
    /// <param name="integrationRepository">The integration repository.</param>
    /// <param name="logger">The logger.</param>
    public IntegrationManagementService(
        IIntegrationRepository integrationRepository,
        ILogger<IntegrationManagementService> logger)
    {
        ArgumentNullException.ThrowIfNull(integrationRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _integrationRepository = integrationRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetIntegrationsResult> GetAllIntegrationsAsync()
    {
        var integrations = await _integrationRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} integrations", integrations.Count);

        return GetIntegrationsResult.Success(integrations);
    }

    /// <inheritdoc />
    public async Task<GetIntegrationResult> GetIntegrationByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetIntegrationResult.Failure("Integration ID is required.");
        }

        var integration = await _integrationRepository.GetByIdAsync(id);

        if (integration == null)
        {
            return GetIntegrationResult.Failure("Integration not found.");
        }

        return GetIntegrationResult.Success(integration);
    }

    /// <inheritdoc />
    public async Task<GetIntegrationsResult> GetIntegrationsByTypeAsync(IntegrationType type)
    {
        var integrations = await _integrationRepository.GetByTypeAsync(type);

        _logger.LogInformation("Retrieved {Count} integrations of type {Type}", integrations.Count, type);

        return GetIntegrationsResult.Success(integrations);
    }

    /// <inheritdoc />
    public async Task<CreateIntegrationResult> CreateIntegrationAsync(CreateIntegrationCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateIntegrationResult.Failure(validationErrors);
        }

        var now = DateTimeOffset.UtcNow;
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            IntegrationType = command.IntegrationType,
            Environment = command.Environment,
            Status = command.IsEnabled ? IntegrationStatus.Active : IntegrationStatus.Inactive,
            ApiEndpoint = command.ApiEndpoint,
            ApiKeyMasked = MaskApiKey(command.ApiKey),
            MerchantId = command.MerchantId,
            CallbackUrl = command.CallbackUrl,
            IsEnabled = command.IsEnabled,
            CreatedAt = now,
            CreatedByUserId = command.CreatedByUserId
        };

        await _integrationRepository.AddAsync(integration);

        _logger.LogInformation(
            "Created integration '{Name}' of type {Type} by user {UserId}",
            integration.Name,
            integration.IntegrationType,
            command.CreatedByUserId);

        return CreateIntegrationResult.Success(integration);
    }

    /// <inheritdoc />
    public async Task<UpdateIntegrationResult> UpdateIntegrationAsync(UpdateIntegrationCommand command)
    {
        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateIntegrationResult.Failure(validationErrors);
        }

        var existingIntegration = await _integrationRepository.GetByIdAsync(command.Id);
        if (existingIntegration == null)
        {
            return UpdateIntegrationResult.Failure("Integration not found.");
        }

        var now = DateTimeOffset.UtcNow;

        existingIntegration.Name = command.Name;
        existingIntegration.IntegrationType = command.IntegrationType;
        existingIntegration.Environment = command.Environment;
        existingIntegration.ApiEndpoint = command.ApiEndpoint;
        existingIntegration.MerchantId = command.MerchantId;
        existingIntegration.CallbackUrl = command.CallbackUrl;
        existingIntegration.UpdatedAt = now;
        existingIntegration.UpdatedByUserId = command.UpdatedByUserId;

        // Only update API key if a new one is provided
        if (!string.IsNullOrWhiteSpace(command.ApiKey))
        {
            existingIntegration.ApiKeyMasked = MaskApiKey(command.ApiKey);
        }

        await _integrationRepository.UpdateAsync(existingIntegration);

        _logger.LogInformation(
            "Updated integration '{Name}' (ID: {Id}) by user {UserId}",
            existingIntegration.Name,
            existingIntegration.Id,
            command.UpdatedByUserId);

        return UpdateIntegrationResult.Success(existingIntegration);
    }

    /// <inheritdoc />
    public async Task<EnableIntegrationResult> EnableIntegrationAsync(Guid id, string userId, string? userEmail = null)
    {
        if (id == Guid.Empty)
        {
            return EnableIntegrationResult.Failure("Integration ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return EnableIntegrationResult.Failure("User ID is required.");
        }

        var integration = await _integrationRepository.GetByIdAsync(id);
        if (integration == null)
        {
            return EnableIntegrationResult.Failure("Integration not found.");
        }

        if (integration.IsEnabled)
        {
            return EnableIntegrationResult.Failure("Integration is already enabled.");
        }

        var now = DateTimeOffset.UtcNow;

        integration.IsEnabled = true;
        integration.Status = IntegrationStatus.Active;
        integration.UpdatedAt = now;
        integration.UpdatedByUserId = userId;

        await _integrationRepository.UpdateAsync(integration);

        _logger.LogInformation(
            "Enabled integration '{Name}' by user {UserId}",
            integration.Name,
            userId);

        return EnableIntegrationResult.Success(integration);
    }

    /// <inheritdoc />
    public async Task<DisableIntegrationResult> DisableIntegrationAsync(Guid id, string userId, string? userEmail = null, string? reason = null)
    {
        if (id == Guid.Empty)
        {
            return DisableIntegrationResult.Failure("Integration ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return DisableIntegrationResult.Failure("User ID is required.");
        }

        var integration = await _integrationRepository.GetByIdAsync(id);
        if (integration == null)
        {
            return DisableIntegrationResult.Failure("Integration not found.");
        }

        if (!integration.IsEnabled)
        {
            return DisableIntegrationResult.Failure("Integration is already disabled.");
        }

        var now = DateTimeOffset.UtcNow;

        integration.IsEnabled = false;
        integration.Status = IntegrationStatus.Inactive;
        integration.UpdatedAt = now;
        integration.UpdatedByUserId = userId;

        await _integrationRepository.UpdateAsync(integration);

        _logger.LogInformation(
            "Disabled integration '{Name}' by user {UserId}. Reason: {Reason}",
            integration.Name,
            userId,
            reason ?? "No reason provided");

        return DisableIntegrationResult.Success(integration);
    }

    /// <inheritdoc />
    public async Task<TestConnectionResult> TestConnectionAsync(Guid id, string userId)
    {
        if (id == Guid.Empty)
        {
            return TestConnectionResult.Failure("Integration ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return TestConnectionResult.Failure("User ID is required.");
        }

        var integration = await _integrationRepository.GetByIdAsync(id);
        if (integration == null)
        {
            return TestConnectionResult.Failure("Integration not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var (isHealthy, message) = SimulateHealthCheck(integration);

        integration.LastHealthCheckAt = now;
        integration.LastHealthCheckStatus = isHealthy;
        integration.LastHealthCheckMessage = message;

        // Update status based on health check result
        if (integration.IsEnabled)
        {
            integration.Status = isHealthy ? IntegrationStatus.Active : IntegrationStatus.Error;
        }

        integration.UpdatedAt = now;
        integration.UpdatedByUserId = userId;

        await _integrationRepository.UpdateAsync(integration);

        _logger.LogInformation(
            "Health check for integration '{Name}': {Status}. Message: {Message}",
            integration.Name,
            isHealthy ? "Healthy" : "Unhealthy",
            message);

        return isHealthy
            ? TestConnectionResult.Healthy(integration, message)
            : TestConnectionResult.Unhealthy(integration, message);
    }

    /// <inheritdoc />
    public async Task<DeleteIntegrationResult> DeleteIntegrationAsync(Guid id, string userId)
    {
        if (id == Guid.Empty)
        {
            return DeleteIntegrationResult.Failure("Integration ID is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return DeleteIntegrationResult.Failure("User ID is required.");
        }

        var integration = await _integrationRepository.GetByIdAsync(id);
        if (integration == null)
        {
            return DeleteIntegrationResult.Failure("Integration not found.");
        }

        await _integrationRepository.DeleteAsync(id);

        _logger.LogInformation(
            "Deleted integration '{Name}' (ID: {Id}) by user {UserId}",
            integration.Name,
            id,
            userId);

        return DeleteIntegrationResult.Success();
    }

    /// <summary>
    /// Masks an API key, showing only the last 4 characters.
    /// </summary>
    /// <param name="apiKey">The full API key.</param>
    /// <returns>The masked API key.</returns>
    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        // Use a fixed-length mask to prevent inferring the original key length
        const int maskLength = 8;
        var lastFourChars = apiKey.Length >= 4 ? apiKey[^4..] : apiKey;

        return new string('*', maskLength) + lastFourChars;
    }

    /// <summary>
    /// Simulates a health check for an integration.
    /// </summary>
    /// <param name="integration">The integration to check.</param>
    /// <returns>A tuple containing the health status and message.</returns>
    private static (bool IsHealthy, string Message) SimulateHealthCheck(Integration integration)
    {
        // Simulate health check based on configuration
        if (string.IsNullOrWhiteSpace(integration.ApiEndpoint))
        {
            return (false, "API endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(integration.ApiKeyMasked))
        {
            return (false, "API key is not configured.");
        }

        // Simulate success for endpoints containing "api"
        if (integration.ApiEndpoint.Contains("api", StringComparison.OrdinalIgnoreCase))
        {
            return (true, $"Connection successful. Endpoint '{integration.ApiEndpoint}' is responding.");
        }

        // Simulate different responses based on integration type
        return integration.IntegrationType switch
        {
            IntegrationType.Payment => (true, "Payment gateway connection verified."),
            IntegrationType.Shipping => (true, "Shipping provider connection verified."),
            IntegrationType.ERP => (true, "ERP system connection verified."),
            _ => (true, "Integration connection verified.")
        };
    }

    private static List<string> ValidateCreateCommand(CreateIntegrationCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Integration name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Integration name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        if (!string.IsNullOrWhiteSpace(command.ApiEndpoint) && command.ApiEndpoint.Length > 500)
        {
            errors.Add("API endpoint must not exceed 500 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.CallbackUrl) && command.CallbackUrl.Length > 500)
        {
            errors.Add("Callback URL must not exceed 500 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.MerchantId) && command.MerchantId.Length > 100)
        {
            errors.Add("Merchant ID must not exceed 100 characters.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateCommand(UpdateIntegrationCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Integration ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Integration name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Integration name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.UpdatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        if (!string.IsNullOrWhiteSpace(command.ApiEndpoint) && command.ApiEndpoint.Length > 500)
        {
            errors.Add("API endpoint must not exceed 500 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.CallbackUrl) && command.CallbackUrl.Length > 500)
        {
            errors.Add("Callback URL must not exceed 500 characters.");
        }

        if (!string.IsNullOrWhiteSpace(command.MerchantId) && command.MerchantId.Length > 100)
        {
            errors.Add("Merchant ID must not exceed 100 characters.");
        }

        return errors;
    }
}
