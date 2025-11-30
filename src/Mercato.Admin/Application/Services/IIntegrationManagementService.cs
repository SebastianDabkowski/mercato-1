using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing integrations from the admin panel.
/// </summary>
public interface IIntegrationManagementService
{
    /// <summary>
    /// Gets all integrations.
    /// </summary>
    /// <returns>The result containing all integrations.</returns>
    Task<GetIntegrationsResult> GetAllIntegrationsAsync();

    /// <summary>
    /// Gets a specific integration by ID.
    /// </summary>
    /// <param name="id">The integration identifier.</param>
    /// <returns>The result containing the integration if found.</returns>
    Task<GetIntegrationResult> GetIntegrationByIdAsync(Guid id);

    /// <summary>
    /// Gets all integrations of a specific type.
    /// </summary>
    /// <param name="type">The integration type to filter by.</param>
    /// <returns>The result containing integrations of the specified type.</returns>
    Task<GetIntegrationsResult> GetIntegrationsByTypeAsync(IntegrationType type);

    /// <summary>
    /// Creates a new integration.
    /// </summary>
    /// <param name="command">The command containing integration details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateIntegrationResult> CreateIntegrationAsync(CreateIntegrationCommand command);

    /// <summary>
    /// Updates an existing integration.
    /// </summary>
    /// <param name="command">The command containing updated integration details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateIntegrationResult> UpdateIntegrationAsync(UpdateIntegrationCommand command);

    /// <summary>
    /// Enables an integration for use.
    /// </summary>
    /// <param name="id">The integration ID to enable.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="userEmail">The email of the user performing the action.</param>
    /// <returns>The result of the enable operation.</returns>
    Task<EnableIntegrationResult> EnableIntegrationAsync(Guid id, string userId, string? userEmail = null);

    /// <summary>
    /// Disables an integration. When disabled, calls are blocked gracefully.
    /// </summary>
    /// <param name="id">The integration ID to disable.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="userEmail">The email of the user performing the action.</param>
    /// <param name="reason">Optional reason for disabling.</param>
    /// <returns>The result of the disable operation.</returns>
    Task<DisableIntegrationResult> DisableIntegrationAsync(Guid id, string userId, string? userEmail = null, string? reason = null);

    /// <summary>
    /// Tests the connection to an integration by performing a health check.
    /// </summary>
    /// <param name="id">The integration ID to test.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <returns>The result of the health check.</returns>
    Task<TestConnectionResult> TestConnectionAsync(Guid id, string userId);

    /// <summary>
    /// Deletes an integration.
    /// </summary>
    /// <param name="id">The integration ID to delete.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteIntegrationResult> DeleteIntegrationAsync(Guid id, string userId);
}

