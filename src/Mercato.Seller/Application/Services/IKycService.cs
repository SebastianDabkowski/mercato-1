using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for KYC operations.
/// </summary>
public interface IKycService
{
    /// <summary>
    /// Submits a KYC document for verification.
    /// </summary>
    /// <param name="command">The submission command.</param>
    /// <returns>The result of the submission.</returns>
    Task<SubmitKycResult> SubmitAsync(SubmitKycCommand command);

    /// <summary>
    /// Gets all KYC submissions for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>A read-only list of KYC submissions.</returns>
    Task<IReadOnlyList<KycSubmission>> GetSubmissionsBySellerAsync(string sellerId);

    /// <summary>
    /// Checks if a seller has been approved for KYC.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>True if the seller has at least one approved KYC submission; otherwise, false.</returns>
    Task<bool> IsSellerKycApprovedAsync(string sellerId);

    /// <summary>
    /// Approves a KYC submission and assigns the Seller role to the user.
    /// </summary>
    /// <param name="command">The approval command containing submission ID and admin user ID.</param>
    /// <returns>The result of the approval operation.</returns>
    Task<ApproveKycResult> ApproveKycAsync(ApproveKycCommand command);

    /// <summary>
    /// Rejects a KYC submission with a reason.
    /// </summary>
    /// <param name="command">The rejection command containing submission ID, admin user ID, and rejection reason.</param>
    /// <returns>The result of the rejection operation.</returns>
    Task<RejectKycResult> RejectKycAsync(RejectKycCommand command);

    /// <summary>
    /// Gets all KYC submissions.
    /// </summary>
    /// <returns>A read-only list of all KYC submissions.</returns>
    Task<IReadOnlyList<KycSubmission>> GetAllSubmissionsAsync();

    /// <summary>
    /// Gets all KYC submissions with a specific status.
    /// </summary>
    /// <param name="status">The KYC status to filter by.</param>
    /// <returns>A read-only list of KYC submissions with the specified status.</returns>
    Task<IReadOnlyList<KycSubmission>> GetSubmissionsByStatusAsync(KycStatus status);

    /// <summary>
    /// Gets a KYC submission by its ID.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    /// <returns>The KYC submission if found; otherwise, null.</returns>
    Task<KycSubmission?> GetSubmissionByIdAsync(Guid id);

    /// <summary>
    /// Gets all audit logs for a specific KYC submission.
    /// </summary>
    /// <param name="kycSubmissionId">The KYC submission ID.</param>
    /// <returns>A read-only list of audit log entries.</returns>
    Task<IReadOnlyList<KycAuditLog>> GetAuditLogsAsync(Guid kycSubmissionId);
}
