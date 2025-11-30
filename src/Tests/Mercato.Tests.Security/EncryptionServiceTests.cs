using Mercato.Application.Security.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Moq;

namespace Mercato.Tests.Security;

/// <summary>
/// Unit tests for the DataProtectionEncryptionService.
/// </summary>
public class EncryptionServiceTests
{
    private readonly Mock<IDataProtector> _mockProtector;
    private readonly Mock<IDataProtectionProvider> _mockProvider;
    private readonly EncryptionOptions _options;
    private readonly DataProtectionEncryptionService _service;

    public EncryptionServiceTests()
    {
        _mockProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        _mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        _options = new EncryptionOptions
        {
            Purpose = "TestPurpose"
        };

        _mockProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(_mockProtector.Object);

        _service = new DataProtectionEncryptionService(_mockProvider.Object, _options);
    }

    [Fact]
    public void Encrypt_WithValidText_CallsProtect()
    {
        // Arrange
        var plainText = "SensitiveData123";

        _mockProtector
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns<byte[]>(b => b);

        // Act
        var result = _service.Encrypt(plainText);

        // Assert
        Assert.NotNull(result);
        _mockProtector.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void Encrypt_WithNullText_ReturnsNull()
    {
        // Arrange - intentionally testing null input behavior
        string plainText = null!;

        // Act
        var result = _service.Encrypt(plainText);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Encrypt_WithEmptyText_ReturnsEmpty()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var result = _service.Encrypt(plainText);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decrypt_WithNullText_ReturnsNull()
    {
        // Arrange - intentionally testing null input behavior
        string cipherText = null!;

        // Act
        var result = _service.Decrypt(cipherText);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Decrypt_WithEmptyText_ReturnsEmpty()
    {
        // Arrange
        var cipherText = string.Empty;

        // Act
        var result = _service.Decrypt(cipherText);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EncryptBytes_WithValidData_ReturnsEncryptedBytes()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var expectedEncrypted = new byte[] { 10, 20, 30, 40, 50 };

        _mockProtector
            .Setup(p => p.Protect(data))
            .Returns(expectedEncrypted);

        // Act
        var result = _service.EncryptBytes(data);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEncrypted, result);
        _mockProtector.VerifyAll();
    }

    [Fact]
    public void EncryptBytes_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange - intentionally testing null input behavior
        byte[] data = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.EncryptBytes(data));
    }

    [Fact]
    public void EncryptBytes_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var data = Array.Empty<byte>();

        // Act
        var result = _service.EncryptBytes(data);

        // Assert
        Assert.Equal(Array.Empty<byte>(), result);
    }

    [Fact]
    public void DecryptBytes_WithValidData_ReturnsDecryptedBytes()
    {
        // Arrange
        var encryptedData = new byte[] { 10, 20, 30, 40, 50 };
        var expectedDecrypted = new byte[] { 1, 2, 3, 4, 5 };

        _mockProtector
            .Setup(p => p.Unprotect(encryptedData))
            .Returns(expectedDecrypted);

        // Act
        var result = _service.DecryptBytes(encryptedData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDecrypted, result);
        _mockProtector.VerifyAll();
    }

    [Fact]
    public void DecryptBytes_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange - intentionally testing null input behavior
        byte[] encryptedData = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.DecryptBytes(encryptedData));
    }

    [Fact]
    public void DecryptBytes_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var encryptedData = Array.Empty<byte>();

        // Act
        var result = _service.DecryptBytes(encryptedData);

        // Assert
        Assert.Equal(Array.Empty<byte>(), result);
    }

    [Fact]
    public void Constructor_CreatesProtectorWithCorrectPurpose()
    {
        // Arrange
        var expectedPurpose = "TestPurpose";

        // Assert (constructor already called in test setup)
        _mockProvider.Verify(p => p.CreateProtector(expectedPurpose), Times.Once);
    }
}

