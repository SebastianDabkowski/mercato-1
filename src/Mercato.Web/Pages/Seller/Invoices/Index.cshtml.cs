using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Invoices;

/// <summary>
/// Page model for the seller invoices index page.
/// </summary>
[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="invoiceService">The commission invoice service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        ICommissionInvoiceService invoiceService,
        IStoreProfileService storeProfileService,
        ILogger<IndexModel> logger)
    {
        _invoiceService = invoiceService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the seller's store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets the list of invoices for the seller.
    /// </summary>
    public IReadOnlyList<CommissionInvoice> Invoices { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets the selected status for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public InvoiceStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the selected type for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public InvoiceType? Type { get; set; }

    /// <summary>
    /// Gets all available invoice statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<InvoiceStatus> AllStatuses => Enum.GetValues<InvoiceStatus>();

    /// <summary>
    /// Gets all available invoice types for the filter dropdown.
    /// </summary>
    public static IEnumerable<InvoiceType> AllTypes => Enum.GetValues<InvoiceType>();

    /// <summary>
    /// Handles GET requests for the seller invoices index page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                return Page();
            }

            var result = await _invoiceService.GetInvoicesBySellerIdAsync(Store.Id);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            // Apply filters
            var filteredInvoices = result.Invoices.AsEnumerable();
            if (Status.HasValue)
            {
                filteredInvoices = filteredInvoices.Where(i => i.Status == Status.Value);
            }
            if (Type.HasValue)
            {
                filteredInvoices = filteredInvoices.Where(i => i.InvoiceType == Type.Value);
            }

            Invoices = filteredInvoices.ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading invoices for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your invoices.";
            return Page();
        }
    }

    /// <summary>
    /// Gets the query string for clear filters link.
    /// </summary>
    /// <returns>The base page URL.</returns>
    public string GetClearFiltersUrl()
    {
        return Url.Page("Index") ?? "/Seller/Invoices";
    }

    /// <summary>
    /// Formats a GUID as a shortened ID for display.
    /// </summary>
    /// <param name="id">The GUID to format.</param>
    /// <returns>A shortened display string.</returns>
    public static string FormatShortId(Guid id)
    {
        const int shortIdLength = 8;
        var idString = id.ToString();
        return idString.Length > shortIdLength
            ? $"{idString[..shortIdLength]}..."
            : idString;
    }

    /// <summary>
    /// Gets the CSS class for an invoice status badge.
    /// </summary>
    /// <param name="status">The invoice status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "bg-secondary",
        InvoiceStatus.Issued => "bg-primary",
        InvoiceStatus.Paid => "bg-success",
        InvoiceStatus.Cancelled => "bg-danger",
        InvoiceStatus.Corrected => "bg-warning",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for an invoice status.
    /// </summary>
    /// <param name="status">The invoice status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Draft",
        InvoiceStatus.Issued => "Issued",
        InvoiceStatus.Paid => "Paid",
        InvoiceStatus.Cancelled => "Cancelled",
        InvoiceStatus.Corrected => "Corrected",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the CSS class for an invoice type badge.
    /// </summary>
    /// <param name="type">The invoice type.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetTypeBadgeClass(InvoiceType type) => type switch
    {
        InvoiceType.Standard => "bg-info",
        InvoiceType.CreditNote => "bg-warning text-dark",
        InvoiceType.Correction => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for an invoice type.
    /// </summary>
    /// <param name="type">The invoice type.</param>
    /// <returns>The display text.</returns>
    public static string GetTypeDisplayText(InvoiceType type) => type switch
    {
        InvoiceType.Standard => "Invoice",
        InvoiceType.CreditNote => "Credit Note",
        InvoiceType.Correction => "Correction",
        _ => type.ToString()
    };

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
