using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Users;

/// <summary>
/// Page model for listing all users with filtering, search, and pagination support.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IUserAccountManagementService _userAccountManagementService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserAccountManagementService userAccountManagementService,
        ILogger<IndexModel> logger)
    {
        _userAccountManagementService = userAccountManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the paginated list of users.
    /// </summary>
    public PagedResult<UserAccountInfo> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the role filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? RoleFilter { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public UserAccountStatus? StatusFilter { get; set; }

    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Gets the available roles for filtering.
    /// </summary>
    public static IReadOnlyList<string> AvailableRoles => ["Buyer", "Seller", "Admin"];

    /// <summary>
    /// Gets the available statuses for filtering.
    /// </summary>
    public static IReadOnlyList<UserAccountStatus> AvailableStatuses =>
        [UserAccountStatus.Active, UserAccountStatus.Blocked, UserAccountStatus.PendingVerification];

    /// <summary>
    /// Handles GET requests to load users with filtering and pagination.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation(
            "Admin accessing user list with filters: Role={Role}, Status={Status}, Search={Search}, Page={Page}",
            RoleFilter ?? "All",
            StatusFilter?.ToString() ?? "All",
            SearchTerm ?? "None",
            CurrentPage);

        var query = new UserAccountFilterQuery
        {
            Role = RoleFilter,
            Status = StatusFilter,
            SearchTerm = SearchTerm,
            Page = CurrentPage,
            PageSize = 20
        };

        Users = await _userAccountManagementService.GetUsersAsync(query);
        SuccessMessage = TempData["SuccessMessage"]?.ToString();
    }
}
