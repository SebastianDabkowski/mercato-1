using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing legal content from the admin panel.
/// </summary>
public interface ILegalContentManagementService
{
    /// <summary>
    /// Gets all legal documents.
    /// </summary>
    /// <returns>The result containing all legal documents.</returns>
    Task<GetLegalDocumentsResult> GetAllDocumentsAsync();

    /// <summary>
    /// Gets a specific legal document by ID.
    /// </summary>
    /// <param name="id">The legal document identifier.</param>
    /// <returns>The result containing the legal document if found.</returns>
    Task<GetLegalDocumentResult> GetDocumentByIdAsync(Guid id);

    /// <summary>
    /// Gets a legal document by its type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <returns>The result containing the legal document if found.</returns>
    Task<GetLegalDocumentResult> GetDocumentByTypeAsync(LegalDocumentType documentType);

    /// <summary>
    /// Gets or creates a legal document for the specified type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="userId">The user ID performing the operation.</param>
    /// <returns>The result containing the legal document.</returns>
    Task<GetLegalDocumentResult> GetOrCreateDocumentByTypeAsync(LegalDocumentType documentType, string userId);

    /// <summary>
    /// Gets all versions for a specific legal document.
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <returns>The result containing all versions.</returns>
    Task<GetLegalDocumentVersionsResult> GetDocumentVersionsAsync(Guid legalDocumentId);

    /// <summary>
    /// Gets a specific version by ID.
    /// </summary>
    /// <param name="versionId">The version identifier.</param>
    /// <returns>The result containing the version if found.</returns>
    Task<GetLegalDocumentVersionResult> GetVersionByIdAsync(Guid versionId);

    /// <summary>
    /// Gets the currently active version for a document.
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <returns>The result containing the active version if found.</returns>
    Task<GetLegalDocumentVersionResult> GetActiveVersionAsync(Guid legalDocumentId);

    /// <summary>
    /// Gets the active version for a document type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <returns>The result containing the active version if found.</returns>
    Task<GetLegalDocumentVersionResult> GetActiveVersionByTypeAsync(LegalDocumentType documentType);

    /// <summary>
    /// Gets any upcoming version for a document (future effective date).
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <returns>The result containing the upcoming version if found.</returns>
    Task<GetLegalDocumentVersionResult> GetUpcomingVersionAsync(Guid legalDocumentId);

    /// <summary>
    /// Creates a new version for a legal document.
    /// </summary>
    /// <param name="command">The command containing version details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateLegalDocumentVersionResult> CreateVersionAsync(CreateLegalDocumentVersionCommand command);

    /// <summary>
    /// Updates an existing version.
    /// </summary>
    /// <param name="command">The command containing updated version details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateLegalDocumentVersionResult> UpdateVersionAsync(UpdateLegalDocumentVersionCommand command);

    /// <summary>
    /// Publishes a version, making it available to become active on its effective date.
    /// </summary>
    /// <param name="versionId">The version ID to publish.</param>
    /// <param name="publishedByUserId">The user ID performing the publish.</param>
    /// <returns>The result of the publish operation.</returns>
    Task<PublishLegalDocumentVersionResult> PublishVersionAsync(Guid versionId, string publishedByUserId);

    /// <summary>
    /// Records a user's consent to a legal document version.
    /// </summary>
    /// <param name="command">The command containing consent details.</param>
    /// <returns>The result of the consent recording operation.</returns>
    Task<RecordConsentResult> RecordConsentAsync(RecordConsentCommand command);

    /// <summary>
    /// Checks if a user has consented to the current active version of a document type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="documentType">The type of legal document.</param>
    /// <returns>The result indicating consent status.</returns>
    Task<CheckConsentResult> CheckConsentAsync(string userId, LegalDocumentType documentType);
}

