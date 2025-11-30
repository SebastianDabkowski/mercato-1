using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class LegalContentManagementServiceTests
{
    private readonly Mock<ILegalDocumentRepository> _mockDocumentRepository;
    private readonly Mock<ILegalDocumentVersionRepository> _mockVersionRepository;
    private readonly Mock<ILegalConsentRepository> _mockConsentRepository;
    private readonly Mock<ILogger<LegalContentManagementService>> _mockLogger;

    public LegalContentManagementServiceTests()
    {
        _mockDocumentRepository = new Mock<ILegalDocumentRepository>(MockBehavior.Strict);
        _mockVersionRepository = new Mock<ILegalDocumentVersionRepository>(MockBehavior.Strict);
        _mockConsentRepository = new Mock<ILegalConsentRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<LegalContentManagementService>>();
    }

    private LegalContentManagementService CreateService()
    {
        return new LegalContentManagementService(
            _mockDocumentRepository.Object,
            _mockVersionRepository.Object,
            _mockConsentRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LegalContentManagementService(null!, _mockVersionRepository.Object, _mockConsentRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullVersionRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LegalContentManagementService(_mockDocumentRepository.Object, null!, _mockConsentRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullConsentRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LegalContentManagementService(_mockDocumentRepository.Object, _mockVersionRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LegalContentManagementService(_mockDocumentRepository.Object, _mockVersionRepository.Object, _mockConsentRepository.Object, null!));
    }

    #endregion

    #region GetAllDocumentsAsync Tests

    [Fact]
    public async Task GetAllDocumentsAsync_ReturnsAllDocuments()
    {
        // Arrange
        var service = CreateService();
        var documents = new List<LegalDocument>
        {
            new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Title = "Terms" },
            new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.PrivacyPolicy, Title = "Privacy" }
        };

        _mockDocumentRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await service.GetAllDocumentsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Documents.Count);
        _mockDocumentRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllDocumentsAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockDocumentRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LegalDocument>());

        // Act
        var result = await service.GetAllDocumentsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Documents);
    }

    #endregion

    #region GetDocumentByIdAsync Tests

    [Fact]
    public async Task GetDocumentByIdAsync_ExistingDocument_ReturnsDocument()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();
        var document = new LegalDocument { Id = docId, DocumentType = LegalDocumentType.TermsOfService, Title = "Terms" };

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await service.GetDocumentByIdAsync(docId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Document);
        Assert.Equal(docId, result.Document.Id);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_NonExistingDocument_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocument?)null);

        // Act
        var result = await service.GetDocumentByIdAsync(docId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task GetDocumentByIdAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetDocumentByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Legal document ID is required"));
    }

    #endregion

    #region GetOrCreateDocumentByTypeAsync Tests

    [Fact]
    public async Task GetOrCreateDocumentByTypeAsync_ExistingDocument_ReturnsDocument()
    {
        // Arrange
        var service = CreateService();
        var document = new LegalDocument 
        { 
            Id = Guid.NewGuid(), 
            DocumentType = LegalDocumentType.TermsOfService, 
            Title = "Terms" 
        };

        _mockDocumentRepository
            .Setup(r => r.GetByTypeAsync(LegalDocumentType.TermsOfService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await service.GetOrCreateDocumentByTypeAsync(LegalDocumentType.TermsOfService, "user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Document);
        Assert.Equal(LegalDocumentType.TermsOfService, result.Document.DocumentType);
    }

    [Fact]
    public async Task GetOrCreateDocumentByTypeAsync_NoExistingDocument_CreatesNewDocument()
    {
        // Arrange
        var service = CreateService();

        _mockDocumentRepository
            .Setup(r => r.GetByTypeAsync(LegalDocumentType.PrivacyPolicy, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocument?)null);

        _mockDocumentRepository
            .Setup(r => r.AddAsync(It.IsAny<LegalDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocument doc, CancellationToken _) => doc);

        // Act
        var result = await service.GetOrCreateDocumentByTypeAsync(LegalDocumentType.PrivacyPolicy, "user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Document);
        Assert.Equal(LegalDocumentType.PrivacyPolicy, result.Document.DocumentType);
        Assert.Equal("Privacy Policy", result.Document.Title);
        Assert.Equal("user-1", result.Document.CreatedByUserId);
        _mockDocumentRepository.VerifyAll();
    }

    [Fact]
    public async Task GetOrCreateDocumentByTypeAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetOrCreateDocumentByTypeAsync(LegalDocumentType.TermsOfService, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region CreateVersionAsync Tests

    [Fact]
    public async Task CreateVersionAsync_ValidCommand_CreatesVersion()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();
        var document = new LegalDocument { Id = docId, DocumentType = LegalDocumentType.TermsOfService };
        var command = new CreateLegalDocumentVersionCommand
        {
            LegalDocumentId = docId,
            VersionNumber = "1.0",
            Content = "<p>Terms content</p>",
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByUserId = "admin-1"
        };

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockVersionRepository
            .Setup(r => r.VersionNumberExistsAsync(docId, "1.0", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockVersionRepository
            .Setup(r => r.AddAsync(It.IsAny<LegalDocumentVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocumentVersion v, CancellationToken _) => v);

        // Act
        var result = await service.CreateVersionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Version);
        Assert.Equal("1.0", result.Version.VersionNumber);
        Assert.Equal(docId, result.Version.LegalDocumentId);
        Assert.False(result.Version.IsPublished);
        _mockVersionRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateVersionAsync_DuplicateVersionNumber_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();
        var document = new LegalDocument { Id = docId, DocumentType = LegalDocumentType.TermsOfService };
        var command = new CreateLegalDocumentVersionCommand
        {
            LegalDocumentId = docId,
            VersionNumber = "1.0",
            Content = "<p>Content</p>",
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByUserId = "admin-1"
        };

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockVersionRepository
            .Setup(r => r.VersionNumberExistsAsync(docId, "1.0", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.CreateVersionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task CreateVersionAsync_EmptyVersionNumber_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateLegalDocumentVersionCommand
        {
            LegalDocumentId = Guid.NewGuid(),
            VersionNumber = "",
            Content = "<p>Content</p>",
            CreatedByUserId = "admin-1"
        };

        // Act
        var result = await service.CreateVersionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Version number is required"));
    }

    [Fact]
    public async Task CreateVersionAsync_EmptyContent_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateLegalDocumentVersionCommand
        {
            LegalDocumentId = Guid.NewGuid(),
            VersionNumber = "1.0",
            Content = "",
            CreatedByUserId = "admin-1"
        };

        // Act
        var result = await service.CreateVersionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Content is required"));
    }

    [Fact]
    public async Task CreateVersionAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateLegalDocumentVersionCommand
        {
            LegalDocumentId = Guid.NewGuid(),
            VersionNumber = "1.0",
            Content = "<p>Content</p>",
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateVersionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region UpdateVersionAsync Tests

    [Fact]
    public async Task UpdateVersionAsync_DraftVersion_UpdatesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var existingVersion = new LegalDocumentVersion
        {
            Id = versionId,
            LegalDocumentId = docId,
            VersionNumber = "1.0",
            Content = "<p>Old content</p>",
            IsPublished = false
        };

        var command = new UpdateLegalDocumentVersionCommand
        {
            Id = versionId,
            VersionNumber = "1.0",
            Content = "<p>New content</p>",
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(7),
            UpdatedByUserId = "admin-1"
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVersion);

        _mockVersionRepository
            .Setup(r => r.VersionNumberExistsAsync(docId, "1.0", versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockVersionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<LegalDocumentVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateVersionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Version);
        Assert.Equal("<p>New content</p>", result.Version.Content);
        _mockVersionRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateVersionAsync_PublishedEffectiveVersion_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var existingVersion = new LegalDocumentVersion
        {
            Id = versionId,
            LegalDocumentId = Guid.NewGuid(),
            VersionNumber = "1.0",
            Content = "<p>Content</p>",
            IsPublished = true,
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(-7) // Already effective
        };

        var command = new UpdateLegalDocumentVersionCommand
        {
            Id = versionId,
            VersionNumber = "1.0",
            Content = "<p>New content</p>",
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(7),
            UpdatedByUserId = "admin-1"
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVersion);

        // Act
        var result = await service.UpdateVersionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Cannot modify a published version that is already effective"));
    }

    #endregion

    #region PublishVersionAsync Tests

    [Fact]
    public async Task PublishVersionAsync_DraftVersion_PublishesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var version = new LegalDocumentVersion
        {
            Id = versionId,
            VersionNumber = "1.0",
            Content = "<p>Content</p>",
            IsPublished = false
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _mockVersionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<LegalDocumentVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.PublishVersionAsync(versionId, "admin-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Version);
        Assert.True(result.Version.IsPublished);
        _mockVersionRepository.VerifyAll();
    }

    [Fact]
    public async Task PublishVersionAsync_AlreadyPublished_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var version = new LegalDocumentVersion
        {
            Id = versionId,
            VersionNumber = "1.0",
            Content = "<p>Content</p>",
            IsPublished = true
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        // Act
        var result = await service.PublishVersionAsync(versionId, "admin-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already published"));
    }

    [Fact]
    public async Task PublishVersionAsync_EmptyContent_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var version = new LegalDocumentVersion
        {
            Id = versionId,
            VersionNumber = "1.0",
            Content = "",
            IsPublished = false
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        // Act
        var result = await service.PublishVersionAsync(versionId, "admin-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("empty content"));
    }

    #endregion

    #region RecordConsentAsync Tests

    [Fact]
    public async Task RecordConsentAsync_ValidCommand_RecordsConsent()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var version = new LegalDocumentVersion
        {
            Id = versionId,
            LegalDocumentId = docId,
            VersionNumber = "1.0",
            IsPublished = true
        };
        var document = new LegalDocument
        {
            Id = docId,
            DocumentType = LegalDocumentType.TermsOfService
        };
        var command = new RecordConsentCommand
        {
            UserId = "user-123",
            LegalDocumentVersionId = versionId,
            ConsentContext = "Registration"
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockConsentRepository
            .Setup(r => r.AddAsync(It.IsAny<LegalConsent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalConsent c, CancellationToken _) => c);

        // Act
        var result = await service.RecordConsentAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Consent);
        Assert.Equal("user-123", result.Consent.UserId);
        Assert.Equal(versionId, result.Consent.LegalDocumentVersionId);
        Assert.Equal("Registration", result.Consent.ConsentContext);
        _mockConsentRepository.VerifyAll();
    }

    [Fact]
    public async Task RecordConsentAsync_UnpublishedVersion_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var version = new LegalDocumentVersion
        {
            Id = versionId,
            VersionNumber = "1.0",
            IsPublished = false
        };
        var command = new RecordConsentCommand
        {
            UserId = "user-123",
            LegalDocumentVersionId = versionId,
            ConsentContext = "Registration"
        };

        _mockVersionRepository
            .Setup(r => r.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        // Act
        var result = await service.RecordConsentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("unpublished"));
    }

    [Fact]
    public async Task RecordConsentAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RecordConsentCommand
        {
            UserId = "",
            LegalDocumentVersionId = Guid.NewGuid(),
            ConsentContext = "Registration"
        };

        // Act
        var result = await service.RecordConsentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task RecordConsentAsync_MissingConsentContext_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RecordConsentCommand
        {
            UserId = "user-123",
            LegalDocumentVersionId = Guid.NewGuid(),
            ConsentContext = ""
        };

        // Act
        var result = await service.RecordConsentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Consent context is required"));
    }

    #endregion

    #region CheckConsentAsync Tests

    [Fact]
    public async Task CheckConsentAsync_UserHasConsented_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var version = new LegalDocumentVersion { Id = versionId, VersionNumber = "1.0" };
        var consent = new LegalConsent
        {
            UserId = "user-123",
            LegalDocumentVersionId = versionId,
            DocumentType = LegalDocumentType.TermsOfService
        };

        _mockVersionRepository
            .Setup(r => r.GetActiveVersionByTypeAsync(LegalDocumentType.TermsOfService, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _mockConsentRepository
            .Setup(r => r.GetLatestConsentAsync("user-123", LegalDocumentType.TermsOfService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent);

        // Act
        var result = await service.CheckConsentAsync("user-123", LegalDocumentType.TermsOfService);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.HasConsented);
        Assert.NotNull(result.CurrentVersion);
        Assert.NotNull(result.LatestConsent);
    }

    [Fact]
    public async Task CheckConsentAsync_UserHasNotConsented_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var versionId = Guid.NewGuid();
        var oldVersionId = Guid.NewGuid();
        var version = new LegalDocumentVersion { Id = versionId, VersionNumber = "2.0" };
        var consent = new LegalConsent
        {
            UserId = "user-123",
            LegalDocumentVersionId = oldVersionId, // Consented to old version
            DocumentType = LegalDocumentType.TermsOfService
        };

        _mockVersionRepository
            .Setup(r => r.GetActiveVersionByTypeAsync(LegalDocumentType.TermsOfService, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _mockConsentRepository
            .Setup(r => r.GetLatestConsentAsync("user-123", LegalDocumentType.TermsOfService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent);

        // Act
        var result = await service.CheckConsentAsync("user-123", LegalDocumentType.TermsOfService);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.HasConsented);
    }

    [Fact]
    public async Task CheckConsentAsync_NoActiveVersion_ReturnsFalseHasConsented()
    {
        // Arrange
        var service = CreateService();

        _mockVersionRepository
            .Setup(r => r.GetActiveVersionByTypeAsync(LegalDocumentType.TermsOfService, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocumentVersion?)null);

        _mockConsentRepository
            .Setup(r => r.GetLatestConsentAsync("user-123", LegalDocumentType.TermsOfService, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalConsent?)null);

        // Act
        var result = await service.CheckConsentAsync("user-123", LegalDocumentType.TermsOfService);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.HasConsented);
        Assert.Null(result.CurrentVersion);
    }

    [Fact]
    public async Task CheckConsentAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckConsentAsync("", LegalDocumentType.TermsOfService);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region GetActiveVersionAsync Tests

    [Fact]
    public async Task GetActiveVersionAsync_ExistingActiveVersion_ReturnsVersion()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();
        var document = new LegalDocument { Id = docId };
        var version = new LegalDocumentVersion { Id = Guid.NewGuid(), LegalDocumentId = docId, VersionNumber = "1.0" };

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockVersionRepository
            .Setup(r => r.GetActiveVersionAsync(docId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _mockVersionRepository
            .Setup(r => r.GetUpcomingVersionAsync(docId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocumentVersion?)null);

        // Act
        var result = await service.GetActiveVersionAsync(docId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Version);
        Assert.Equal("1.0", result.Version.VersionNumber);
    }

    [Fact]
    public async Task GetActiveVersionAsync_WithUpcomingVersion_ReturnsUpcomingVersion()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();
        var document = new LegalDocument { Id = docId };
        var activeVersion = new LegalDocumentVersion { Id = Guid.NewGuid(), LegalDocumentId = docId, VersionNumber = "1.0" };
        var upcomingVersion = new LegalDocumentVersion 
        { 
            Id = Guid.NewGuid(), 
            LegalDocumentId = docId, 
            VersionNumber = "2.0",
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(7)
        };

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockVersionRepository
            .Setup(r => r.GetActiveVersionAsync(docId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVersion);

        _mockVersionRepository
            .Setup(r => r.GetUpcomingVersionAsync(docId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(upcomingVersion);

        // Act
        var result = await service.GetActiveVersionAsync(docId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Version);
        Assert.NotNull(result.UpcomingVersion);
        Assert.Equal("2.0", result.UpcomingVersion.VersionNumber);
    }

    [Fact]
    public async Task GetActiveVersionAsync_NoActiveVersion_ReturnsNoVersionFound()
    {
        // Arrange
        var service = CreateService();
        var docId = Guid.NewGuid();
        var document = new LegalDocument { Id = docId };

        _mockDocumentRepository
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockVersionRepository
            .Setup(r => r.GetActiveVersionAsync(docId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocumentVersion?)null);

        // Act
        var result = await service.GetActiveVersionAsync(docId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.Version);
        Assert.NotNull(result.Document);
    }

    #endregion
}
