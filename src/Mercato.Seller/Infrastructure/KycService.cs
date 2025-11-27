using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for KYC operations.
/// </summary>
public class KycService : IKycService
{
    private readonly IKycRepository _kycRepository;
    private readonly ILogger<KycService> _logger;
    private readonly UserManager<IdentityUser> _userManager;

    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private const string SellerRole = "Seller";
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="KycService"/> class.
    /// </summary>
    /// <param name="kycRepository">The KYC repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="userManager">The user manager for role assignment.</param>
    public KycService(IKycRepository kycRepository, ILogger<KycService> logger, UserManager<IdentityUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(kycRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userManager);

        _kycRepository = kycRepository;
        _logger = logger;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<SubmitKycResult> SubmitAsync(SubmitKycCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation("Processing KYC submission for seller {SellerId}", command.SellerId);

        // Validate command
        var errors = ValidateCommand(command);
        if (errors.Count > 0)
        {
            _logger.LogWarning("KYC submission validation failed for seller {SellerId}: {Errors}",
                command.SellerId, string.Join(", ", errors));
            return SubmitKycResult.Failure(errors);
        }

        // Create KYC submission
        var submission = new KycSubmission
        {
            Id = Guid.NewGuid(),
            SellerId = command.SellerId,
            DocumentType = command.DocumentType,
            DocumentFileName = command.DocumentFileName,
            DocumentContentType = command.DocumentContentType,
            DocumentData = command.DocumentData,
            Status = KycStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        await _kycRepository.AddAsync(submission);

        // Create audit log entry
        var auditLog = new KycAuditLog
        {
            Id = Guid.NewGuid(),
            KycSubmissionId = submission.Id,
            Action = "Submitted",
            OldStatus = null,
            NewStatus = KycStatus.Pending,
            PerformedBy = command.SellerId,
            PerformedAt = DateTimeOffset.UtcNow,
            Details = $"KYC document submitted: {command.DocumentFileName} ({command.DocumentType})"
        };

        await _kycRepository.AddAuditLogAsync(auditLog);

        _logger.LogInformation("KYC submission created successfully with ID {SubmissionId} for seller {SellerId}",
            submission.Id, command.SellerId);

        return SubmitKycResult.Success(submission.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KycSubmission>> GetSubmissionsBySellerAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _kycRepository.GetBySellerIdAsync(sellerId);
    }

    /// <inheritdoc />
    public async Task<bool> IsSellerKycApprovedAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        var submissions = await _kycRepository.GetBySellerIdAsync(sellerId);
        return submissions.Any(s => s.Status == KycStatus.Approved);
    }

    /// <inheritdoc />
    public async Task<ApproveKycResult> ApproveKycAsync(ApproveKycCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.AdminUserId);

        _logger.LogInformation("Processing KYC approval for submission {SubmissionId} by admin {AdminUserId}",
            command.SubmissionId, command.AdminUserId);

        // Get the KYC submission
        var submission = await _kycRepository.GetByIdAsync(command.SubmissionId);
        if (submission == null)
        {
            _logger.LogWarning("KYC submission {SubmissionId} not found", command.SubmissionId);
            return ApproveKycResult.Failure("KYC submission not found.");
        }

        // Validate the submission is in a valid state for approval
        if (submission.Status != KycStatus.Pending && submission.Status != KycStatus.UnderReview)
        {
            _logger.LogWarning("KYC submission {SubmissionId} is not in a valid state for approval. Current status: {Status}",
                command.SubmissionId, submission.Status);
            return ApproveKycResult.Failure($"KYC submission cannot be approved. Current status: {submission.Status}.");
        }

        // Find the user to assign the Seller role
        var user = await _userManager.FindByIdAsync(submission.SellerId);
        if (user == null)
        {
            _logger.LogWarning("User {SellerId} not found for KYC submission {SubmissionId}",
                submission.SellerId, command.SubmissionId);
            return ApproveKycResult.Failure("Seller user not found.");
        }

        // Update submission status
        var oldStatus = submission.Status;
        submission.Status = KycStatus.Approved;
        submission.ReviewedAt = DateTimeOffset.UtcNow;
        submission.ReviewedBy = command.AdminUserId;

        await _kycRepository.UpdateAsync(submission);

        // Assign the Seller role
        var roleResult = await _userManager.AddToRoleAsync(user, SellerRole);
        if (!roleResult.Succeeded)
        {
            // Compensating transaction: revert the submission status
            try
            {
                submission.Status = oldStatus;
                submission.ReviewedAt = null;
                submission.ReviewedBy = null;
                await _kycRepository.UpdateAsync(submission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revert KYC submission {SubmissionId} status after role assignment failure. Manual intervention required.",
                    command.SubmissionId);
            }

            var errorDescriptions = roleResult.Errors.Select(e => e.Description);
            var errorMessage = string.Join(", ", errorDescriptions);
            _logger.LogError("Failed to assign Seller role to user {SellerId}. Errors: {Errors}",
                submission.SellerId, errorMessage);
            return ApproveKycResult.Failure($"Failed to assign Seller role: {errorMessage}");
        }

        // Create audit log entry
        var auditLog = new KycAuditLog
        {
            Id = Guid.NewGuid(),
            KycSubmissionId = submission.Id,
            Action = "Approved",
            OldStatus = oldStatus,
            NewStatus = KycStatus.Approved,
            PerformedBy = command.AdminUserId,
            PerformedAt = DateTimeOffset.UtcNow,
            Details = $"KYC submission approved. Seller role assigned to user {submission.SellerId}."
        };

        await _kycRepository.AddAuditLogAsync(auditLog);

        _logger.LogInformation("KYC submission {SubmissionId} approved successfully. Seller role assigned to user {SellerId}",
            command.SubmissionId, submission.SellerId);

        return ApproveKycResult.Success();
    }

    private static List<string> ValidateCommand(SubmitKycCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.DocumentFileName))
        {
            errors.Add("Document file name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.DocumentContentType))
        {
            errors.Add("Document content type is required.");
        }
        else if (!AllowedContentTypes.Contains(command.DocumentContentType.ToLowerInvariant()))
        {
            errors.Add("Invalid document type. Only PDF, JPG, and PNG files are allowed.");
        }

        if (command.DocumentData == null || command.DocumentData.Length == 0)
        {
            errors.Add("Document data is required.");
        }
        else if (command.DocumentData.Length > MaxFileSizeBytes)
        {
            errors.Add($"Document size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB.");
        }

        if (!Enum.IsDefined(typeof(KycDocumentType), command.DocumentType))
        {
            errors.Add("Invalid document type specified.");
        }

        return errors;
    }
}
