using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for KYC submission data access.
/// </summary>
public interface IKycRepository
{
    /// <summary>
    /// Gets a KYC submission by its unique identifier.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    /// <returns>The KYC submission if found; otherwise, null.</returns>
    Task<KycSubmission?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all KYC submissions for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>A read-only list of KYC submissions.</returns>
    Task<IReadOnlyList<KycSubmission>> GetBySellerIdAsync(string sellerId);

    /// <summary>
    /// Adds a new KYC submission.
    /// </summary>
    /// <param name="submission">The KYC submission to add.</param>
    /// <returns>The added KYC submission.</returns>
    Task<KycSubmission> AddAsync(KycSubmission submission);

    /// <summary>
    /// Updates an existing KYC submission.
    /// </summary>
    /// <param name="submission">The KYC submission to update.</param>
    Task UpdateAsync(KycSubmission submission);

    /// <summary>
    /// Adds an audit log entry for a KYC submission.
    /// </summary>
    /// <param name="auditLog">The audit log entry to add.</param>
    Task AddAuditLogAsync(KycAuditLog auditLog);

    /// <summary>
    /// Gets all audit logs for a specific KYC submission.
    /// </summary>
    /// <param name="kycSubmissionId">The KYC submission ID.</param>
    /// <returns>A read-only list of audit log entries.</returns>
    Task<IReadOnlyList<KycAuditLog>> GetAuditLogsAsync(Guid kycSubmissionId);
}