/// <summary>
/// Result of getting all legal documents.
/// </summary>
public class GetLegalDocumentsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the legal documents.
    /// </summary>
    public IReadOnlyList<LegalDocument> Documents { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with legal documents.
    /// </summary>
    /// <param name="documents">The legal documents.</param>
    /// <returns>A successful result.</returns>
    public static GetLegalDocumentsResult Success(IReadOnlyList<LegalDocument> documents) => new()
    {
        Succeeded = true,
        Errors = [],
        Documents = documents
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetLegalDocumentsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single legal document.
/// </summary>
public class GetLegalDocumentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the legal document.
    /// </summary>
    public LegalDocument? Document { get; private init; }

    /// <summary>
    /// Creates a successful result with a legal document.
    /// </summary>
    /// <param name="document">The legal document.</param>
    /// <returns>A successful result.</returns>
    public static GetLegalDocumentResult Success(LegalDocument document) => new()
    {
        Succeeded = true,
        Errors = [],
        Document = document
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetLegalDocumentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting legal document versions.
/// </summary>
public class GetLegalDocumentVersionsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the legal document versions.
    /// </summary>
    public IReadOnlyList<LegalDocumentVersion> Versions { get; private init; } = [];

    /// <summary>
    /// Gets the parent legal document.
    /// </summary>
    public LegalDocument? Document { get; private init; }

    /// <summary>
    /// Creates a successful result with versions.
    /// </summary>
    /// <param name="versions">The versions.</param>
    /// <param name="document">The parent document.</param>
    /// <returns>A successful result.</returns>
    public static GetLegalDocumentVersionsResult Success(IReadOnlyList<LegalDocumentVersion> versions, LegalDocument? document) => new()
    {
        Succeeded = true,
        Errors = [],
        Versions = versions,
        Document = document
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentVersionsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentVersionsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetLegalDocumentVersionsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single legal document version.
/// </summary>
public class GetLegalDocumentVersionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the legal document version.
    /// </summary>
    public LegalDocumentVersion? Version { get; private init; }

    /// <summary>
    /// Gets the parent legal document.
    /// </summary>
    public LegalDocument? Document { get; private init; }

    /// <summary>
    /// Gets any upcoming version that will take effect after the current one.
    /// </summary>
    public LegalDocumentVersion? UpcomingVersion { get; private init; }

    /// <summary>
    /// Creates a successful result with a version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="document">The parent document.</param>
    /// <param name="upcomingVersion">Optional upcoming version.</param>
    /// <returns>A successful result.</returns>
    public static GetLegalDocumentVersionResult Success(LegalDocumentVersion version, LegalDocument? document = null, LegalDocumentVersion? upcomingVersion = null) => new()
    {
        Succeeded = true,
        Errors = [],
        Version = version,
        Document = document,
        UpcomingVersion = upcomingVersion
    };

    /// <summary>
    /// Creates a successful result with no version found.
    /// </summary>
    /// <param name="document">The parent document.</param>
    /// <returns>A successful result with no version.</returns>
    public static GetLegalDocumentVersionResult NoVersionFound(LegalDocument? document = null) => new()
    {
        Succeeded = true,
        Errors = [],
        Version = null,
        Document = document
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentVersionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetLegalDocumentVersionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetLegalDocumentVersionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new legal document version.
/// </summary>
public class CreateLegalDocumentVersionCommand
{
    /// <summary>
    /// Gets or sets the parent legal document ID.
    /// </summary>
    public Guid LegalDocumentId { get; set; }

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTML content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the effective date.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets an optional change summary.
    /// </summary>
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Gets or sets the user ID creating this version.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of creating a legal document version.
/// </summary>
public class CreateLegalDocumentVersionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the created version.
    /// </summary>
    public LegalDocumentVersion? Version { get; private init; }

    /// <summary>
    /// Creates a successful result with the created version.
    /// </summary>
    /// <param name="version">The created version.</param>
    /// <returns>A successful result.</returns>
    public static CreateLegalDocumentVersionResult Success(LegalDocumentVersion version) => new()
    {
        Succeeded = true,
        Errors = [],
        Version = version
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateLegalDocumentVersionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateLegalDocumentVersionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateLegalDocumentVersionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing legal document version.
/// </summary>
public class UpdateLegalDocumentVersionCommand
{
    /// <summary>
    /// Gets or sets the version ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTML content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the effective date.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets an optional change summary.
    /// </summary>
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Gets or sets the user ID updating this version.
    /// </summary>
    public string UpdatedByUserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of updating a legal document version.
/// </summary>
public class UpdateLegalDocumentVersionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the updated version.
    /// </summary>
    public LegalDocumentVersion? Version { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated version.
    /// </summary>
    /// <param name="version">The updated version.</param>
    /// <returns>A successful result.</returns>
    public static UpdateLegalDocumentVersionResult Success(LegalDocumentVersion version) => new()
    {
        Succeeded = true,
        Errors = [],
        Version = version
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateLegalDocumentVersionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateLegalDocumentVersionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateLegalDocumentVersionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of publishing a legal document version.
/// </summary>
public class PublishLegalDocumentVersionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the published version.
    /// </summary>
    public LegalDocumentVersion? Version { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="version">The published version.</param>
    /// <returns>A successful result.</returns>
    public static PublishLegalDocumentVersionResult Success(LegalDocumentVersion version) => new()
    {
        Succeeded = true,
        Errors = [],
        Version = version
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static PublishLegalDocumentVersionResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static PublishLegalDocumentVersionResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static PublishLegalDocumentVersionResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to record a user's consent.
/// </summary>
public class RecordConsentCommand
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal document version ID.
    /// </summary>
    public Guid LegalDocumentVersionId { get; set; }

    /// <summary>
    /// Gets or sets the optional hashed IP address.
    /// </summary>
    public string? IpAddressHash { get; set; }

    /// <summary>
    /// Gets or sets the consent context (e.g., "Registration", "Checkout").
    /// </summary>
    public string ConsentContext { get; set; } = string.Empty;
}

/// <summary>
/// Result of recording consent.
/// </summary>
public class RecordConsentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the recorded consent.
    /// </summary>
    public LegalConsent? Consent { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="consent">The recorded consent.</param>
    /// <returns>A successful result.</returns>
    public static RecordConsentResult Success(LegalConsent consent) => new()
    {
        Succeeded = true,
        Errors = [],
        Consent = consent
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RecordConsentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RecordConsentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RecordConsentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of checking consent status.
/// </summary>
public class CheckConsentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the user has consented to the current version.
    /// </summary>
    public bool HasConsented { get; private init; }

    /// <summary>
    /// Gets the current active version.
    /// </summary>
    public LegalDocumentVersion? CurrentVersion { get; private init; }

    /// <summary>
    /// Gets the user's most recent consent record for this document type.
    /// </summary>
    public LegalConsent? LatestConsent { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="hasConsented">Whether the user has consented.</param>
    /// <param name="currentVersion">The current active version.</param>
    /// <param name="latestConsent">The user's latest consent record.</param>
    /// <returns>A successful result.</returns>
    public static CheckConsentResult Success(bool hasConsented, LegalDocumentVersion? currentVersion, LegalConsent? latestConsent) => new()
    {
        Succeeded = true,
        Errors = [],
        HasConsented = hasConsented,
        CurrentVersion = currentVersion,
        LatestConsent = latestConsent
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CheckConsentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CheckConsentResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CheckConsentResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
