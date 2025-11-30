namespace Mercato.Application.Security.Encryption;

/// <summary>
/// Configuration options for the encryption service.
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Encryption";

    /// <summary>
    /// Gets or sets the encryption provider type.
    /// Supported values: "DataProtection" (default), "AzureKeyVault"
    /// </summary>
    public string Provider { get; set; } = "DataProtection";

    /// <summary>
    /// Gets or sets the Azure Key Vault URI when using AzureKeyVault provider.
    /// </summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// Gets or sets the key name/identifier for encryption operations.
    /// </summary>
    public string? KeyName { get; set; }

    /// <summary>
    /// Gets or sets the application name used for key derivation in DataProtection.
    /// </summary>
    public string ApplicationName { get; set; } = "Mercato";

    /// <summary>
    /// Gets or sets the purpose string for encryption operations.
    /// Different purposes create isolated encryption keys.
    /// </summary>
    public string Purpose { get; set; } = "SensitiveDataProtection";
}