/// <summary>
/// Result of getting all integrations.
/// </summary>
public class GetIntegrationsResult
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
    /// Gets the integrations.
    /// </summary>
    public IReadOnlyList<Integration> Integrations { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with integrations.
    /// </summary>
    /// <param name="integrations">The integrations.</param>
    /// <returns>A successful result.</returns>
    public static GetIntegrationsResult Success(IReadOnlyList<Integration> integrations) => new()
    {
        Succeeded = true,
        Errors = [],
        Integrations = integrations
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetIntegrationsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetIntegrationsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetIntegrationsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single integration.
/// </summary>
public class GetIntegrationResult
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
    /// Gets the integration.
    /// </summary>
    public Integration? Integration { get; private init; }

    /// <summary>
    /// Creates a successful result with an integration.
    /// </summary>
    /// <param name="integration">The integration.</param>
    /// <returns>A successful result.</returns>
    public static GetIntegrationResult Success(Integration integration) => new()
    {
        Succeeded = true,
        Errors = [],
        Integration = integration
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetIntegrationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetIntegrationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetIntegrationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new integration.
/// </summary>
public class CreateIntegrationCommand
{
    /// <summary>
    /// Gets or sets the integration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public IntegrationType IntegrationType { get; set; }

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public IntegrationEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint.
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the API key (will be masked after storage).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the merchant ID.
    /// </summary>
    public string? MerchantId { get; set; }

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets whether this integration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID creating this integration.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user creating this integration.
    /// </summary>
    public string? CreatedByUserEmail { get; set; }
}

/// <summary>
/// Result of creating an integration.
/// </summary>
public class CreateIntegrationResult
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
    /// Gets the created integration.
    /// </summary>
    public Integration? Integration { get; private init; }

    /// <summary>
    /// Creates a successful result with the created integration.
    /// </summary>
    /// <param name="integration">The created integration.</param>
    /// <returns>A successful result.</returns>
    public static CreateIntegrationResult Success(Integration integration) => new()
    {
        Succeeded = true,
        Errors = [],
        Integration = integration
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateIntegrationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateIntegrationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateIntegrationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing integration.
/// </summary>
public class UpdateIntegrationCommand
{
    /// <summary>
    /// Gets or sets the ID of the integration to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the integration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public IntegrationType IntegrationType { get; set; }

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public IntegrationEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint.
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the new API key (will be masked after storage).
    /// If null or empty, the existing API key is preserved.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the merchant ID.
    /// </summary>
    public string? MerchantId { get; set; }

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets the user ID updating this integration.
    /// </summary>
    public string UpdatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user updating this integration.
    /// </summary>
    public string? UpdatedByUserEmail { get; set; }
}

/// <summary>
/// Result of updating an integration.
/// </summary>
public class UpdateIntegrationResult
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
    /// Gets the updated integration.
    /// </summary>
    public Integration? Integration { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated integration.
    /// </summary>
    /// <param name="integration">The updated integration.</param>
    /// <returns>A successful result.</returns>
    public static UpdateIntegrationResult Success(Integration integration) => new()
    {
        Succeeded = true,
        Errors = [],
        Integration = integration
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateIntegrationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateIntegrationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateIntegrationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of enabling an integration.
/// </summary>
public class EnableIntegrationResult
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
    /// Gets the enabled integration.
    /// </summary>
    public Integration? Integration { get; private init; }

    /// <summary>
    /// Creates a successful result with the enabled integration.
    /// </summary>
    /// <param name="integration">The enabled integration.</param>
    /// <returns>A successful result.</returns>
    public static EnableIntegrationResult Success(Integration integration) => new()
    {
        Succeeded = true,
        Errors = [],
        Integration = integration
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static EnableIntegrationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static EnableIntegrationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static EnableIntegrationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of disabling an integration.
/// </summary>
public class DisableIntegrationResult
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
    /// Gets the disabled integration.
    /// </summary>
    public Integration? Integration { get; private init; }

    /// <summary>
    /// Creates a successful result with the disabled integration.
    /// </summary>
    /// <param name="integration">The disabled integration.</param>
    /// <returns>A successful result.</returns>
    public static DisableIntegrationResult Success(Integration integration) => new()
    {
        Succeeded = true,
        Errors = [],
        Integration = integration
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DisableIntegrationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DisableIntegrationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DisableIntegrationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of testing connection to an integration.
/// </summary>
public class TestConnectionResult
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
    /// Gets a value indicating whether the health check passed.
    /// </summary>
    public bool IsHealthy { get; private init; }

    /// <summary>
    /// Gets the health check message.
    /// </summary>
    public string? Message { get; private init; }

    /// <summary>
    /// Gets the integration that was tested.
    /// </summary>
    public Integration? Integration { get; private init; }

    /// <summary>
    /// Creates a successful healthy result.
    /// </summary>
    /// <param name="integration">The integration that was tested.</param>
    /// <param name="message">The health check message.</param>
    /// <returns>A successful healthy result.</returns>
    public static TestConnectionResult Healthy(Integration integration, string message) => new()
    {
        Succeeded = true,
        Errors = [],
        IsHealthy = true,
        Message = message,
        Integration = integration
    };

    /// <summary>
    /// Creates a successful unhealthy result.
    /// </summary>
    /// <param name="integration">The integration that was tested.</param>
    /// <param name="message">The health check error message.</param>
    /// <returns>A successful unhealthy result.</returns>
    public static TestConnectionResult Unhealthy(Integration integration, string message) => new()
    {
        Succeeded = true,
        Errors = [],
        IsHealthy = false,
        Message = message,
        Integration = integration
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static TestConnectionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static TestConnectionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static TestConnectionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of deleting an integration.
/// </summary>
public class DeleteIntegrationResult
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
    public static DeleteIntegrationResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DeleteIntegrationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DeleteIntegrationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DeleteIntegrationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
