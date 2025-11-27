using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.AspNetCore.Identity;
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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.IsSellerKycApprovedAsync(string.Empty));
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<KycService>>();
        var mockUserManager = CreateMockUserManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KycService(null!, mockLogger.Object, mockUserManager.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockUserManager = CreateMockUserManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KycService(mockRepository.Object, null!, mockUserManager.Object));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<KycService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KycService(mockRepository.Object, mockLogger.Object, null!));
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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

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
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);
        var beforeSubmit = DateTimeOffset.UtcNow;

        // Act
        await service.SubmitAsync(command);
        var afterSubmit = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(capturedSubmission);
        Assert.True(capturedSubmission.SubmittedAt >= beforeSubmit);
        Assert.True(capturedSubmission.SubmittedAt <= afterSubmit);
    }

    #region ApproveKycAsync Tests

    [Fact]
    public async Task ApproveKycAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var sellerId = "seller-123";
        var adminUserId = "admin-456";
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = sellerId,
            Status = KycStatus.Pending,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = adminUserId
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>()))
            .Returns(Task.CompletedTask);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockUserManager = CreateMockUserManager();
        var user = new IdentityUser { Id = sellerId, Email = "seller@test.com" };
        mockUserManager.Setup(u => u.FindByIdAsync(sellerId))
            .ReturnsAsync(user);
        mockUserManager.Setup(u => u.AddToRoleAsync(user, "Seller"))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<KycSubmission>(s =>
            s.Status == KycStatus.Approved &&
            s.ReviewedBy == adminUserId)), Times.Once);
        mockUserManager.Verify(u => u.AddToRoleAsync(user, "Seller"), Times.Once);
        mockRepository.Verify(r => r.AddAuditLogAsync(It.Is<KycAuditLog>(a =>
            a.Action == "Approved" &&
            a.NewStatus == KycStatus.Approved &&
            a.OldStatus == KycStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task ApproveKycAsync_WithUnderReviewStatus_ReturnsSuccess()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var sellerId = "seller-123";
        var adminUserId = "admin-456";
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = sellerId,
            Status = KycStatus.UnderReview,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = adminUserId
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>()))
            .Returns(Task.CompletedTask);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockUserManager = CreateMockUserManager();
        var user = new IdentityUser { Id = sellerId, Email = "seller@test.com" };
        mockUserManager.Setup(u => u.FindByIdAsync(sellerId))
            .ReturnsAsync(user);
        mockUserManager.Setup(u => u.AddToRoleAsync(user, "Seller"))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<KycSubmission>(s =>
            s.Status == KycStatus.Approved)), Times.Once);
    }

    [Fact]
    public async Task ApproveKycAsync_WithNonExistentSubmission_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync((KycSubmission?)null);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0]);
    }

    [Fact]
    public async Task ApproveKycAsync_WithAlreadyApprovedSubmission_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = "seller-123",
            Status = KycStatus.Approved,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be approved", result.Errors[0]);
    }

    [Fact]
    public async Task ApproveKycAsync_WithRejectedSubmission_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = "seller-123",
            Status = KycStatus.Rejected,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be approved", result.Errors[0]);
    }

    [Fact]
    public async Task ApproveKycAsync_WithNonExistentUser_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var sellerId = "seller-123";
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = sellerId,
            Status = KycStatus.Pending,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(u => u.FindByIdAsync(sellerId))
            .ReturnsAsync((IdentityUser?)null);

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("Seller user not found", result.Errors[0]);
    }

    [Fact]
    public async Task ApproveKycAsync_WhenRoleAssignmentFails_RevertsStatusAndReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var sellerId = "seller-123";
        var adminUserId = "admin-456";
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = sellerId,
            Status = KycStatus.Pending,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new ApproveKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = adminUserId
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>()))
            .Returns(Task.CompletedTask);

        var mockUserManager = CreateMockUserManager();
        var user = new IdentityUser { Id = sellerId, Email = "seller@test.com" };
        mockUserManager.Setup(u => u.FindByIdAsync(sellerId))
            .ReturnsAsync(user);
        mockUserManager.Setup(u => u.AddToRoleAsync(user, "Seller"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));

        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.ApproveKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("Failed to assign Seller role", result.Errors[0]);
        // Verify the status was reverted by checking UpdateAsync was called twice
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<KycSubmission>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ApproveKycAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ApproveKycAsync(null!));
    }

    [Fact]
    public async Task ApproveKycAsync_WithEmptyAdminUserId_ThrowsArgumentException()
    {
        // Arrange
        var command = new ApproveKycCommand
        {
            SubmissionId = Guid.NewGuid(),
            AdminUserId = string.Empty
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ApproveKycAsync(command));
    }

    #endregion

    #region RejectKycAsync Tests

    [Fact]
    public async Task RejectKycAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var sellerId = "seller-123";
        var adminUserId = "admin-456";
        var rejectionReason = "Document is not legible";
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = sellerId,
            Status = KycStatus.Pending,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new RejectKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = adminUserId,
            RejectionReason = rejectionReason
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>()))
            .Returns(Task.CompletedTask);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.RejectKycAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<KycSubmission>(s =>
            s.Status == KycStatus.Rejected &&
            s.RejectionReason == rejectionReason &&
            s.ReviewedBy == adminUserId)), Times.Once);
        mockRepository.Verify(r => r.AddAuditLogAsync(It.Is<KycAuditLog>(a =>
            a.Action == "Rejected" &&
            a.NewStatus == KycStatus.Rejected &&
            a.OldStatus == KycStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task RejectKycAsync_WithUnderReviewStatus_ReturnsSuccess()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var sellerId = "seller-123";
        var adminUserId = "admin-456";
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = sellerId,
            Status = KycStatus.UnderReview,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new RejectKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = adminUserId,
            RejectionReason = "Document expired"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>()))
            .Returns(Task.CompletedTask);
        mockRepository.Setup(r => r.AddAuditLogAsync(It.IsAny<KycAuditLog>()))
            .Returns(Task.CompletedTask);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.RejectKycAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<KycSubmission>(s =>
            s.Status == KycStatus.Rejected)), Times.Once);
    }

    [Fact]
    public async Task RejectKycAsync_WithNonExistentSubmission_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var command = new RejectKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456",
            RejectionReason = "Document not valid"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync((KycSubmission?)null);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.RejectKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0]);
    }

    [Fact]
    public async Task RejectKycAsync_WithAlreadyApprovedSubmission_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = "seller-123",
            Status = KycStatus.Approved,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new RejectKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456",
            RejectionReason = "Document not valid"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.RejectKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be rejected", result.Errors[0]);
    }

    [Fact]
    public async Task RejectKycAsync_WithAlreadyRejectedSubmission_ReturnsError()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = "seller-123",
            Status = KycStatus.Rejected,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var command = new RejectKycCommand
        {
            SubmissionId = submissionId,
            AdminUserId = "admin-456",
            RejectionReason = "Document not valid"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);

        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.RejectKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be rejected", result.Errors[0]);
    }

    [Fact]
    public async Task RejectKycAsync_WithEmptyRejectionReason_ReturnsError()
    {
        // Arrange
        var command = new RejectKycCommand
        {
            SubmissionId = Guid.NewGuid(),
            AdminUserId = "admin-456",
            RejectionReason = string.Empty
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.RejectKycAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("Rejection reason is required", result.Errors[0]);
    }

    [Fact]
    public async Task RejectKycAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RejectKycAsync(null!));
    }

    [Fact]
    public async Task RejectKycAsync_WithEmptyAdminUserId_ThrowsArgumentException()
    {
        // Arrange
        var command = new RejectKycCommand
        {
            SubmissionId = Guid.NewGuid(),
            AdminUserId = string.Empty,
            RejectionReason = "Invalid document"
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<KycService>>();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.RejectKycAsync(command));
    }

    #endregion

    #region GetAllSubmissionsAsync Tests

    [Fact]
    public async Task GetAllSubmissionsAsync_ReturnsAllSubmissions()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = "seller-1",
                Status = KycStatus.Pending,
                DocumentFileName = "id1.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [1, 2, 3],
                SubmittedAt = DateTimeOffset.UtcNow
            },
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = "seller-2",
                Status = KycStatus.Approved,
                DocumentFileName = "id2.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [4, 5, 6],
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(submissions);

        var mockLogger = new Mock<ILogger<KycService>>();
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.GetAllSubmissionsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetSubmissionsByStatusAsync Tests

    [Fact]
    public async Task GetSubmissionsByStatusAsync_ReturnsFilteredSubmissions()
    {
        // Arrange
        var status = KycStatus.Pending;
        var submissions = new List<KycSubmission>
        {
            new KycSubmission
            {
                Id = Guid.NewGuid(),
                SellerId = "seller-1",
                Status = KycStatus.Pending,
                DocumentFileName = "id1.pdf",
                DocumentContentType = "application/pdf",
                DocumentData = [1, 2, 3],
                SubmittedAt = DateTimeOffset.UtcNow
            }
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByStatusAsync(status))
            .ReturnsAsync(submissions);

        var mockLogger = new Mock<ILogger<KycService>>();
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.GetSubmissionsByStatusAsync(status);

        // Assert
        Assert.Single(result);
        Assert.All(result, s => Assert.Equal(KycStatus.Pending, s.Status));
        mockRepository.Verify(r => r.GetByStatusAsync(status), Times.Once);
    }

    #endregion

    #region GetSubmissionByIdAsync Tests

    [Fact]
    public async Task GetSubmissionByIdAsync_WithExistingId_ReturnsSubmission()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var submission = new KycSubmission
        {
            Id = submissionId,
            SellerId = "seller-1",
            Status = KycStatus.Pending,
            DocumentFileName = "id.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            SubmittedAt = DateTimeOffset.UtcNow
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync(submission);

        var mockLogger = new Mock<ILogger<KycService>>();
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.GetSubmissionByIdAsync(submissionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(submissionId, result.Id);
        mockRepository.Verify(r => r.GetByIdAsync(submissionId), Times.Once);
    }

    [Fact]
    public async Task GetSubmissionByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetByIdAsync(submissionId))
            .ReturnsAsync((KycSubmission?)null);

        var mockLogger = new Mock<ILogger<KycService>>();
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.GetSubmissionByIdAsync(submissionId);

        // Assert
        Assert.Null(result);
        mockRepository.Verify(r => r.GetByIdAsync(submissionId), Times.Once);
    }

    #endregion

    #region GetAuditLogsAsync Tests

    [Fact]
    public async Task GetAuditLogsAsync_ReturnsAuditLogs()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var auditLogs = new List<KycAuditLog>
        {
            new KycAuditLog
            {
                Id = Guid.NewGuid(),
                KycSubmissionId = submissionId,
                Action = "Submitted",
                OldStatus = null,
                NewStatus = KycStatus.Pending,
                PerformedBy = "seller-1",
                PerformedAt = DateTimeOffset.UtcNow,
                Details = "KYC submitted"
            },
            new KycAuditLog
            {
                Id = Guid.NewGuid(),
                KycSubmissionId = submissionId,
                Action = "Approved",
                OldStatus = KycStatus.Pending,
                NewStatus = KycStatus.Approved,
                PerformedBy = "admin-1",
                PerformedAt = DateTimeOffset.UtcNow.AddMinutes(10),
                Details = "KYC approved"
            }
        };

        var mockRepository = new Mock<IKycRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetAuditLogsAsync(submissionId))
            .ReturnsAsync(auditLogs);

        var mockLogger = new Mock<ILogger<KycService>>();
        var mockUserManager = CreateMockUserManager();
        var service = new KycService(mockRepository.Object, mockLogger.Object, mockUserManager.Object);

        // Act
        var result = await service.GetAuditLogsAsync(submissionId);

        // Assert
        Assert.Equal(2, result.Count);
        mockRepository.Verify(r => r.GetAuditLogsAsync(submissionId), Times.Once);
    }

    #endregion

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

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
