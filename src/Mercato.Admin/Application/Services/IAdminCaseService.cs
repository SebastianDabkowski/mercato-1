using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for admin case management operations.
/// </summary>
public interface IAdminCaseService
{
    /// <summary>
    /// Gets filtered and paginated cases for admin view.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the filtered cases.</returns>
    Task<GetAdminCasesResult> GetCasesAsync(AdminCaseFilterQuery query);

    /// <summary>
    /// Gets full details of a specific case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <returns>The result containing the case details.</returns>
    Task<GetAdminCaseDetailsResult> GetCaseDetailsAsync(Guid caseId);

    /// <summary>
    /// Escalates a case to admin review status.
    /// </summary>
    /// <param name="command">The escalate command.</param>
    /// <returns>The result of the escalation operation.</returns>
    Task<EscalateCaseResult> EscalateCaseAsync(EscalateCaseCommand command);

    /// <summary>
    /// Records an admin decision on an escalated case.
    /// </summary>
    /// <param name="command">The admin decision command.</param>
    /// <returns>The result of the decision operation.</returns>
    Task<RecordAdminDecisionResult> RecordAdminDecisionAsync(RecordAdminDecisionCommand command);
}
