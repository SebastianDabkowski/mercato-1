using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Sellers;

/// <summary>
/// Page model for listing all KYC submissions with filtering.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IKycService _kycService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IKycService kycService,
        ILogger<IndexModel> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of KYC submissions.
    /// </summary>
    public IReadOnlyList<KycSubmission> Submissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the current status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load KYC submissions.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing KYC submissions list. Filter: {StatusFilter}", StatusFilter ?? "None");

        if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<KycStatus>(StatusFilter, out var status))
        {
            Submissions = await _kycService.GetSubmissionsByStatusAsync(status);
        }
        else
        {
            Submissions = await _kycService.GetAllSubmissionsAsync();
        }

        SuccessMessage = TempData["SuccessMessage"]?.ToString();
        ErrorMessage = TempData["ErrorMessage"]?.ToString();
    }
}
