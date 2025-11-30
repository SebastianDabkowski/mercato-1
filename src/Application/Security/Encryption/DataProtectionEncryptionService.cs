using Microsoft.AspNetCore.DataProtection;

namespace Mercato.Application.Security.Encryption;

/// <summary>
/// Encryption service implementation using ASP.NET Core Data Protection API.
/// Provides key management, rotation, and encryption using industry-standard algorithms.
/// </summary>
public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectionEncryptionService"/> class.
    /// </summary>
    /// <param name="dataProtectionProvider">The data protection provider.</param>
    /// <param name="options">The encryption options.</param>
    public DataProtectionEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        EncryptionOptions options)
    {
        _protector = dataProtectionProvider.CreateProtector(options.Purpose);
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        return _protector.Protect(plainText);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        return _protector.Unprotect(cipherText);
    }

    /// <inheritdoc />
    public byte[] EncryptBytes(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return data!;
        }

        return _protector.Protect(data);
    }

    /// <inheritdoc />
    public byte[] DecryptBytes(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
        {
            return encryptedData!;
        }

        return _protector.Unprotect(encryptedData);
    }
}
