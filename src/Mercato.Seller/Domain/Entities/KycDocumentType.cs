namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the type of document submitted for KYC verification.
/// </summary>
public enum KycDocumentType
{
    /// <summary>
    /// Personal identification document (e.g., passport, driver's license).
    /// </summary>
    PersonalId = 0,

    /// <summary>
    /// Business license or registration certificate.
    /// </summary>
    BusinessLicense = 1,

    /// <summary>
    /// Tax registration certificate or tax ID document.
    /// </summary>
    TaxCertificate = 2,

    /// <summary>
    /// Proof of address (e.g., utility bill, bank statement).
    /// </summary>
    AddressProof = 3
}
