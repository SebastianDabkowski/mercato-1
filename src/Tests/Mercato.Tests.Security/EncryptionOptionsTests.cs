using Mercato.Application.Security.Encryption;

namespace Mercato.Tests.Security;

/// <summary>
/// Unit tests for the EncryptionOptions class.
/// </summary>
public class EncryptionOptionsTests
{
    [Fact]
    public void SectionName_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("Encryption", EncryptionOptions.SectionName);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new EncryptionOptions();

        // Assert
        Assert.Equal("DataProtection", options.Provider);
        Assert.Equal("Mercato", options.ApplicationName);
        Assert.Equal("SensitiveDataProtection", options.Purpose);
        Assert.Null(options.KeyVaultUri);
        Assert.Null(options.KeyName);
    }

    [Fact]
    public void Provider_CanBeSet()
    {
        // Arrange
        var options = new EncryptionOptions();

        // Act
        options.Provider = "AzureKeyVault";

        // Assert
        Assert.Equal("AzureKeyVault", options.Provider);
    }

    [Fact]
    public void KeyVaultUri_CanBeSet()
    {
        // Arrange
        var options = new EncryptionOptions();
        var expectedUri = "https://my-vault.vault.azure.net/";

        // Act
        options.KeyVaultUri = expectedUri;

        // Assert
        Assert.Equal(expectedUri, options.KeyVaultUri);
    }
}
