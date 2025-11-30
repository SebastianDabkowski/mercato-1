using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Queries;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Roles;

/// <summary>
/// Page model for managing permissions for a specific role.
/// </summary>
[Authorize(Roles = "Admin")]
public class PermissionsModel : PageModel
{
    private readonly IRbacConfigurationService _rbacService;
    private readonly ILogger<PermissionsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionsModel"/> class.
    /// </summary>
    /// <param name="rbacService">The RBAC configuration service.</param>
    /// <param name="logger">The logger.</param>
    public PermissionsModel(
        IRbacConfigurationService rbacService,
        ILogger<PermissionsModel> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role configuration.
    /// </summary>
    public RolePermissionConfiguration? RoleConfiguration { get; set; }

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
    /// Gets permissions grouped by module.
    /// </summary>
    public IEnumerable<IGrouping<string, PermissionInfo>> PermissionsByModule =>
        RoleConfiguration?.Permissions.GroupBy(p => p.Module).OrderBy(g => g.Key) 
        ?? Enumerable.Empty<IGrouping<string, PermissionInfo>>();

    /// <summary>
    /// Handles GET requests to load role permissions.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(RoleName))
        {
            return RedirectToPage("Index");
        }

        _logger.LogInformation("Admin accessing permissions for role {RoleName}", RoleName);

        RoleConfiguration = await _rbacService.GetRoleConfigurationAsync(RoleName);
        
        if (RoleConfiguration == null)
        {
            TempData["ErrorMessage"] = $"Role '{RoleName}' not found.";
            return RedirectToPage("Index");
        }

        Modules = await _rbacService.GetModulesAsync();
        SuccessMessage = TempData["SuccessMessage"]?.ToString();
        ErrorMessage = TempData["ErrorMessage"]?.ToString();

        return Page();
    }

    /// <summary>
    /// Handles POST requests to assign a permission to the role.
    /// </summary>
    /// <param name="permissionId">The permission ID to assign.</param>
    public async Task<IActionResult> OnPostAssignAsync(string permissionId)
    {
        if (string.IsNullOrEmpty(RoleName))
        {
            return RedirectToPage("Index");
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            TempData["ErrorMessage"] = "Could not verify your identity. Please log in again.";
            return RedirectToPage(new { roleName = RoleName });
        }

        _logger.LogInformation(
            "Admin {AdminId} assigning permission {PermissionId} to role {RoleName}",
            adminUserId,
            permissionId,
            RoleName);

        var command = new AssignPermissionCommand
        {
            RoleName = RoleName,
            PermissionId = permissionId,
            AdminUserId = adminUserId
        };

        var result = await _rbacService.AssignPermissionAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Permission assigned successfully to {RoleName}.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage(new { roleName = RoleName });
    }

    /// <summary>
    /// Handles POST requests to revoke a permission from the role.
    /// </summary>
    /// <param name="permissionId">The permission ID to revoke.</param>
    public async Task<IActionResult> OnPostRevokeAsync(string permissionId)
    {
        if (string.IsNullOrEmpty(RoleName))
        {
            return RedirectToPage("Index");
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            TempData["ErrorMessage"] = "Could not verify your identity. Please log in again.";
            return RedirectToPage(new { roleName = RoleName });
        }

        _logger.LogInformation(
            "Admin {AdminId} revoking permission {PermissionId} from role {RoleName}",
            adminUserId,
            permissionId,
            RoleName);

        var command = new RevokePermissionCommand
        {
            RoleName = RoleName,
            PermissionId = permissionId,
            AdminUserId = adminUserId
        };

        var result = await _rbacService.RevokePermissionAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Permission revoked successfully from {RoleName}.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage(new { roleName = RoleName });
    }
}
