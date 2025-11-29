using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

/// <summary>
/// Unit tests for the AdminCaseService.
/// </summary>
public class AdminCaseServiceTests
{
    private readonly Mock<IReturnRequestRepository> _mockReturnRequestRepository;
    private readonly Mock<ICaseStatusHistoryRepository> _mockCaseStatusHistoryRepository;
    private readonly Mock<IAdminAuditRepository> _mockAdminAuditRepository;
    private readonly Mock<ILogger<AdminCaseService>> _mockLogger;
    private readonly AdminCaseService _service;

    public AdminCaseServiceTests()
    {
        _mockReturnRequestRepository = new Mock<IReturnRequestRepository>(MockBehavior.Strict);
        _mockCaseStatusHistoryRepository = new Mock<ICaseStatusHistoryRepository>(MockBehavior.Strict);
        _mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<AdminCaseService>>();

        _service = new AdminCaseService(
            _mockReturnRequestRepository.Object,
            _mockCaseStatusHistoryRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullReturnRequestRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AdminCaseService(
            null!,
            _mockCaseStatusHistoryRepository.Object,
            _mockAdminAuditRepository.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullCaseStatusHistoryRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AdminCaseService(
            _mockReturnRequestRepository.Object,
            null!,
            _mockAdminAuditRepository.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAdminAuditRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AdminCaseService(
            _mockReturnRequestRepository.Object,
            _mockCaseStatusHistoryRepository.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AdminCaseService(
            _mockReturnRequestRepository.Object,
            _mockCaseStatusHistoryRepository.Object,
            _mockAdminAuditRepository.Object,
            null!));
    }

    #endregion

    #region GetCasesAsync Tests

    [Fact]
    public async Task GetCasesAsync_ReturnsSuccessWithCases()
    {
        // Arrange
        var returnRequests = new List<ReturnRequest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CaseNumber = "CASE-001",
                CaseType = CaseType.Return,
                Status = ReturnStatus.Requested,
                BuyerId = "buyer-123",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                LastUpdatedAt = DateTimeOffset.UtcNow,
                SellerSubOrder = new SellerSubOrder
                {
                    StoreName = "Test Store",
                    StoreId = Guid.NewGuid()
                }
            }
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetAllFilteredAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<ReturnStatus>?>(),
                It.IsAny<IReadOnlyList<CaseType>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((returnRequests, 1));

        var query = new AdminCaseFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetCasesAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Cases);
        Assert.Equal("CASE-001", result.Cases[0].CaseNumber);
        Assert.Equal("Test Store", result.Cases[0].StoreName);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetCasesAsync_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var statuses = new List<ReturnStatus> { ReturnStatus.Requested, ReturnStatus.UnderReview };
        var caseTypes = new List<CaseType> { CaseType.Return };

        _mockReturnRequestRepository
            .Setup(r => r.GetAllFilteredAsync(
                "test",
                statuses,
                caseTypes,
                fromDate,
                toDate,
                2,
                10))
            .ReturnsAsync((new List<ReturnRequest>(), 0));

        var query = new AdminCaseFilterQuery
        {
            SearchTerm = "test",
            Statuses = statuses,
            CaseTypes = caseTypes,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _service.GetCasesAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockReturnRequestRepository.Verify(r => r.GetAllFilteredAsync(
            "test", statuses, caseTypes, fromDate, toDate, 2, 10), Times.Once);
    }

    #endregion

    #region GetCaseDetailsAsync Tests

    [Fact]
    public async Task GetCaseDetailsAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            CaseType = CaseType.Return,
            Status = ReturnStatus.Requested,
            BuyerId = "buyer-123",
            CreatedAt = DateTimeOffset.UtcNow,
            StatusHistory = new List<CaseStatusHistory>(),
            Messages = new List<CaseMessage>()
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdWithHistoryAsync(caseId))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetCaseDetailsAsync(caseId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.CaseDetails);
        Assert.Equal(caseId, result.CaseDetails.ReturnRequest.Id);
    }

