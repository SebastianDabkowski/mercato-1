using Mercato.Identity.Application.Queries;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Roles;

/// <summary>
/// Page model for viewing RBAC (Role-Based Access Control) configuration.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IRbacConfigurationService _rbacService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="rbacService">The RBAC configuration service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IRbacConfigurationService rbacService,
        ILogger<IndexModel> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of role-permission configurations.
    /// </summary>
    public IReadOnlyList<RolePermissionConfiguration> RoleConfigurations { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of available modules.
    /// </summary>
    public IReadOnlyList<string> Modules { get; set; } = [];

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load RBAC configuration.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing RBAC configuration");

        RoleConfigurations = await _rbacService.GetRbacConfigurationAsync();
        Modules = await _rbacService.GetModulesAsync();

        SuccessMessage = TempData["SuccessMessage"]?.ToString();
        ErrorMessage = TempData["ErrorMessage"]?.ToString();
    }
}
