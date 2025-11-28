using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Products;

/// <summary>
/// Page model for importing products from CSV/XLS files.
/// </summary>
[Authorize(Roles = "Seller")]
public class ImportModel : PageModel
{
    private readonly IProductImportService _importService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ImportModel> _logger;

    /// <summary>
    /// Maximum file size in bytes (10 MB).
    /// </summary>
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Supported file extensions.
    /// </summary>
    private static readonly string[] SupportedExtensions = [".csv", ".xls", ".xlsx"];

    public ImportModel(
        IProductImportService importService,
        IStoreProfileService storeProfileService,
        ILogger<ImportModel> logger)
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
    /// Gets or sets the uploaded file.
    /// </summary>
    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    /// <summary>
    /// Gets the upload result after file validation.
    /// </summary>
    public UploadProductImportResult? UploadResult { get; private set; }

    /// <summary>
    /// Gets the import job ID for confirmation.
    /// </summary>
    public Guid? ImportJobId { get; private set; }

    /// <summary>
    /// Gets whether the import was successfully confirmed.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool ImportConfirmed { get; set; }

    /// <summary>
    /// Gets the confirmation result after import execution.
    /// </summary>
    public ConfirmProductImportResult? ConfirmResult { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadStoreAsync();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        await LoadStoreAsync();

        if (!HasStore || HasError)
        {
            return Page();
        }

        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            ModelState.AddModelError("UploadedFile", "Please select a file to upload.");
            return Page();
        }

        // Validate file size
        if (UploadedFile.Length > MaxFileSizeBytes)
        {
            ModelState.AddModelError("UploadedFile", $"File size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
            return Page();
        }

        // Validate file extension
        var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            ModelState.AddModelError("UploadedFile", $"Unsupported file type. Supported types: {string.Join(", ", SupportedExtensions)}");
            return Page();
        }

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return Page();
            }

            using var stream = UploadedFile.OpenReadStream();
            var command = new UploadProductImportCommand
            {
                StoreId = store.Id,
                SellerId = sellerId,
                FileName = UploadedFile.FileName,
                FileContent = stream
            };

            UploadResult = await _importService.UploadAndValidateAsync(command);
            ImportJobId = UploadResult.ImportJobId;

            if (UploadResult.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogInformation(
                "Product import file {FileName} uploaded by seller {SellerId}: {TotalRows} rows, {NewProducts} new, {UpdatedProducts} updates, {Errors} errors",
                UploadedFile.FileName, sellerId, UploadResult.TotalRows, UploadResult.NewProductsCount, UploadResult.UpdatedProductsCount, UploadResult.ErrorCount);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading product import file");
            ModelState.AddModelError(string.Empty, "An error occurred while processing the file. Please try again.");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid importJobId)
    {
        await LoadStoreAsync();

        if (!HasStore || HasError)
        {
            return Page();
        }

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return Page();
            }

            var command = new ConfirmProductImportCommand
            {
                ImportJobId = importJobId,
                StoreId = store.Id,
                SellerId = sellerId
            };

            ConfirmResult = await _importService.ConfirmImportAsync(command);

            if (ConfirmResult.IsNotAuthorized)
            {
                return Forbid();
            }

            if (ConfirmResult.Succeeded)
            {
                _logger.LogInformation(
                    "Product import job {JobId} confirmed by seller {SellerId}: {Created} created, {Updated} updated",
                    importJobId, sellerId, ConfirmResult.CreatedCount, ConfirmResult.UpdatedCount);

                return RedirectToPage("Import", new { importConfirmed = true });
            }

            ImportJobId = importJobId;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming product import job {JobId}", importJobId);
            ModelState.AddModelError(string.Empty, "An error occurred while confirming the import. Please try again.");
            return Page();
        }
    }

    private async Task LoadStoreAsync()
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
                ErrorMessage = "You must configure your store profile before importing products.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store for seller {SellerId}", sellerId);
            HasError = true;
            ErrorMessage = "An error occurred while loading your store. Please try again.";
        }
    }
}
