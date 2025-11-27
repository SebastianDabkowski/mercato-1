using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Users;

/// <summary>
/// Page model for listing all users with their roles.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IUserRoleManagementService _userRoleManagementService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserRoleManagementService userRoleManagementService,
        ILogger<IndexModel> logger)
    {
        _userRoleManagementService = userRoleManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of users with their roles.
    /// </summary>
    public IReadOnlyList<UserWithRolesInfo> Users { get; set; } = [];

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load all users.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing user list.");
        Users = await _userRoleManagementService.GetAllUsersWithRolesAsync();
        SuccessMessage = TempData["SuccessMessage"]?.ToString();
    }
}
