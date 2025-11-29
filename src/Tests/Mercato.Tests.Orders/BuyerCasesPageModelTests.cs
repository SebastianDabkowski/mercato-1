using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;
using Mercato.Web.Pages.Buyer.Cases;

namespace Mercato.Tests.Orders;

/// <summary>
/// Unit tests for the buyer cases page models.
/// </summary>
public class BuyerCasesPageModelTests
{
    #region IndexModel Tests

    [Fact]
    public void IndexModel_GetStatusBadgeClass_ReturnsCorrectClass()
    {
        // Assert
        Assert.Equal("bg-warning text-dark", IndexModel.GetStatusBadgeClass(ReturnStatus.Requested));
        Assert.Equal("bg-info", IndexModel.GetStatusBadgeClass(ReturnStatus.UnderReview));
        Assert.Equal("bg-success", IndexModel.GetStatusBadgeClass(ReturnStatus.Approved));
        Assert.Equal("bg-danger", IndexModel.GetStatusBadgeClass(ReturnStatus.Rejected));
        Assert.Equal("bg-dark", IndexModel.GetStatusBadgeClass(ReturnStatus.Completed));
    }

    [Fact]
    public void IndexModel_GetStatusDisplayText_ReturnsCorrectText()
    {
        // Assert
        Assert.Equal("Pending Seller Review", IndexModel.GetStatusDisplayText(ReturnStatus.Requested));
        Assert.Equal("In Progress", IndexModel.GetStatusDisplayText(ReturnStatus.UnderReview));
        Assert.Equal("Approved", IndexModel.GetStatusDisplayText(ReturnStatus.Approved));
        Assert.Equal("Rejected", IndexModel.GetStatusDisplayText(ReturnStatus.Rejected));
        Assert.Equal("Resolved", IndexModel.GetStatusDisplayText(ReturnStatus.Completed));
    }

    [Fact]
    public void IndexModel_GetCaseType_ReturnsReturnRequest()
    {
        // Act
        var result = IndexModel.GetCaseType();

        // Assert
        Assert.Equal("Return Request", result);
    }

    [Fact]
    public void IndexModel_AllStatuses_ContainsAllReturnStatuses()
    {
        // Act
        var allStatuses = IndexModel.AllStatuses.ToList();

        // Assert
        Assert.Contains(ReturnStatus.Requested, allStatuses);
        Assert.Contains(ReturnStatus.UnderReview, allStatuses);
        Assert.Contains(ReturnStatus.Approved, allStatuses);
        Assert.Contains(ReturnStatus.Rejected, allStatuses);
        Assert.Contains(ReturnStatus.Completed, allStatuses);
        Assert.Equal(5, allStatuses.Count);
    }

    [Fact]
    public void IndexModel_FormatCaseId_ReturnsFirst8CharsUppercase()
    {
        // Arrange
        var caseId = Guid.Parse("12345678-abcd-1234-abcd-123456789abc");

        // Act
        var result = IndexModel.FormatCaseId(caseId);

        // Assert
        Assert.Equal("12345678", result);
    }

    #endregion

    #region DetailsModel Tests

    [Fact]
    public void DetailsModel_GetStatusBadgeClass_ReturnsCorrectClass()
    {
        // Assert
        Assert.Equal("bg-warning text-dark", DetailsModel.GetStatusBadgeClass(ReturnStatus.Requested));
        Assert.Equal("bg-info", DetailsModel.GetStatusBadgeClass(ReturnStatus.UnderReview));
        Assert.Equal("bg-success", DetailsModel.GetStatusBadgeClass(ReturnStatus.Approved));
        Assert.Equal("bg-danger", DetailsModel.GetStatusBadgeClass(ReturnStatus.Rejected));
        Assert.Equal("bg-dark", DetailsModel.GetStatusBadgeClass(ReturnStatus.Completed));
    }

