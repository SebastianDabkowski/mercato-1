using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Domain.Interfaces;
using Mercato.Buyer.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Buyer;

public class ConsentServiceTests
{
    private static readonly string TestUserId = "test-user-id";
    private static readonly Guid TestConsentTypeId = Guid.NewGuid();
    private static readonly Guid TestConsentVersionId = Guid.NewGuid();

    private readonly Mock<IConsentRepository> _mockRepository;
    private readonly Mock<ILogger<ConsentService>> _mockLogger;
    private readonly ConsentService _service;

    public ConsentServiceTests()
    {
        _mockRepository = new Mock<IConsentRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ConsentService>>();
        _service = new ConsentService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetConsentTypesAsync Tests

    [Fact]
    public async Task GetConsentTypesAsync_ReturnsActiveConsentTypes()
    {
        // Arrange
        var consentTypes = CreateTestConsentTypes();
        _mockRepository.Setup(r => r.GetActiveConsentTypesAsync())
            .ReturnsAsync(consentTypes);

        // Act
        var result = await _service.GetConsentTypesAsync(new GetConsentTypesQuery());

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ConsentTypes.Count);
        Assert.Equal("Newsletter", result.ConsentTypes[0].Name);
        Assert.Equal("Marketing", result.ConsentTypes[1].Name);
    }

