using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for admin case management operations.
/// </summary>
public class AdminCaseService : IAdminCaseService
{
    /// <summary>
    /// Minimum buyer ID length for partial display.
    /// </summary>
    private const int MinBuyerIdLengthForPartialDisplay = 8;

    /// <summary>
    /// Number of characters to show at start and end of buyer ID.
    /// </summary>
    private const int BuyerAliasDisplayChars = 4;

    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly ICaseStatusHistoryRepository _caseStatusHistoryRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ILogger<AdminCaseService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminCaseService"/> class.
    /// </summary>
    /// <param name="returnRequestRepository">The return request repository.</param>
    /// <param name="caseStatusHistoryRepository">The case status history repository.</param>
    /// <param name="adminAuditRepository">The admin audit repository.</param>
    /// <param name="logger">The logger.</param>
    public AdminCaseService(
        IReturnRequestRepository returnRequestRepository,
        ICaseStatusHistoryRepository caseStatusHistoryRepository,
        IAdminAuditRepository adminAuditRepository,
        ILogger<AdminCaseService> logger)
    {
        _returnRequestRepository = returnRequestRepository ?? throw new ArgumentNullException(nameof(returnRequestRepository));
        _caseStatusHistoryRepository = caseStatusHistoryRepository ?? throw new ArgumentNullException(nameof(caseStatusHistoryRepository));
        _adminAuditRepository = adminAuditRepository ?? throw new ArgumentNullException(nameof(adminAuditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GetAdminCasesResult> GetCasesAsync(AdminCaseFilterQuery query)
    {
        try
        {
            var (returnRequests, totalCount) = await _returnRequestRepository.GetAllFilteredAsync(
                query.SearchTerm,
                query.Statuses.Count > 0 ? query.Statuses : null,
                query.CaseTypes.Count > 0 ? query.CaseTypes : null,
                query.FromDate,
                query.ToDate,
                query.Page,
                query.PageSize);

            var caseSummaries = returnRequests.Select(r => new AdminCaseSummary
            {
                Id = r.Id,
                CaseNumber = r.CaseNumber,
                CaseType = r.CaseType,
                Status = r.Status,
                StoreName = r.SellerSubOrder?.StoreName ?? "Unknown",
                StoreId = r.SellerSubOrder?.StoreId ?? Guid.Empty,
                BuyerAlias = GenerateBuyerAlias(r.BuyerId),
                CreatedAt = r.CreatedAt,
                LastUpdatedAt = r.LastUpdatedAt,
                IsEscalated = r.Status == ReturnStatus.UnderAdminReview || r.EscalatedAt.HasValue,
                HasNewActivity = r.HasNewActivity
            }).ToList();

            return GetAdminCasesResult.Success(caseSummaries, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin cases");
            return GetAdminCasesResult.Failure("An error occurred while retrieving cases.");
        }
    }

    /// <inheritdoc />
    public async Task<GetAdminCaseDetailsResult> GetCaseDetailsAsync(Guid caseId)
    {
        try
        {
            var returnRequest = await _returnRequestRepository.GetByIdWithHistoryAsync(caseId);
            if (returnRequest == null)
            {
                return GetAdminCaseDetailsResult.Failure("Case not found.");
            }

            var caseDetails = new AdminCaseDetails
            {
                ReturnRequest = returnRequest,
                StatusHistory = returnRequest.StatusHistory?.ToList() ?? [],
                Messages = returnRequest.Messages?.ToList() ?? [],
                RefundInfo = returnRequest.LinkedRefundId.HasValue
                    ? new Application.Queries.CaseRefundInfo
                    {
                        RefundId = returnRequest.LinkedRefundId.Value,
                        Amount = returnRequest.RefundAmount ?? 0,
                        Status = "Pending Retrieval", // Actual status retrieved separately via Payments module
                        CreatedAt = returnRequest.ResolvedAt ?? DateTimeOffset.UtcNow
                    }
                    : null
            };

            return GetAdminCaseDetailsResult.Success(caseDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving case details for case {CaseId}", caseId);
            return GetAdminCaseDetailsResult.Failure("An error occurred while retrieving case details.");
        }
    }

    /// <inheritdoc />
    public async Task<EscalateCaseResult> EscalateCaseAsync(EscalateCaseCommand command)
    {
        try
        {
            // Validate command
            var validationErrors = ValidateEscalateCaseCommand(command);
            if (validationErrors.Count > 0)
            {
                return EscalateCaseResult.Failure(validationErrors);
            }

            var returnRequest = await _returnRequestRepository.GetByIdAsync(command.CaseId);
            if (returnRequest == null)
            {
                return EscalateCaseResult.Failure("Case not found.");
            }

            // Check if already under admin review
            if (returnRequest.Status == ReturnStatus.UnderAdminReview)
            {
                return EscalateCaseResult.Failure("Case is already under admin review.");
            }

            // Check if case can be escalated (not already completed)
            if (returnRequest.Status == ReturnStatus.Completed)
            {
                return EscalateCaseResult.Failure("Cannot escalate a completed case.");
            }

            var oldStatus = returnRequest.Status;

            // Update return request
            returnRequest.Status = ReturnStatus.UnderAdminReview;
            returnRequest.EscalatedAt = DateTimeOffset.UtcNow;
            returnRequest.EscalatedByUserId = command.EscalatedByUserId;
            returnRequest.EscalationReason = command.EscalationReason;
            returnRequest.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _returnRequestRepository.UpdateAsync(returnRequest);

            // Add status history entry
            var statusHistory = new CaseStatusHistory
            {
                Id = Guid.NewGuid(),
                ReturnRequestId = returnRequest.Id,
                OldStatus = oldStatus,
                NewStatus = ReturnStatus.UnderAdminReview,
                ChangedByUserId = command.EscalatedByUserId,
                ChangedByRole = "Admin",
                Notes = $"Escalated to admin review. Reason: {command.EscalationReason}",
                ChangedAt = DateTimeOffset.UtcNow
            };

            await _caseStatusHistoryRepository.AddAsync(statusHistory);

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.EscalatedByUserId,
                Action = "EscalateCase",
                EntityType = "ReturnRequest",
                EntityId = returnRequest.Id.ToString(),
                Details = $"Escalated case {returnRequest.CaseNumber} to admin review. Reason: {command.EscalationReason}",
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Case {CaseNumber} escalated to admin review by user {UserId}",
                returnRequest.CaseNumber, command.EscalatedByUserId);

            return EscalateCaseResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating case {CaseId}", command.CaseId);
            return EscalateCaseResult.Failure("An error occurred while escalating the case.");
        }
    }

    /// <inheritdoc />
    public async Task<RecordAdminDecisionResult> RecordAdminDecisionAsync(RecordAdminDecisionCommand command)
    {
        try
        {
            // Validate command
            var validationErrors = ValidateRecordAdminDecisionCommand(command);
            if (validationErrors.Count > 0)
            {
                return RecordAdminDecisionResult.Failure(validationErrors);
            }

            var returnRequest = await _returnRequestRepository.GetByIdAsync(command.CaseId);
            if (returnRequest == null)
            {
                return RecordAdminDecisionResult.Failure("Case not found.");
            }

            // Check if case is under admin review
            if (returnRequest.Status != ReturnStatus.UnderAdminReview)
            {
                return RecordAdminDecisionResult.Failure("Case must be under admin review to record a decision.");
            }

            var oldStatus = returnRequest.Status;
            var newStatus = command.NewStatus ?? DetermineNewStatus(command.DecisionType!.Value);

            // Update return request with admin decision
            returnRequest.AdminDecision = command.DecisionType!.Value.ToString();
            returnRequest.AdminDecisionReason = command.DecisionReason;
            returnRequest.AdminDecisionAt = DateTimeOffset.UtcNow;
            returnRequest.AdminDecisionByUserId = command.AdminUserId;
            returnRequest.Status = newStatus;
            returnRequest.LastUpdatedAt = DateTimeOffset.UtcNow;

            // Handle refund if applicable
            if (command.DecisionType == AdminDecisionType.EnforceRefund && command.RefundAmount.HasValue)
            {
                returnRequest.RefundAmount = command.RefundAmount.Value;
                // TODO: Integrate with Payments module to create refund
            }

            if (newStatus == ReturnStatus.Completed)
            {
                returnRequest.ResolvedAt = DateTimeOffset.UtcNow;
            }

            await _returnRequestRepository.UpdateAsync(returnRequest);

            // Add status history entry
            var statusHistory = new CaseStatusHistory
            {
                Id = Guid.NewGuid(),
                ReturnRequestId = returnRequest.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = command.AdminUserId,
                ChangedByRole = "Admin",
                Notes = $"Admin decision: {command.DecisionType}. Reason: {command.DecisionReason}",
                ChangedAt = DateTimeOffset.UtcNow
            };

            await _caseStatusHistoryRepository.AddAsync(statusHistory);

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "RecordAdminDecision",
                EntityType = "ReturnRequest",
                EntityId = returnRequest.Id.ToString(),
                Details = $"Admin decision on case {returnRequest.CaseNumber}: {command.DecisionType}. Reason: {command.DecisionReason}",
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Admin decision recorded on case {CaseNumber} by user {UserId}: {Decision}",
                returnRequest.CaseNumber, command.AdminUserId, command.DecisionType);

            return RecordAdminDecisionResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording admin decision on case {CaseId}", command.CaseId);
            return RecordAdminDecisionResult.Failure("An error occurred while recording the admin decision.");
        }
    }

    private static List<string> ValidateEscalateCaseCommand(EscalateCaseCommand command)
    {
        var errors = new List<string>();

        if (command.CaseId == Guid.Empty)
        {
            errors.Add("Case ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.EscalatedByUserId))
        {
            errors.Add("Escalated by user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.EscalationReason))
        {
            errors.Add("Escalation reason is required.");
        }

        return errors;
    }

    private static List<string> ValidateRecordAdminDecisionCommand(RecordAdminDecisionCommand command)
    {
        var errors = new List<string>();

        if (command.CaseId == Guid.Empty)
        {
            errors.Add("Case ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (!command.DecisionType.HasValue)
        {
            errors.Add("Decision is required.");
        }

        if (command.DecisionType == AdminDecisionType.EnforceRefund && (!command.RefundAmount.HasValue || command.RefundAmount.Value <= 0))
        {
            errors.Add("A valid refund amount is required when enforcing a refund.");
        }

        return errors;
    }

    private static ReturnStatus DetermineNewStatus(AdminDecisionType decision)
    {
        return decision switch
        {
            AdminDecisionType.OverrideSellerDecision => ReturnStatus.Approved,
            AdminDecisionType.EnforceRefund => ReturnStatus.Completed,
            AdminDecisionType.CloseWithoutAction => ReturnStatus.Completed,
            AdminDecisionType.ApproveReturn => ReturnStatus.Approved,
            AdminDecisionType.RejectReturn => ReturnStatus.Rejected,
            _ => ReturnStatus.Completed
        };
    }

    private static string GenerateBuyerAlias(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return "Unknown";
        }

        // Create a privacy-preserving alias using partial display
        if (buyerId.Length <= MinBuyerIdLengthForPartialDisplay)
        {
            return $"Buyer-{buyerId[..Math.Min(BuyerAliasDisplayChars, buyerId.Length)]}***";
        }

        return $"Buyer-{buyerId[..BuyerAliasDisplayChars]}***{buyerId[^BuyerAliasDisplayChars..]}";
    }
}