    [Fact]
    public void DetailsModel_GetStatusDisplayText_ReturnsCorrectText()
    {
        // Assert
        Assert.Equal("Pending Seller Review", DetailsModel.GetStatusDisplayText(ReturnStatus.Requested));
        Assert.Equal("In Progress", DetailsModel.GetStatusDisplayText(ReturnStatus.UnderReview));
        Assert.Equal("Approved", DetailsModel.GetStatusDisplayText(ReturnStatus.Approved));
        Assert.Equal("Rejected", DetailsModel.GetStatusDisplayText(ReturnStatus.Rejected));
        Assert.Equal("Resolved", DetailsModel.GetStatusDisplayText(ReturnStatus.Completed));
    }

    [Fact]
    public void DetailsModel_GetResolutionSummary_ApprovedStatus_ReturnsApprovalMessage()
    {
        // Act
        var result = DetailsModel.GetResolutionSummary(ReturnStatus.Approved);

        // Assert
        Assert.Contains("approved", result.ToLowerInvariant());
    }

    [Fact]
    public void DetailsModel_GetResolutionSummary_RejectedStatus_ReturnsRejectionMessage()
    {
        // Act
        var result = DetailsModel.GetResolutionSummary(ReturnStatus.Rejected);

        // Assert
        Assert.Contains("declined", result.ToLowerInvariant());
    }

    [Fact]
    public void DetailsModel_GetResolutionSummary_CompletedStatus_ReturnsResolvedMessage()
    {
        // Act
        var result = DetailsModel.GetResolutionSummary(ReturnStatus.Completed);

        // Assert
        Assert.Contains("resolved", result.ToLowerInvariant());
    }

    [Fact]
    public void DetailsModel_GetResolutionSummary_PendingStatuses_ReturnsEmptyString()
    {
        // Assert
        Assert.Equal(string.Empty, DetailsModel.GetResolutionSummary(ReturnStatus.Requested));
        Assert.Equal(string.Empty, DetailsModel.GetResolutionSummary(ReturnStatus.UnderReview));
    }

    [Fact]
    public void DetailsModel_GetCaseType_ReturnsReturnRequest()
    {
        // Act
        var result = DetailsModel.GetCaseType();

        // Assert
        Assert.Equal("Return Request", result);
    }

    [Fact]
    public void DetailsModel_GetRefundStatusBadgeClass_ReturnsCorrectClass()
    {
        // Assert
        Assert.Equal("bg-warning text-dark", DetailsModel.GetRefundStatusBadgeClass(RefundStatus.Pending));
        Assert.Equal("bg-info", DetailsModel.GetRefundStatusBadgeClass(RefundStatus.Processing));
        Assert.Equal("bg-success", DetailsModel.GetRefundStatusBadgeClass(RefundStatus.Completed));
        Assert.Equal("bg-danger", DetailsModel.GetRefundStatusBadgeClass(RefundStatus.Failed));
        Assert.Equal("bg-secondary", DetailsModel.GetRefundStatusBadgeClass(RefundStatus.Cancelled));
    }

    [Fact]
    public void DetailsModel_GetRefundStatusDisplayText_ReturnsCorrectText()
    {
        // Assert
        Assert.Equal("Pending", DetailsModel.GetRefundStatusDisplayText(RefundStatus.Pending));
        Assert.Equal("Processing", DetailsModel.GetRefundStatusDisplayText(RefundStatus.Processing));
        Assert.Equal("Completed", DetailsModel.GetRefundStatusDisplayText(RefundStatus.Completed));
        Assert.Equal("Failed", DetailsModel.GetRefundStatusDisplayText(RefundStatus.Failed));
        Assert.Equal("Cancelled", DetailsModel.GetRefundStatusDisplayText(RefundStatus.Cancelled));
    }

    [Fact]
    public void DetailsModel_FormatCaseId_ReturnsFirst8CharsUppercase()
    {
        // Arrange
        var caseId = Guid.Parse("12345678-abcd-1234-abcd-123456789abc");

        // Act
        var result = DetailsModel.FormatCaseId(caseId);

        // Assert
        Assert.Equal("12345678", result);
    }

    [Fact]
    public void DetailsModel_FormatRefundId_ReturnsFirst8CharsUppercase()
    {
        // Arrange
        var refundId = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890");

        // Act
        var result = DetailsModel.FormatRefundId(refundId);

        // Assert
        Assert.Equal("ABCDEF12", result);
    }

    #endregion
}
