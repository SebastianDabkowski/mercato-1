using Mercato.Payments.Application.Services;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Invoices;

/// <summary>
/// Page model for downloading invoice PDFs.
/// </summary>
[Authorize(Roles = "Seller")]
public class DownloadModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<DownloadModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadModel"/> class.
    /// </summary>
    /// <param name="invoiceService">The commission invoice service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public DownloadModel(
        ICommissionInvoiceService invoiceService,
        IStoreProfileService storeProfileService,
        ILogger<DownloadModel> logger)
    {
        _invoiceService = invoiceService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Handles GET requests for downloading invoice PDFs.
    /// </summary>
    /// <param name="id">The invoice ID.</param>
    /// <returns>The file result or an error page.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return RedirectToPage("Index");
            }

            var result = await _invoiceService.GeneratePdfAsync(id, store.Id);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                _logger.LogWarning(
                    "Failed to generate PDF for invoice {InvoiceId}, seller {SellerId}: {Errors}",
                    id, sellerId, string.Join(", ", result.Errors));
                return RedirectToPage("Index");
            }

            return File(result.PdfContent, "application/pdf", result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF for invoice {InvoiceId}, seller {SellerId}", id, sellerId);
            return RedirectToPage("Index");
        }
    }

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
