namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the type of external integration.
/// </summary>
public enum IntegrationType
{
    /// <summary>
    /// Payment provider integration (e.g., Stripe, PayPal).
    /// </summary>
    Payment = 0,

    /// <summary>
    /// Shipping provider integration (e.g., FedEx, UPS).
    /// </summary>
    Shipping = 1,

    /// <summary>
    /// Enterprise Resource Planning system integration.
    /// </summary>
    ERP = 2,

    /// <summary>
    /// Other types of integrations.
    /// </summary>
    Other = 3
}
