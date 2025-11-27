using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for access denied errors.
/// Displayed when a user tries to access a resource they don't have permission for.
/// </summary>
public class AccessDeniedModel : PageModel
{
    private readonly ILogger<AccessDeniedModel> _logger;

    public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogWarning(
            "Access denied for user {UserId} attempting to access {Path}",
            User.Identity?.Name ?? "Anonymous",
            HttpContext.Request.Path);
    }
}
