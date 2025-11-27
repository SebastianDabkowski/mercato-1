using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for KYC operations.
/// </summary>
public class KycService : IKycService
{
    private readonly IKycRepository _kycRepository;
    private readonly ILogger<KycService> _logger;

    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png"
    ];

    public KycService(IKycRepository kycRepository, ILogger<KycService> logger)
    {
        ArgumentNullException.ThrowIfNull(kycRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _kycRepository = kycRepository;
        _logger = logger;
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
