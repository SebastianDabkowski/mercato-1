namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for auditing admin access to sensitive data.
/// Logs when admins or support users access sensitive views such as customer profiles,
/// payout details, KYC documents, and store/seller details.
/// </summary>
public interface ISensitiveAccessAuditService
{
    /// <summary>
    /// Logs access to a customer profile view.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin user accessing the data.</param>
    /// <param name="customerId">The ID of the customer whose profile is being accessed.</param>
    /// <param name="ipAddress">The IP address from which the access occurred (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogCustomerProfileAccessAsync(
        string adminUserId,
        string customerId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs access to payout or financial detail views.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin user accessing the data.</param>
    /// <param name="sellerId">The ID of the seller whose payout details are being accessed.</param>
    /// <param name="ipAddress">The IP address from which the access occurred (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogPayoutDetailsAccessAsync(
        string adminUserId,
        string sellerId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs access to KYC document views.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin user accessing the data.</param>
    /// <param name="submissionId">The ID of the KYC submission being accessed.</param>
    /// <param name="ipAddress">The IP address from which the access occurred (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogKycDocumentAccessAsync(
        string adminUserId,
        Guid submissionId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs access to store or seller detail views.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin user accessing the data.</param>
    /// <param name="storeId">The ID of the store being accessed.</param>
    /// <param name="ipAddress">The IP address from which the access occurred (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogStoreDetailsAccessAsync(
        string adminUserId,
        Guid storeId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
