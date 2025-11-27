using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Web.Pages.Seller;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Mercato.Tests.Seller;

public class SellerDashboardIndexModelTests
{
    private const string TestSellerId = "seller-test-123";

    [Fact]
    public async Task OnGetAsync_WithNoSubmissions_SetsKycStatusToNotSubmitted()
    {
        // Arrange
        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(new List<KycSubmission>());

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.NotSubmitted, model.KycStatus);
        Assert.Null(model.LatestSubmission);
        Assert.False(model.HasError);
        Assert.Null(model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_WithPendingSubmission_SetsKycStatusToPending()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            CreateSubmission(KycStatus.Pending)
        };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.Pending, model.KycStatus);
        Assert.NotNull(model.LatestSubmission);
        Assert.False(model.HasError);
    }

    [Fact]
    public async Task OnGetAsync_WithUnderReviewSubmission_SetsKycStatusToUnderReview()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            CreateSubmission(KycStatus.UnderReview)
        };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.UnderReview, model.KycStatus);
        Assert.NotNull(model.LatestSubmission);
        Assert.False(model.HasError);
    }

    [Fact]
    public async Task OnGetAsync_WithApprovedSubmission_SetsKycStatusToVerified()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            CreateSubmission(KycStatus.Approved)
        };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.Verified, model.KycStatus);
        Assert.NotNull(model.LatestSubmission);
        Assert.False(model.HasError);
    }

    [Fact]
    public async Task OnGetAsync_WithRejectedSubmission_SetsKycStatusToRejected()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            CreateSubmission(KycStatus.Rejected, "Document unclear")
        };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.Rejected, model.KycStatus);
        Assert.NotNull(model.LatestSubmission);
        Assert.Equal("Document unclear", model.LatestSubmission.RejectionReason);
        Assert.False(model.HasError);
    }

    [Fact]
    public async Task OnGetAsync_WithMixedSubmissions_ApprovedTakesPriority()
    {
        // Arrange - If any submission is approved, status should be Verified
        var submissions = new List<KycSubmission>
        {
            CreateSubmission(KycStatus.Rejected, submittedAt: DateTimeOffset.UtcNow.AddDays(-2)),
            CreateSubmission(KycStatus.Approved, submittedAt: DateTimeOffset.UtcNow.AddDays(-1)),
            CreateSubmission(KycStatus.Pending, submittedAt: DateTimeOffset.UtcNow)
        };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.Verified, model.KycStatus);
    }

    [Fact]
    public async Task OnGetAsync_WithMixedSubmissions_UnderReviewTakesPriorityOverPending()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            CreateSubmission(KycStatus.Pending, submittedAt: DateTimeOffset.UtcNow.AddDays(-2)),
            CreateSubmission(KycStatus.UnderReview, submittedAt: DateTimeOffset.UtcNow)
        };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(KycVerificationStatus.UnderReview, model.KycStatus);
    }

    [Fact]
    public async Task OnGetAsync_SetsLatestSubmissionToMostRecent()
    {
        // Arrange
        var olderSubmission = CreateSubmission(KycStatus.Pending, submittedAt: DateTimeOffset.UtcNow.AddDays(-2));
        var newerSubmission = CreateSubmission(KycStatus.Pending, submittedAt: DateTimeOffset.UtcNow);

        var submissions = new List<KycSubmission> { olderSubmission, newerSubmission };

        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ReturnsAsync(submissions);

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.NotNull(model.LatestSubmission);
        Assert.Equal(newerSubmission.Id, model.LatestSubmission.Id);
    }

    [Fact]
    public async Task OnGetAsync_WhenServiceThrowsException_SetsHasErrorToTrue()
    {
        // Arrange
        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        mockKycService.Setup(s => s.GetSubmissionsBySellerAsync(TestSellerId))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var model = CreateModel(mockKycService.Object, TestSellerId);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.True(model.HasError);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("error occurred", model.ErrorMessage.ToLowerInvariant());
    }

    [Fact]
    public async Task OnGetAsync_WithNoUserIdentifier_SetsHasErrorToTrue()
    {
        // Arrange
        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        var model = CreateModel(mockKycService.Object, sellerId: null);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.True(model.HasError);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("identify", model.ErrorMessage.ToLowerInvariant());
    }

    [Fact]
    public async Task OnGetAsync_WithEmptyUserIdentifier_SetsHasErrorToTrue()
    {
        // Arrange
        var mockKycService = new Mock<IKycService>(MockBehavior.Strict);
        var model = CreateModel(mockKycService.Object, sellerId: string.Empty);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.True(model.HasError);
        Assert.NotNull(model.ErrorMessage);
    }

    private static IndexModel CreateModel(IKycService kycService, string? sellerId)
    {
        var mockLogger = new Mock<ILogger<IndexModel>>();
        var mockOnboardingService = new Mock<ISellerOnboardingService>(MockBehavior.Strict);
        var mockPayoutSettingsService = new Mock<IPayoutSettingsService>(MockBehavior.Strict);
        
        // Setup mock to return null (no onboarding) by default for existing tests
        if (!string.IsNullOrEmpty(sellerId))
        {
            mockOnboardingService.Setup(s => s.GetOnboardingAsync(sellerId))
                .ReturnsAsync(null as SellerOnboarding);
        }
        
        var model = new IndexModel(kycService, mockOnboardingService.Object, mockPayoutSettingsService.Object, mockLogger.Object);

        // Set up HttpContext with user claims
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(sellerId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, sellerId));
        }
        claims.Add(new Claim(ClaimTypes.Role, "Seller"));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private static KycSubmission CreateSubmission(
        KycStatus status,
        string? rejectionReason = null,
        DateTimeOffset? submittedAt = null)
    {
        return new KycSubmission
        {
            Id = Guid.NewGuid(),
            SellerId = TestSellerId,
            DocumentType = KycDocumentType.PersonalId,
            DocumentFileName = "test-document.pdf",
            DocumentContentType = "application/pdf",
            DocumentData = [1, 2, 3],
            Status = status,
            RejectionReason = rejectionReason,
            SubmittedAt = submittedAt ?? DateTimeOffset.UtcNow
        };
    }
}
