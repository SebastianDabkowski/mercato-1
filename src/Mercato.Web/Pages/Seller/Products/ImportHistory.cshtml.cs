using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Products;

/// <summary>
/// Page model for viewing product import history.
/// </summary>
[Authorize(Roles = "Seller")]
public class ImportHistoryModel : PageModel
{
    private readonly IProductImportService _importService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ImportHistoryModel> _logger;

    public ImportHistoryModel(
        IProductImportService importService,
        IStoreProfileService storeProfileService,
        ILogger<ImportHistoryModel> logger)
    {
        _importService = importService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether there was an error loading the page.
    /// </summary>
    public bool HasError { get; private set; }

    /// <summary>
    /// Gets the error message if an error occurred.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the seller has a store configured.
    /// </summary>
    public bool HasStore { get; private set; }

    /// <summary>
    /// Gets the list of import jobs for the seller's store.
    /// </summary>
    public IReadOnlyList<ProductImportJob> ImportJobs { get; private set; } = [];

    /// <summary>
    /// Gets the currently selected job for viewing details.
    /// </summary>
    public ProductImportJob? SelectedJob { get; private set; }

    /// <summary>
    /// Gets the row errors for the selected job.
    /// </summary>
    public IReadOnlyList<ProductImportRowError> SelectedJobErrors { get; private set; } = [];

    /// <summary>
    /// Gets or sets the job ID to view details for.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? JobId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();

        if (JobId.HasValue && HasStore && !HasError)
        {
            await LoadJobDetailsAsync(JobId.Value);
        }
    }

    private async Task LoadDataAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            HasError = true;
            ErrorMessage = "Unable to identify the current user.";
            return;
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            HasStore = store != null;

            if (store == null)
            {
                ErrorMessage = "You must configure your store profile before viewing import history.";
                return;
            }

            ImportJobs = await _importService.GetImportJobsByStoreIdAsync(store.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading import history for seller {SellerId}", sellerId);
            HasError = true;
            ErrorMessage = "An error occurred while loading your import history. Please try again.";
        }
    }

    private async Task LoadJobDetailsAsync(Guid jobId)
    {
        try
        {
            SelectedJob = await _importService.GetImportJobByIdAsync(jobId);

            if (SelectedJob != null)
            {
                SelectedJobErrors = await _importService.GetImportJobErrorsAsync(jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading job details for job {JobId}", jobId);
            // Don't fail the whole page, just don't show job details
        }
    }

    /// <summary>
    /// Gets the CSS class for a job status badge.
    /// </summary>
    public static string GetStatusBadgeClass(ProductImportStatus status)
    {
        return status switch
        {
            ProductImportStatus.Pending => "bg-secondary",
            ProductImportStatus.Validating => "bg-info",
            ProductImportStatus.ValidationFailed => "bg-warning text-dark",
            ProductImportStatus.AwaitingConfirmation => "bg-primary",
            ProductImportStatus.Processing => "bg-info",
            ProductImportStatus.Completed => "bg-success",
            ProductImportStatus.CompletedWithErrors => "bg-warning text-dark",
            ProductImportStatus.Failed => "bg-danger",
            ProductImportStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Gets the display name for a job status.
    /// </summary>
    public static string GetStatusDisplayName(ProductImportStatus status)
    {
        return status switch
        {
            ProductImportStatus.Pending => "Pending",
            ProductImportStatus.Validating => "Validating",
            ProductImportStatus.ValidationFailed => "Validation Failed",
            ProductImportStatus.AwaitingConfirmation => "Awaiting Confirmation",
            ProductImportStatus.Processing => "Processing",
            ProductImportStatus.Completed => "Completed",
            ProductImportStatus.CompletedWithErrors => "Completed with Errors",
            ProductImportStatus.Failed => "Failed",
            ProductImportStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }
}
