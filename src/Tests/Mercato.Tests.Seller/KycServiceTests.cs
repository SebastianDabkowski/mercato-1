using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class KycServiceTests
{
    [Fact]
    public async Task SubmitAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<KycSubmission>()))
            .ReturnsAsync((KycSubmission s) => s);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.SubmissionId);
        mockRepository.Verify(r => r.AddAsync(It.Is<KycSubmission>(s =>
            s.SellerId == command.SellerId &&
            s.DocumentType == command.DocumentType &&
            s.DocumentFileName == command.DocumentFileName &&
            s.DocumentContentType == command.DocumentContentType &&
            s.Status == KycStatus.Pending)), Times.Once);
        mockRepository.Verify(r => r.AddAuditLogAsync(It.Is<KycAuditLog>(a =>
            a.Action == "Submitted" &&
            a.NewStatus == KycStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithFileSizeExceedingLimit_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.DocumentData = new byte[6 * 1024 * 1024]; // 6MB, exceeds 5MB limit

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("maximum allowed size", result.Errors[0]);
        Assert.Null(result.SubmissionId);
    }

    [Fact]
    public async Task SubmitAsync_WithInvalidContentType_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.DocumentContentType = "application/zip";

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("Only PDF, JPG, and PNG files are allowed", result.Errors[0]);
        Assert.Null(result.SubmissionId);
    }

    [Fact]
    public async Task SubmitAsync_WithEmptyDocumentData_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.DocumentData = [];

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("Document data is required", result.Errors[0]);
        Assert.Null(result.SubmissionId);
    }

    [Fact]
    public async Task SubmitAsync_WithEmptySellerId_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.SellerId = string.Empty;

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
        Assert.Null(result.SubmissionId);
    }

    [Fact]
    public async Task SubmitAsync_WithEmptyDocumentFileName_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.DocumentFileName = string.Empty;

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Document file name is required"));
        Assert.Null(result.SubmissionId);
    }

    [Fact]
    public async Task SubmitAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SubmitAsync(null!));
    }

    [Fact]
    public async Task GetSubmissionsBySellerAsync_ReturnsSubmissions()
    {
        // Arrange
        var sellerId = "seller-123";
        var submissions = new List<KycSubmission>
        {
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                DocumentType = KycDocumentType.PersonalId,
                DocumentFileName = "id.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [1, 2, 3],
                Status = KycStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow
            },
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                DocumentType = KycDocumentType.BusinessLicense,
                DocumentFileName = "license.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [4, 5, 6],
                Status = KycStatus.Approved,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(submissions);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.GetSubmissionsBySellerAsync(sellerId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.DocumentType == KycDocumentType.PersonalId);
        Assert.Contains(result, s => s.DocumentType == KycDocumentType.BusinessLicense);
        mockRepository.Verify(r => r.GetBySellerIdAsync(sellerId), Times.Once);
    }

    [Fact]
    public async Task GetSubmissionsBySellerAsync_WithEmptySellerId_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSubmissionsBySellerAsync(string.Empty));
    }

    [Fact]
    public async Task IsSellerKycApprovedAsync_WithApprovedSubmission_ReturnsTrue()
    {
        // Arrange
        var sellerId = "seller-123";
        var submissions = new List<KycSubmission>
        {
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Status = KycStatus.Approved,
                DocumentFileName = "id.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [1, 2, 3],
                SubmittedAt = DateTimeOffset.UtcNow
            }
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(submissions);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.IsSellerKycApprovedAsync(sellerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsSellerKycApprovedAsync_WithNoApprovedSubmission_ReturnsFalse()
    {
        // Arrange
        var sellerId = "seller-123";
        var submissions = new List<KycSubmission>
        {
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Status = KycStatus.Pending,
                DocumentFileName = "id.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [1, 2, 3],
                SubmittedAt = DateTimeOffset.UtcNow
            }
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(submissions);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.IsSellerKycApprovedAsync(sellerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsSellerKycApprovedAsync_WithEmptySellerId_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.IsSellerKycApprovedAsync(string.Empty));
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<KycService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KycService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KycService(mockRepository.Object, null!));
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/jpg")]
    [InlineData("image/png")]
    public async Task SubmitAsync_WithValidContentTypes_ReturnsSuccess(string contentType)
    {
        // Arrange
        var command = CreateValidCommand();
        command.DocumentContentType = contentType;

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<KycSubmission>()))
            .ReturnsAsync((KycSubmission s) => s);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(KycDocumentType.PersonalId)]
    [InlineData(KycDocumentType.BusinessLicense)]
    [InlineData(KycDocumentType.TaxCertificate)]
    [InlineData(KycDocumentType.AddressProof)]
    public async Task SubmitAsync_WithValidDocumentTypes_ReturnsSuccess(KycDocumentType documentType)
    {
        // Arrange
        var command = CreateValidCommand();
        command.DocumentType = documentType;

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<KycSubmission>()))
            .ReturnsAsync((KycSubmission s) => s);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await service.SubmitAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.AddAsync(It.Is<KycSubmission>(s =>
            s.DocumentType == documentType)), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_SetsSubmittedAtToUtcNow()
    {
        // Arrange
        var command = CreateValidCommand();
        KycSubmission? capturedSubmission = null;

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<KycSubmission>()))
            .Callback<KycSubmission>(s => capturedSubmission = s)
            .ReturnsAsync((KycSubmission s) => s);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object);
        var beforeSubmit = DateTimeOffset.UtcNow;

        // Act
        await service.SubmitAsync(command);
        var afterSubmit = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(capturedSubmission);
        Assert.True(capturedSubmission.SubmittedAt >= beforeSubmit);
        Assert.True(capturedSubmission.SubmittedAt <= afterSubmit);
    }

    private static SubmitKycCommand CreateValidCommand()
    {
        return new SubmitKycCommand
        {
            SellerId = "seller-123",
            DocumentType = KycDocumentType.PersonalId,
            DocumentFileName = "passport.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = new byte[1024] // 1KB test data
        };
    }
}
