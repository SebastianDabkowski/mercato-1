using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Buyer;

/// <summary>
/// Page model for managing privacy settings and consents.
/// </summary>
[Authorize(Roles = "Buyer")]
public class PrivacySettingsModel : PageModel
{
    private readonly IConsentService _consentService;
    private readonly ILogger<PrivacySettingsModel> _logger;

    public PrivacySettingsModel(
        IConsentService consentService,
        ILogger<PrivacySettingsModel> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the user's current consents.
    /// </summary>
    public List<UserConsentDto> Consents { get; set; } = [];

    /// <summary>
    /// Gets or sets the consent updates to apply.
    /// </summary>
    [BindProperty]
    public List<ConsentUpdateInput> ConsentUpdates { get; set; } = [];

    /// <summary>
    /// Gets or sets a message to display to the user.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        await LoadConsentsAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        if (ConsentUpdates.Count > 0)
        {
            var command = new RecordMultipleConsentsCommand
            {
                UserId = userId,
                Consents = ConsentUpdates.Select(u => new ConsentDecision
                {
                    ConsentTypeCode = u.ConsentTypeCode,
                    IsGranted = u.IsGranted
                }).ToList(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            var result = await _consentService.RecordMultipleConsentsAsync(command);

            if (result.Succeeded && result.ConsentsRecorded > 0)
            {
                _logger.LogInformation(
                    "Updated {Count} consents for user {UserId}",
                    result.ConsentsRecorded, userId);
                StatusMessage = "Your privacy preferences have been updated successfully.";
            }
            else
            {
                StatusMessage = "No changes were made to your preferences.";
            }
        }
        else
        {
            StatusMessage = "No changes were made to your preferences.";
        }

        await LoadConsentsAsync(userId);
        return Page();
    }

    private async Task LoadConsentsAsync(string userId)
    {
        var result = await _consentService.GetUserConsentsAsync(new GetUserConsentsQuery { UserId = userId });
        if (result.Succeeded)
        {
            Consents = result.Consents.ToList();
            
            // Initialize consent updates based on current state
            ConsentUpdates = Consents.Select(c => new ConsentUpdateInput
            {
                ConsentTypeCode = c.ConsentTypeCode,
                CurrentVersionId = c.CurrentVersionId,
                IsGranted = c.IsGranted
            }).ToList();
        }
    }
}

/// <summary>
/// Input model for consent updates.
/// </summary>
public class ConsentUpdateInput
{
    /// <summary>
    /// Gets or sets the consent type code.
    /// </summary>
    public string ConsentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current consent version ID.
    /// </summary>
    public Guid CurrentVersionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent is granted.
    /// </summary>
    public bool IsGranted { get; set; }
}
