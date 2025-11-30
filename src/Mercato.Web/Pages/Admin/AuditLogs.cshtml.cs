using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin;

/// <summary>
/// Page model for the audit logs page displaying admin action history.
/// </summary>
[Authorize(Roles = "Admin")]
public class AuditLogsModel : PageModel
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogsModel"/> class.
    /// </summary>
    /// <param name="auditLogService">The audit log service.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogsModel(
        IAuditLogService auditLogService,
        ILogger<AuditLogsModel> logger)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the list of audit log entries.
    /// </summary>
    public IReadOnlyList<AdminAuditLog> AuditLogs { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the entity type filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the action type filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ActionType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? EntityId { get; set; }

    /// <summary>
    /// Handles GET requests to load the audit logs page.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation(
            "Admin accessing audit logs with filters: StartDate={StartDate}, EndDate={EndDate}, AdminUserId={AdminUserId}, EntityType={EntityType}, ActionType={ActionType}, EntityId={EntityId}",
            StartDate,
            EndDate,
            AdminUserId,
            EntityType,
            ActionType,
            EntityId);

        AuditLogs = await _auditLogService.GetAuditLogsAsync(
            StartDate,
            EndDate,
            AdminUserId,
            EntityType,
            ActionType,
            EntityId);
    }
}
