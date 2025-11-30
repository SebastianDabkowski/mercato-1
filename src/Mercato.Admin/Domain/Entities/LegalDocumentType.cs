namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the type of legal document managed in the platform.
/// </summary>
public enum LegalDocumentType
{
    /// <summary>
    /// Terms of Service document.
    /// </summary>
    TermsOfService = 0,

    /// <summary>
    /// Privacy Policy document.
    /// </summary>
    PrivacyPolicy = 1,

    /// <summary>
    /// Cookie Policy document.
    /// </summary>
    CookiePolicy = 2,

    /// <summary>
    /// Seller Agreement document.
    /// </summary>
    SellerAgreement = 3
}
