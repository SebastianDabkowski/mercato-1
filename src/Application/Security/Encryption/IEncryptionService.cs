namespace Mercato.Application.Security.Encryption;

/// <summary>
/// Service interface for encrypting and decrypting sensitive data.
/// Implementations can be backed by cloud KMS (Azure Key Vault) or local Data Protection API.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted data as a Base64 string.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts the specified cipher text.
    /// </summary>
    /// <param name="cipherText">The cipher text (Base64) to decrypt.</param>
    /// <returns>The decrypted plain text.</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Encrypts the specified byte array.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <returns>The encrypted data.</returns>
    byte[] EncryptBytes(byte[] data);

    /// <summary>
    /// Decrypts the specified encrypted byte array.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt.</param>
    /// <returns>The decrypted data.</returns>
    byte[] DecryptBytes(byte[] encryptedData);
}
