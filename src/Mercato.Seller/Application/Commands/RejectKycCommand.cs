namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command to reject a KYC submission.
/// </summary>
public class RejectKycCommand
{
    /// <summary>
    /// Gets or sets the ID of the KYC submission to reject.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the admin user rejecting the submission.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for rejecting the submission.
    /// </summary>
    public string RejectionReason { get; set; } = string.Empty;
}
