using System.Text;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for the data processing registry index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IDataProcessingRegistryService _registryService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="registryService">The data processing registry service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IDataProcessingRegistryService registryService,
        ILogger<IndexModel> logger)
    {
        _registryService = registryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of data processing activities.
    /// </summary>
    public IReadOnlyList<DataProcessingActivity> Activities { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to show inactive activities.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the data processing registry index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var result = ShowInactive
            ? await _registryService.GetAllActivitiesAsync()
            : await _registryService.GetActiveActivitiesAsync();

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Activities = result.Activities;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to export the registry to CSV.
    /// </summary>
    public async Task<IActionResult> OnPostExportAsync()
    {
        var result = await _registryService.ExportToCsvAsync();

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        _logger.LogInformation("Data processing registry exported to CSV");

        var bytes = Encoding.UTF8.GetBytes(result.CsvContent);
        return File(bytes, "text/csv", result.FileName);
    }
}
