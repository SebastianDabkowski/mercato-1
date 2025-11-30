namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents an external integration configuration for the marketplace platform.
/// Integrations can include payment providers, shipping systems, ERP, and other external services.
/// </summary>
public class Integration
{
    /// <summary>
    /// Gets or sets the unique identifier for the integration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the integration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of integration (Payment, Shipping, ERP, Other).
    /// </summary>
    public IntegrationType IntegrationType { get; set; }

    /// <summary>
    /// Gets or sets the environment for this integration (Sandbox or Production).
    /// </summary>
    public IntegrationEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the current status of the integration.
    /// </summary>
    public IntegrationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint URL for the integration.
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the masked API key (only last 4 characters are visible).
    /// Full API keys are stored securely and never exposed.
    /// </summary>
    public string? ApiKeyMasked { get; set; }

    /// <summary>
    /// Gets or sets the merchant identifier for the integration.
    /// </summary>
    public string? MerchantId { get; set; }

    /// <summary>
    /// Gets or sets the callback URL for the integration.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last health check.
    /// </summary>
    public DateTimeOffset? LastHealthCheckAt { get; set; }

    /// <summary>
    /// Gets or sets the status of the last health check (true = success, false = failure).
    /// </summary>
    public bool? LastHealthCheckStatus { get; set; }

    /// <summary>
    /// Gets or sets the message from the last health check.
    /// </summary>
    public string? LastHealthCheckMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this integration is enabled.
    /// When disabled, all calls to this integration are blocked gracefully.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this integration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this integration.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this integration was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this integration.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
