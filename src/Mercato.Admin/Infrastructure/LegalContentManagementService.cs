using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing legal content from the admin panel.
/// </summary>
public class LegalContentManagementService : ILegalContentManagementService
{
    private readonly ILegalDocumentRepository _documentRepository;
    private readonly ILegalDocumentVersionRepository _versionRepository;
    private readonly ILegalConsentRepository _consentRepository;
    private readonly ILogger<LegalContentManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalContentManagementService"/> class.
    /// </summary>
    /// <param name="documentRepository">The legal document repository.</param>
    /// <param name="versionRepository">The legal document version repository.</param>
    /// <param name="consentRepository">The legal consent repository.</param>
    /// <param name="logger">The logger.</param>
    public LegalContentManagementService(
        ILegalDocumentRepository documentRepository,
        ILegalDocumentVersionRepository versionRepository,
        ILegalConsentRepository consentRepository,
        ILogger<LegalContentManagementService> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _versionRepository = versionRepository ?? throw new ArgumentNullException(nameof(versionRepository));
        _consentRepository = consentRepository ?? throw new ArgumentNullException(nameof(consentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentsResult> GetAllDocumentsAsync()
    {
        try
        {
            var documents = await _documentRepository.GetAllAsync();
            return GetLegalDocumentsResult.Success(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all legal documents");
            return GetLegalDocumentsResult.Failure("An error occurred while retrieving legal documents.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentResult> GetDocumentByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return GetLegalDocumentResult.Failure("Legal document ID is required.");
        }

        try
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document == null)
            {
                return GetLegalDocumentResult.Failure($"Legal document with ID {id} not found.");
            }

            return GetLegalDocumentResult.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting legal document by ID {Id}", id);
            return GetLegalDocumentResult.Failure("An error occurred while retrieving the legal document.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentResult> GetDocumentByTypeAsync(LegalDocumentType documentType)
    {
        try
        {
            var document = await _documentRepository.GetByTypeAsync(documentType);
            if (document == null)
            {
                return GetLegalDocumentResult.Failure($"Legal document of type {documentType} not found.");
            }

            return GetLegalDocumentResult.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting legal document by type {DocumentType}", documentType);
            return GetLegalDocumentResult.Failure("An error occurred while retrieving the legal document.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentResult> GetOrCreateDocumentByTypeAsync(LegalDocumentType documentType, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return GetLegalDocumentResult.Failure("User ID is required.");
        }

        try
        {
            var document = await _documentRepository.GetByTypeAsync(documentType);
            if (document != null)
            {
                return GetLegalDocumentResult.Success(document);
            }

            // Create a new document for this type
            var newDocument = new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = documentType,
                Title = GetDefaultTitle(documentType),
                Description = GetDefaultDescription(documentType),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = userId
            };

            var created = await _documentRepository.AddAsync(newDocument);
            _logger.LogInformation("Created new legal document of type {DocumentType} by user {UserId}", documentType, userId);
            return GetLegalDocumentResult.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating legal document by type {DocumentType}", documentType);
            return GetLegalDocumentResult.Failure("An error occurred while retrieving or creating the legal document.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentVersionsResult> GetDocumentVersionsAsync(Guid legalDocumentId)
    {
        if (legalDocumentId == Guid.Empty)
        {
            return GetLegalDocumentVersionsResult.Failure("Legal document ID is required.");
        }

        try
        {
            var document = await _documentRepository.GetByIdAsync(legalDocumentId);
            var versions = await _versionRepository.GetByDocumentIdAsync(legalDocumentId);
            return GetLegalDocumentVersionsResult.Success(versions, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting versions for legal document {LegalDocumentId}", legalDocumentId);
            return GetLegalDocumentVersionsResult.Failure("An error occurred while retrieving document versions.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentVersionResult> GetVersionByIdAsync(Guid versionId)
    {
        if (versionId == Guid.Empty)
        {
            return GetLegalDocumentVersionResult.Failure("Version ID is required.");
        }

        try
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
            {
                return GetLegalDocumentVersionResult.Failure($"Version with ID {versionId} not found.");
            }

            var document = await _documentRepository.GetByIdAsync(version.LegalDocumentId);
            var upcomingVersion = await _versionRepository.GetUpcomingVersionAsync(version.LegalDocumentId, DateTimeOffset.UtcNow);

            return GetLegalDocumentVersionResult.Success(version, document, upcomingVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version by ID {VersionId}", versionId);
            return GetLegalDocumentVersionResult.Failure("An error occurred while retrieving the version.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentVersionResult> GetActiveVersionAsync(Guid legalDocumentId)
    {
        if (legalDocumentId == Guid.Empty)
        {
            return GetLegalDocumentVersionResult.Failure("Legal document ID is required.");
        }

        try
        {
            var document = await _documentRepository.GetByIdAsync(legalDocumentId);
            var version = await _versionRepository.GetActiveVersionAsync(legalDocumentId, DateTimeOffset.UtcNow);
            
            if (version == null)
            {
                return GetLegalDocumentVersionResult.NoVersionFound(document);
            }

            var upcomingVersion = await _versionRepository.GetUpcomingVersionAsync(legalDocumentId, DateTimeOffset.UtcNow);
            return GetLegalDocumentVersionResult.Success(version, document, upcomingVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active version for document {LegalDocumentId}", legalDocumentId);
            return GetLegalDocumentVersionResult.Failure("An error occurred while retrieving the active version.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentVersionResult> GetActiveVersionByTypeAsync(LegalDocumentType documentType)
    {
        try
        {
            var document = await _documentRepository.GetByTypeAsync(documentType);
            var version = await _versionRepository.GetActiveVersionByTypeAsync(documentType, DateTimeOffset.UtcNow);
            
            if (version == null)
            {
                return GetLegalDocumentVersionResult.NoVersionFound(document);
            }

            LegalDocumentVersion? upcomingVersion = null;
            if (document != null)
            {
                upcomingVersion = await _versionRepository.GetUpcomingVersionAsync(document.Id, DateTimeOffset.UtcNow);
            }

            return GetLegalDocumentVersionResult.Success(version, document, upcomingVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active version for document type {DocumentType}", documentType);
            return GetLegalDocumentVersionResult.Failure("An error occurred while retrieving the active version.");
        }
    }

    /// <inheritdoc/>
    public async Task<GetLegalDocumentVersionResult> GetUpcomingVersionAsync(Guid legalDocumentId)
    {
        if (legalDocumentId == Guid.Empty)
        {
            return GetLegalDocumentVersionResult.Failure("Legal document ID is required.");
        }

        try
        {
            var document = await _documentRepository.GetByIdAsync(legalDocumentId);
            var version = await _versionRepository.GetUpcomingVersionAsync(legalDocumentId, DateTimeOffset.UtcNow);
            
            if (version == null)
            {
                return GetLegalDocumentVersionResult.NoVersionFound(document);
            }

            return GetLegalDocumentVersionResult.Success(version, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming version for document {LegalDocumentId}", legalDocumentId);
            return GetLegalDocumentVersionResult.Failure("An error occurred while retrieving the upcoming version.");
        }
    }

    /// <inheritdoc/>
    public async Task<CreateLegalDocumentVersionResult> CreateVersionAsync(CreateLegalDocumentVersionCommand command)
    {
        var validationErrors = ValidateCreateVersionCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateLegalDocumentVersionResult.Failure(validationErrors);
        }

        try
        {
            // Verify the document exists
            var document = await _documentRepository.GetByIdAsync(command.LegalDocumentId);
            if (document == null)
            {
                return CreateLegalDocumentVersionResult.Failure($"Legal document with ID {command.LegalDocumentId} not found.");
            }

            // Check for duplicate version number
            if (await _versionRepository.VersionNumberExistsAsync(command.LegalDocumentId, command.VersionNumber))
            {
                return CreateLegalDocumentVersionResult.Failure($"Version number '{command.VersionNumber}' already exists for this document.");
            }

            var version = new LegalDocumentVersion
            {
                Id = Guid.NewGuid(),
                LegalDocumentId = command.LegalDocumentId,
                VersionNumber = command.VersionNumber,
                Content = command.Content,
                EffectiveDate = command.EffectiveDate,
                IsPublished = false,
                ChangeSummary = command.ChangeSummary,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = command.CreatedByUserId
            };

            var created = await _versionRepository.AddAsync(version);
            _logger.LogInformation("Created version {VersionNumber} for legal document {LegalDocumentId} by user {UserId}",
                version.VersionNumber, version.LegalDocumentId, version.CreatedByUserId);

            return CreateLegalDocumentVersionResult.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version for legal document {LegalDocumentId}", command.LegalDocumentId);
            return CreateLegalDocumentVersionResult.Failure("An error occurred while creating the version.");
        }
    }

    /// <inheritdoc/>
    public async Task<UpdateLegalDocumentVersionResult> UpdateVersionAsync(UpdateLegalDocumentVersionCommand command)
    {
        var validationErrors = ValidateUpdateVersionCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateLegalDocumentVersionResult.Failure(validationErrors);
        }

        try
        {
            var version = await _versionRepository.GetByIdAsync(command.Id);
            if (version == null)
            {
                return UpdateLegalDocumentVersionResult.Failure($"Version with ID {command.Id} not found.");
            }

            // Cannot update published versions that are already effective
            if (version.IsPublished && version.EffectiveDate <= DateTimeOffset.UtcNow)
            {
                return UpdateLegalDocumentVersionResult.Failure("Cannot modify a published version that is already effective. Create a new version instead.");
            }

            // Check for duplicate version number (excluding current version)
            if (await _versionRepository.VersionNumberExistsAsync(version.LegalDocumentId, command.VersionNumber, command.Id))
            {
                return UpdateLegalDocumentVersionResult.Failure($"Version number '{command.VersionNumber}' already exists for this document.");
            }

            version.VersionNumber = command.VersionNumber;
            version.Content = command.Content;
            version.EffectiveDate = command.EffectiveDate;
            version.ChangeSummary = command.ChangeSummary;
            version.UpdatedAt = DateTimeOffset.UtcNow;
            version.UpdatedByUserId = command.UpdatedByUserId;

            await _versionRepository.UpdateAsync(version);
            _logger.LogInformation("Updated version {VersionId} by user {UserId}", version.Id, version.UpdatedByUserId);

            return UpdateLegalDocumentVersionResult.Success(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating version {VersionId}", command.Id);
            return UpdateLegalDocumentVersionResult.Failure("An error occurred while updating the version.");
        }
    }

    /// <inheritdoc/>
    public async Task<PublishLegalDocumentVersionResult> PublishVersionAsync(Guid versionId, string publishedByUserId)
    {
        if (versionId == Guid.Empty)
        {
            return PublishLegalDocumentVersionResult.Failure("Version ID is required.");
        }

        if (string.IsNullOrWhiteSpace(publishedByUserId))
        {
            return PublishLegalDocumentVersionResult.Failure("User ID is required.");
        }

        try
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
            {
                return PublishLegalDocumentVersionResult.Failure($"Version with ID {versionId} not found.");
            }

            if (version.IsPublished)
            {
                return PublishLegalDocumentVersionResult.Failure("Version is already published.");
            }

            if (string.IsNullOrWhiteSpace(version.Content))
            {
                return PublishLegalDocumentVersionResult.Failure("Cannot publish a version with empty content.");
            }

            version.IsPublished = true;
            version.UpdatedAt = DateTimeOffset.UtcNow;
            version.UpdatedByUserId = publishedByUserId;

            await _versionRepository.UpdateAsync(version);
            _logger.LogInformation("Published version {VersionId} by user {UserId}", versionId, publishedByUserId);

            return PublishLegalDocumentVersionResult.Success(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing version {VersionId}", versionId);
            return PublishLegalDocumentVersionResult.Failure("An error occurred while publishing the version.");
        }
    }

    /// <inheritdoc/>
    public async Task<RecordConsentResult> RecordConsentAsync(RecordConsentCommand command)
    {
        var validationErrors = ValidateRecordConsentCommand(command);
        if (validationErrors.Count > 0)
        {
            return RecordConsentResult.Failure(validationErrors);
        }

        try
        {
            var version = await _versionRepository.GetByIdAsync(command.LegalDocumentVersionId);
            if (version == null)
            {
                return RecordConsentResult.Failure("Legal document version not found.");
            }

            if (!version.IsPublished)
            {
                return RecordConsentResult.Failure("Cannot consent to an unpublished version.");
            }

            var document = await _documentRepository.GetByIdAsync(version.LegalDocumentId);
            if (document == null)
            {
                return RecordConsentResult.Failure("Legal document not found.");
            }

            var consent = new LegalConsent
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                LegalDocumentVersionId = command.LegalDocumentVersionId,
                DocumentType = document.DocumentType,
                VersionNumber = version.VersionNumber,
                ConsentedAt = DateTimeOffset.UtcNow,
                IpAddressHash = command.IpAddressHash,
                ConsentContext = command.ConsentContext
            };

            var recorded = await _consentRepository.AddAsync(consent);
            _logger.LogInformation("Recorded consent for user {UserId} to version {VersionId} in context {Context}",
                command.UserId, command.LegalDocumentVersionId, command.ConsentContext);

            return RecordConsentResult.Success(recorded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording consent for user {UserId} to version {VersionId}",
                command.UserId, command.LegalDocumentVersionId);
            return RecordConsentResult.Failure("An error occurred while recording consent.");
        }
    }

    /// <inheritdoc/>
    public async Task<CheckConsentResult> CheckConsentAsync(string userId, LegalDocumentType documentType)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CheckConsentResult.Failure("User ID is required.");
        }

        try
        {
            var currentVersion = await _versionRepository.GetActiveVersionByTypeAsync(documentType, DateTimeOffset.UtcNow);
            var latestConsent = await _consentRepository.GetLatestConsentAsync(userId, documentType);

            bool hasConsented = false;
            if (currentVersion != null && latestConsent != null)
            {
                hasConsented = latestConsent.LegalDocumentVersionId == currentVersion.Id;
            }

            return CheckConsentResult.Success(hasConsented, currentVersion, latestConsent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking consent for user {UserId} and document type {DocumentType}",
                userId, documentType);
            return CheckConsentResult.Failure("An error occurred while checking consent status.");
        }
    }

    private static List<string> ValidateCreateVersionCommand(CreateLegalDocumentVersionCommand command)
    {
        var errors = new List<string>();

        if (command.LegalDocumentId == Guid.Empty)
        {
            errors.Add("Legal document ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.VersionNumber))
        {
            errors.Add("Version number is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            errors.Add("Content is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateVersionCommand(UpdateLegalDocumentVersionCommand command)
    {
        var errors = new List<string>();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Version ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.VersionNumber))
        {
            errors.Add("Version number is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            errors.Add("Content is required.");
        }

        if (string.IsNullOrWhiteSpace(command.UpdatedByUserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRecordConsentCommand(RecordConsentCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (command.LegalDocumentVersionId == Guid.Empty)
        {
            errors.Add("Legal document version ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ConsentContext))
        {
            errors.Add("Consent context is required.");
        }

        return errors;
    }

    private static string GetDefaultTitle(LegalDocumentType documentType)
    {
        return documentType switch
        {
            LegalDocumentType.TermsOfService => "Terms of Service",
            LegalDocumentType.PrivacyPolicy => "Privacy Policy",
            LegalDocumentType.CookiePolicy => "Cookie Policy",
            LegalDocumentType.SellerAgreement => "Seller Agreement",
            _ => documentType.ToString()
        };
    }

    private static string GetDefaultDescription(LegalDocumentType documentType)
    {
        return documentType switch
        {
            LegalDocumentType.TermsOfService => "The terms and conditions for using the marketplace platform.",
            LegalDocumentType.PrivacyPolicy => "How we collect, use, and protect your personal information.",
            LegalDocumentType.CookiePolicy => "Information about how we use cookies and similar technologies.",
            LegalDocumentType.SellerAgreement => "The agreement between sellers and the marketplace platform.",
            _ => $"Legal document for {documentType}"
        };
    }
}
