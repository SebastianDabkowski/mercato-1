using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Sellers;

/// <summary>
/// Page model for viewing and managing a KYC submission.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IKycService _kycService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IKycService kycService,
        ILogger<DetailsModel> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the KYC submission.
    /// </summary>
    public KycSubmission? Submission { get; set; }

    /// <summary>
    /// Gets or sets the audit logs for this submission.
    /// </summary>
    public IReadOnlyList<KycAuditLog> AuditLogs { get; set; } = [];

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load the submission details.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        _logger.LogInformation("Admin accessing KYC submission details for {SubmissionId}", id);

        Submission = await _kycService.GetSubmissionByIdAsync(id);

        if (Submission == null)
        {
            _logger.LogWarning("KYC submission {SubmissionId} not found", id);
            return NotFound();
        }

        AuditLogs = await _kycService.GetAuditLogsAsync(id);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to approve a submission.
    /// </summary>
    public async Task<IActionResult> OnPostApproveAsync(Guid id, bool confirmApprove)
    {
        if (!confirmApprove)
        {
            ErrorMessage = "You must confirm the approval.";
            Submission = await _kycService.GetSubmissionByIdAsync(id);
            if (Submission != null)
            {
                AuditLogs = await _kycService.GetAuditLogsAsync(id);
            }
            return Page();
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            _logger.LogError("Could not determine admin user ID.");
            TempData["ErrorMessage"] = "Could not verify your identity. Please log in again.";
            return RedirectToPage("Index");
        }

        _logger.LogInformation("Admin {AdminUserId} approving KYC submission {SubmissionId}", adminUserId, id);

        var command = new ApproveKycCommand
        {
            SubmissionId = id,
            AdminUserId = adminUserId
        };

        var result = await _kycService.ApproveKycAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "KYC submission approved successfully. Seller role has been assigned.";
            return RedirectToPage("Index");
        }

        // Reload data for display
        Submission = await _kycService.GetSubmissionByIdAsync(id);
        if (Submission != null)
        {
            AuditLogs = await _kycService.GetAuditLogsAsync(id);
        }

        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    /// <summary>
    /// Handles POST requests to reject a submission.
    /// </summary>
    public async Task<IActionResult> OnPostRejectAsync(Guid id, string rejectionReason)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            _logger.LogError("Could not determine admin user ID.");
            TempData["ErrorMessage"] = "Could not verify your identity. Please log in again.";
            return RedirectToPage("Index");
        }

        _logger.LogInformation("Admin {AdminUserId} rejecting KYC submission {SubmissionId}", adminUserId, id);

        var command = new RejectKycCommand
        {
            SubmissionId = id,
            AdminUserId = adminUserId,
            RejectionReason = rejectionReason
        };

        var result = await _kycService.RejectKycAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "KYC submission rejected successfully.";
            return RedirectToPage("Index");
        }

        // Reload data for display
        Submission = await _kycService.GetSubmissionByIdAsync(id);
        if (Submission != null)
        {
            AuditLogs = await _kycService.GetAuditLogsAsync(id);
        }

        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    /// <summary>
    /// Handles POST requests to download the document.
    /// </summary>
    public async Task<IActionResult> OnPostDownloadDocumentAsync(Guid id)
    {
        _logger.LogInformation("Admin downloading document for KYC submission {SubmissionId}", id);

        var submission = await _kycService.GetSubmissionByIdAsync(id);

        if (submission == null)
        {
            _logger.LogWarning("KYC submission {SubmissionId} not found for download", id);
            TempData["ErrorMessage"] = "KYC submission not found.";
            return RedirectToPage("Index");
        }

        return File(submission.DocumentData, submission.DocumentContentType, submission.DocumentFileName);
    }
}