    [Fact]
    public async Task GetConsentTypesAsync_MandatoryOnly_FiltersMandatoryTypes()
    {
        // Arrange
        var consentTypes = CreateTestConsentTypes();
        consentTypes[0].IsMandatory = true;
        _mockRepository.Setup(r => r.GetActiveConsentTypesAsync())
            .ReturnsAsync(consentTypes);

        // Act
        var result = await _service.GetConsentTypesAsync(new GetConsentTypesQuery { MandatoryOnly = true });

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.ConsentTypes);
        Assert.True(result.ConsentTypes[0].IsMandatory);
    }

    [Fact]
    public async Task GetConsentTypesAsync_NoVersions_ExcludesFromResult()
    {
        // Arrange
        var consentType = new ConsentType
        {
            Id = TestConsentTypeId,
            Code = "NO_VERSIONS",
            Name = "No Versions",
            Description = "Test consent without versions",
            IsActive = true,
            DisplayOrder = 1,
            Versions = []
        };
        _mockRepository.Setup(r => r.GetActiveConsentTypesAsync())
            .ReturnsAsync(new List<ConsentType> { consentType });

        // Act
        var result = await _service.GetConsentTypesAsync(new GetConsentTypesQuery());

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.ConsentTypes);
    }

    #endregion

    #region GetUserConsentsAsync Tests

    [Fact]
    public async Task GetUserConsentsAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var query = new GetUserConsentsQuery { UserId = string.Empty };

        // Act
        var result = await _service.GetUserConsentsAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetUserConsentsAsync_ValidUserId_ReturnsConsents()
    {
        // Arrange
        var query = new GetUserConsentsQuery { UserId = TestUserId };
        var consentTypes = CreateTestConsentTypes();
        var userConsents = CreateTestUserConsents(consentTypes);

        _mockRepository.Setup(r => r.GetUserConsentsAsync(TestUserId))
            .ReturnsAsync(userConsents);
        _mockRepository.Setup(r => r.GetActiveConsentTypesAsync())
            .ReturnsAsync(consentTypes);

        // Act
        var result = await _service.GetUserConsentsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Consents.Count);
    }

    [Fact]
    public async Task GetUserConsentsAsync_NoExistingConsent_ReturnsNotGranted()
    {
        // Arrange
        var query = new GetUserConsentsQuery { UserId = TestUserId };
        var consentTypes = CreateTestConsentTypes();

        _mockRepository.Setup(r => r.GetUserConsentsAsync(TestUserId))
            .ReturnsAsync(new List<UserConsent>());
        _mockRepository.Setup(r => r.GetActiveConsentTypesAsync())
            .ReturnsAsync(consentTypes);

        // Act
        var result = await _service.GetUserConsentsAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Consents.Count);
        Assert.All(result.Consents, c => Assert.False(c.IsGranted));
    }

    #endregion

    #region RecordConsentAsync Tests

    [Fact]
    public async Task RecordConsentAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var command = new RecordConsentCommand
        {
            UserId = string.Empty,
            ConsentVersionId = TestConsentVersionId,
            IsGranted = true
        };

        // Act
        var result = await _service.RecordConsentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task RecordConsentAsync_EmptyConsentVersionId_ReturnsFailure()
    {
        // Arrange
        var command = new RecordConsentCommand
        {
            UserId = TestUserId,
            ConsentVersionId = Guid.Empty,
            IsGranted = true
        };

        // Act
        var result = await _service.RecordConsentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Consent version ID is required.", result.Errors);
    }

    [Fact]
    public async Task RecordConsentAsync_ConsentVersionNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new RecordConsentCommand
        {
            UserId = TestUserId,
            ConsentVersionId = TestConsentVersionId,
            IsGranted = true
        };

        _mockRepository.Setup(r => r.GetVersionByIdAsync(TestConsentVersionId))
            .ReturnsAsync((ConsentVersion?)null);

        // Act
        var result = await _service.RecordConsentAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Consent version not found.", result.Errors);
    }

    [Fact]
    public async Task RecordConsentAsync_ValidCommand_RecordsConsent()
    {
        // Arrange
        var command = new RecordConsentCommand
        {
            UserId = TestUserId,
            ConsentVersionId = TestConsentVersionId,
            IsGranted = true,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        var consentVersion = new ConsentVersion
        {
            Id = TestConsentVersionId,
            ConsentTypeId = TestConsentTypeId,
            VersionNumber = 1,
            ConsentText = "Test consent text",
            ConsentType = new ConsentType { Code = "NEWSLETTER" }
        };

        _mockRepository.Setup(r => r.GetVersionByIdAsync(TestConsentVersionId))
            .ReturnsAsync(consentVersion);
        _mockRepository.Setup(r => r.AddUserConsentAsync(It.IsAny<UserConsent>()))
            .ReturnsAsync((UserConsent c) => c);

        // Act
        var result = await _service.RecordConsentAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ConsentId);
        _mockRepository.Verify(r => r.AddUserConsentAsync(It.Is<UserConsent>(
            c => c.UserId == TestUserId && 
                 c.ConsentVersionId == TestConsentVersionId && 
                 c.IsGranted == true)), Times.Once);
    }

    #endregion

    #region RecordMultipleConsentsAsync Tests

    [Fact]
    public async Task RecordMultipleConsentsAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var command = new RecordMultipleConsentsCommand
        {
            UserId = string.Empty,
            Consents = [new ConsentDecision { ConsentTypeCode = "NEWSLETTER", IsGranted = true }]
        };

        // Act
        var result = await _service.RecordMultipleConsentsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task RecordMultipleConsentsAsync_EmptyConsents_ReturnsFailure()
    {
        // Arrange
        var command = new RecordMultipleConsentsCommand
        {
            UserId = TestUserId,
            Consents = []
        };

        // Act
        var result = await _service.RecordMultipleConsentsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("At least one consent decision is required.", result.Errors);
    }

    [Fact]
    public async Task RecordMultipleConsentsAsync_ValidCommand_RecordsAllConsents()
    {
        // Arrange
        var consentType = CreateTestConsentTypes()[0];
        var currentVersion = consentType.Versions.First();

        var command = new RecordMultipleConsentsCommand
        {
            UserId = TestUserId,
            Consents =
            [
                new ConsentDecision { ConsentTypeCode = "NEWSLETTER", IsGranted = true }
            ],
            IpAddress = "127.0.0.1"
        };

        _mockRepository.Setup(r => r.GetConsentTypeByCodeAsync("NEWSLETTER"))
            .ReturnsAsync(consentType);
        _mockRepository.Setup(r => r.GetCurrentVersionAsync(consentType.Id))
            .ReturnsAsync(currentVersion);
        _mockRepository.Setup(r => r.AddUserConsentAsync(It.IsAny<UserConsent>()))
            .ReturnsAsync((UserConsent c) => c);

        // Act
        var result = await _service.RecordMultipleConsentsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.ConsentsRecorded);
    }

    [Fact]
    public async Task RecordMultipleConsentsAsync_ConsentTypeNotFound_SkipsAndContinues()
    {
        // Arrange
        var command = new RecordMultipleConsentsCommand
        {
            UserId = TestUserId,
            Consents =
            [
                new ConsentDecision { ConsentTypeCode = "UNKNOWN", IsGranted = true }
            ]
        };

        _mockRepository.Setup(r => r.GetConsentTypeByCodeAsync("UNKNOWN"))
            .ReturnsAsync((ConsentType?)null);

        // Act
        var result = await _service.RecordMultipleConsentsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ConsentsRecorded);
    }

    #endregion

    #region HasActiveConsentAsync Tests

    [Fact]
    public async Task HasActiveConsentAsync_EmptyUserId_ReturnsFalse()
    {
        // Act
        var result = await _service.HasActiveConsentAsync(string.Empty, "NEWSLETTER");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasActiveConsentAsync_EmptyConsentTypeCode_ReturnsFalse()
    {
        // Act
        var result = await _service.HasActiveConsentAsync(TestUserId, string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasActiveConsentAsync_ValidParameters_CallsRepository()
    {
        // Arrange
        _mockRepository.Setup(r => r.HasActiveConsentAsync(TestUserId, "NEWSLETTER"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasActiveConsentAsync(TestUserId, "NEWSLETTER");

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.HasActiveConsentAsync(TestUserId, "NEWSLETTER"), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static List<ConsentType> CreateTestConsentTypes()
    {
        var consentType1Id = Guid.NewGuid();
        var consentType2Id = Guid.NewGuid();

        return
        [
            new ConsentType
            {
                Id = consentType1Id,
                Code = "NEWSLETTER",
                Name = "Newsletter",
                Description = "Receive newsletter emails",
                IsActive = true,
                IsMandatory = false,
                DisplayOrder = 1,
                Versions =
                [
                    new ConsentVersion
                    {
                        Id = Guid.NewGuid(),
                        ConsentTypeId = consentType1Id,
                        VersionNumber = 1,
                        ConsentText = "I agree to receive newsletter emails",
                        EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-30),
                        EffectiveTo = null,
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
                    }
                ]
            },
            new ConsentType
            {
                Id = consentType2Id,
                Code = "MARKETING",
                Name = "Marketing",
                Description = "Receive marketing communications",
                IsActive = true,
                IsMandatory = false,
                DisplayOrder = 2,
                Versions =
                [
                    new ConsentVersion
                    {
                        Id = Guid.NewGuid(),
                        ConsentTypeId = consentType2Id,
                        VersionNumber = 1,
                        ConsentText = "I agree to receive marketing communications",
                        EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-30),
                        EffectiveTo = null,
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
                    }
                ]
            }
        ];
    }

    private static List<UserConsent> CreateTestUserConsents(List<ConsentType> consentTypes)
    {
        return consentTypes.Select(ct => new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ConsentVersionId = ct.Versions.First().Id,
            IsGranted = true,
            ConsentedAt = DateTimeOffset.UtcNow.AddDays(-5),
            ConsentVersion = ct.Versions.First()
        }).ToList();
    }

    #endregion
}
