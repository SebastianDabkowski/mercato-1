using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for GDPR data export functionality.
/// Allows users to download a copy of their personal data.
/// </summary>
[Authorize]
public class DataExportModel : PageModel
{
    private readonly IUserDataExportService _userDataExportService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ILogger<DataExportModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataExportModel"/> class.
    /// </summary>
    /// <param name="userDataExportService">The user data export service.</param>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="adminAuditRepository">The admin audit repository for logging.</param>
    /// <param name="logger">The logger.</param>
    public DataExportModel(
        IUserDataExportService userDataExportService,
        UserManager<IdentityUser> userManager,
        IAdminAuditRepository adminAuditRepository,
        ILogger<DataExportModel> logger)
    {
        _userDataExportService = userDataExportService;
        _userManager = userManager;
        _adminAuditRepository = adminAuditRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets a status message to display.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status message is an error.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Handles the GET request for the data export page.
    /// </summary>
    public IActionResult OnGet()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Check for status message from TempData
        if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
            IsError = TempData["IsError"] as bool? ?? false;
        }

        return Page();
    }

    /// <summary>
    /// Handles the POST request to export user data.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var userEmail = User.Identity?.Name ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation("User {UserId} requested data export", userId);

        var result = await _userDataExportService.ExportUserDataAsync(userId);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Data export failed for user {UserId}: {Errors}", 
                userId, string.Join(", ", result.Errors));

            TempData["StatusMessage"] = "An error occurred while generating your data export. Please try again.";
            TempData["IsError"] = true;
            return RedirectToPage();
        }

        // Log the data export request for audit
        await _adminAuditRepository.AddAsync(new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = userId, // Self-service export
            Action = "DataExport",
            EntityType = "User",
            EntityId = userId,
            Details = $"GDPR data export requested and completed for user {userEmail}",
            Timestamp = result.ExportedAt ?? DateTimeOffset.UtcNow,
            IpAddress = ipAddress
        });

        _logger.LogInformation("Data export completed successfully for user {UserId}", userId);

        // Return the JSON file as a download
        var fileName = $"mercato-data-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var contentType = "application/json";
        var fileContent = System.Text.Encoding.UTF8.GetBytes(result.ExportData ?? "{}");

        return File(fileContent, contentType, fileName);
    }
}
