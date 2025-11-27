namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command to approve a KYC submission.
/// </summary>
public class ApproveKycCommand
{
    /// <summary>
    /// Gets or sets the ID of the KYC submission to approve.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the admin user approving the submission.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