    [Fact]
    public async Task GetCaseDetailsAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var caseId = Guid.NewGuid();

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdWithHistoryAsync(caseId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.GetCaseDetailsAsync(caseId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case not found.", result.Errors);
    }

    #endregion

    #region EscalateCaseAsync Tests

    [Fact]
    public async Task EscalateCaseAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.Requested
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        _mockReturnRequestRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        _mockCaseStatusHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CaseStatusHistory>()))
            .ReturnsAsync((CaseStatusHistory h) => h);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new EscalateCaseCommand
        {
            CaseId = caseId,
            EscalatedByUserId = "admin-user",
            EscalationReason = "Buyer requested escalation"
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ReturnStatus.UnderAdminReview, returnRequest.Status);
        Assert.NotNull(returnRequest.EscalatedAt);
        Assert.Equal("admin-user", returnRequest.EscalatedByUserId);
        _mockCaseStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<CaseStatusHistory>()), Times.Once);
        _mockAdminAuditRepository.Verify(r => r.AddAsync(It.IsAny<AdminAuditLog>()), Times.Once);
    }

    [Fact]
    public async Task EscalateCaseAsync_WithEmptyCaseId_ReturnsValidationError()
    {
        // Arrange
        var command = new EscalateCaseCommand
        {
            CaseId = Guid.Empty,
            EscalatedByUserId = "admin-user",
            EscalationReason = "Test reason"
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case ID is required.", result.Errors);
    }

    [Fact]
    public async Task EscalateCaseAsync_WithEmptyUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new EscalateCaseCommand
        {
            CaseId = Guid.NewGuid(),
            EscalatedByUserId = "",
            EscalationReason = "Test reason"
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Escalated by user ID is required.", result.Errors);
    }

    [Fact]
    public async Task EscalateCaseAsync_WithEmptyReason_ReturnsValidationError()
    {
        // Arrange
        var command = new EscalateCaseCommand
        {
            CaseId = Guid.NewGuid(),
            EscalatedByUserId = "admin-user",
            EscalationReason = ""
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Escalation reason is required.", result.Errors);
    }

    [Fact]
    public async Task EscalateCaseAsync_AlreadyUnderAdminReview_ReturnsError()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.UnderAdminReview
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        var command = new EscalateCaseCommand
        {
            CaseId = caseId,
            EscalatedByUserId = "admin-user",
            EscalationReason = "Test reason"
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case is already under admin review.", result.Errors);
    }

    [Fact]
    public async Task EscalateCaseAsync_CaseCompleted_ReturnsError()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.Completed
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        var command = new EscalateCaseCommand
        {
            CaseId = caseId,
            EscalatedByUserId = "admin-user",
            EscalationReason = "Test reason"
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot escalate a completed case.", result.Errors);
    }

    [Fact]
    public async Task EscalateCaseAsync_CaseNotFound_ReturnsError()
    {
        // Arrange
        var caseId = Guid.NewGuid();

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync((ReturnRequest?)null);

        var command = new EscalateCaseCommand
        {
            CaseId = caseId,
            EscalatedByUserId = "admin-user",
            EscalationReason = "Test reason"
        };

        // Act
        var result = await _service.EscalateCaseAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case not found.", result.Errors);
    }

    #endregion

    #region RecordAdminDecisionAsync Tests

    [Fact]
    public async Task RecordAdminDecisionAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.UnderAdminReview
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        _mockReturnRequestRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        _mockCaseStatusHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CaseStatusHistory>()))
            .ReturnsAsync((CaseStatusHistory h) => h);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new RecordAdminDecisionCommand
        {
            CaseId = caseId,
            AdminUserId = "admin-user",
            DecisionType = AdminDecisionType.CloseWithoutAction,
            DecisionReason = "No further action needed"
        };

        // Act
        var result = await _service.RecordAdminDecisionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("CloseWithoutAction", returnRequest.AdminDecision);
        Assert.Equal("No further action needed", returnRequest.AdminDecisionReason);
        Assert.NotNull(returnRequest.AdminDecisionAt);
        Assert.Equal(ReturnStatus.Completed, returnRequest.Status);
    }

    [Fact]
    public async Task RecordAdminDecisionAsync_WithEnforceRefund_RequiresRefundAmount()
    {
        // Arrange
        var command = new RecordAdminDecisionCommand
        {
            CaseId = Guid.NewGuid(),
            AdminUserId = "admin-user",
            DecisionType = AdminDecisionType.EnforceRefund,
            DecisionReason = "Refund required"
        };

        // Act
        var result = await _service.RecordAdminDecisionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("A valid refund amount is required when enforcing a refund.", result.Errors);
    }

    [Fact]
    public async Task RecordAdminDecisionAsync_WithInvalidDecision_ReturnsValidationError()
    {
        // Arrange
        var command = new RecordAdminDecisionCommand
        {
            CaseId = Guid.NewGuid(),
            AdminUserId = "admin-user",
            DecisionType = null,
            DecisionReason = "Test"
        };

        // Act
        var result = await _service.RecordAdminDecisionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Invalid decision", result.Errors[0]);
    }

    [Fact]
    public async Task RecordAdminDecisionAsync_CaseNotUnderAdminReview_ReturnsError()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.Requested
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        var command = new RecordAdminDecisionCommand
        {
            CaseId = caseId,
            AdminUserId = "admin-user",
            DecisionType = AdminDecisionType.CloseWithoutAction,
            DecisionReason = "Test"
        };

        // Act
        var result = await _service.RecordAdminDecisionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case must be under admin review to record a decision.", result.Errors);
    }

    [Fact]
    public async Task RecordAdminDecisionAsync_OverrideSellerDecision_SetsApprovedStatus()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.UnderAdminReview
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        _mockReturnRequestRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        _mockCaseStatusHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CaseStatusHistory>()))
            .ReturnsAsync((CaseStatusHistory h) => h);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new RecordAdminDecisionCommand
        {
            CaseId = caseId,
            AdminUserId = "admin-user",
            DecisionType = AdminDecisionType.OverrideSellerDecision,
            DecisionReason = "Seller was wrong"
        };

        // Act
        var result = await _service.RecordAdminDecisionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(ReturnStatus.Approved, returnRequest.Status);
    }

    [Fact]
    public async Task RecordAdminDecisionAsync_EnforceRefund_SetsRefundAmount()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var returnRequest = new ReturnRequest
        {
            Id = caseId,
            CaseNumber = "CASE-001",
            Status = ReturnStatus.UnderAdminReview
        };

        _mockReturnRequestRepository
            .Setup(r => r.GetByIdAsync(caseId))
            .ReturnsAsync(returnRequest);

        _mockReturnRequestRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        _mockCaseStatusHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CaseStatusHistory>()))
            .ReturnsAsync((CaseStatusHistory h) => h);

        _mockAdminAuditRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog a) => a);

        var command = new RecordAdminDecisionCommand
        {
            CaseId = caseId,
            AdminUserId = "admin-user",
            DecisionType = AdminDecisionType.EnforceRefund,
            DecisionReason = "Refund required",
            RefundAmount = 99.99m
        };

        // Act
        var result = await _service.RecordAdminDecisionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(99.99m, returnRequest.RefundAmount);
        Assert.Equal(ReturnStatus.Completed, returnRequest.Status);
    }

    #endregion
}
