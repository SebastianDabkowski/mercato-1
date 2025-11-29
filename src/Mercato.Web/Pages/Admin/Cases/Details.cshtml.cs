using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Cases;

/// <summary>
/// Page model for viewing and managing individual case details.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IAdminCaseService _adminCaseService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="adminCaseService">The admin case service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IAdminCaseService adminCaseService,
        ILogger<DetailsModel> logger)
    {
        _adminCaseService = adminCaseService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the case details.
    /// </summary>
    public AdminCaseDetails? CaseDetails { get; private set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the escalation reason.
    /// </summary>
    [BindProperty]
    public string? EscalationReason { get; set; }

    /// <summary>
    /// Gets or sets the admin decision type.
    /// </summary>
    [BindProperty]
    public AdminDecisionType? AdminDecision { get; set; }

    /// <summary>
    /// Gets or sets the admin decision reason.
    /// </summary>
    [BindProperty]
    public string? AdminDecisionReason { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for EnforceRefund decision.
    /// </summary>
    [BindProperty]
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Handles GET requests to load case details.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _adminCaseService.GetCaseDetailsAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        CaseDetails = result.CaseDetails;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to escalate a case.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostEscalateAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(EscalationReason))
        {
            ErrorMessage = "Escalation reason is required.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to escalate case {CaseId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new EscalateCaseCommand
        {
            CaseId = id,
            EscalatedByUserId = userId,
            EscalationReason = EscalationReason
        };

        var result = await _adminCaseService.EscalateCaseAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Case has been escalated to admin review.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Handles POST requests to record an admin decision.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostDecisionAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (!AdminDecision.HasValue)
        {
            ErrorMessage = "Decision is required.";
            return await OnGetAsync(id);
        }

        if (string.IsNullOrWhiteSpace(AdminDecisionReason))
        {
            ErrorMessage = "Decision reason is required.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to record decision on case {CaseId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new RecordAdminDecisionCommand
        {
            CaseId = id,
            AdminUserId = userId,
            DecisionType = AdminDecision,
            DecisionReason = AdminDecisionReason,
            RefundAmount = RefundAmount
        };

        var result = await _adminCaseService.RecordAdminDecisionAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Admin decision has been recorded successfully.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Gets the CSS class for a case status badge.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "bg-warning text-dark",
        ReturnStatus.UnderReview => "bg-info",
        ReturnStatus.Approved => "bg-success",
        ReturnStatus.Rejected => "bg-danger",
        ReturnStatus.Completed => "bg-secondary",
        ReturnStatus.UnderAdminReview => "bg-primary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a return status.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Requested",
        ReturnStatus.UnderReview => "Under Review",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Completed",
        ReturnStatus.UnderAdminReview => "Under Admin Review",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the display text for a case type.
    /// </summary>
    /// <param name="caseType">The case type.</param>
    /// <returns>The display text.</returns>
    public static string GetCaseTypeDisplayText(CaseType caseType) => caseType switch
    {
        CaseType.Return => "Return",
        CaseType.Complaint => "Complaint",
        _ => caseType.ToString()
    };

    /// <summary>
    /// Gets the CSS class for a message sender role.
    /// </summary>
    /// <param name="role">The sender role.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetMessageRoleClass(string role) => role.ToLower() switch
    {
        "buyer" => "bg-light",
        "seller" => "bg-info bg-opacity-25",
        "admin" => "bg-warning bg-opacity-25",
        _ => "bg-secondary bg-opacity-10"
    };
}
