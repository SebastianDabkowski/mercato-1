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
}
