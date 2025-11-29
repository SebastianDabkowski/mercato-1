using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Payouts;

/// <summary>
/// Page model for the seller payouts index page with filtering support.
/// </summary>
[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly IPayoutService _payoutService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="payoutService">The payout service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IPayoutService payoutService,
        IStoreProfileService storeProfileService,
        ILogger<IndexModel> logger)
    {
        _payoutService = payoutService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the seller's store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets the list of payouts for the seller.
    /// </summary>
    public IReadOnlyList<Payout> Payouts { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets the selected status for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public PayoutStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start date for date range filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets all available payout statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<PayoutStatus> AllStatuses => Enum.GetValues<PayoutStatus>();

    /// <summary>
    /// Handles GET requests for the seller payouts index page with filtering.
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

            var query = new GetPayoutsFilteredQuery
            {
                SellerId = Store.Id,
                Status = Status,
                FromDate = FromDate,
                ToDate = ToDate
            };

            var result = await _payoutService.GetPayoutsFilteredAsync(query);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            Payouts = result.Payouts;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payouts for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your payouts.";
            return Page();
        }
    }

    /// <summary>
    /// Gets the query string for clear filters link.
    /// </summary>
    /// <returns>The base page URL.</returns>
    public string GetClearFiltersUrl()
    {
        return Url.Page("Index") ?? "/Seller/Payouts";
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
    /// Gets the CSS class for a payout status badge.
    /// </summary>
    /// <param name="status">The payout status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(PayoutStatus status) => status switch
    {
        PayoutStatus.Scheduled => "bg-secondary",
        PayoutStatus.Processing => "bg-info",
        PayoutStatus.Paid => "bg-success",
        PayoutStatus.Failed => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a payout status.
    /// </summary>
    /// <param name="status">The payout status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(PayoutStatus status) => status switch
    {
        PayoutStatus.Scheduled => "Scheduled",
        PayoutStatus.Processing => "Processing",
        PayoutStatus.Paid => "Paid",
        PayoutStatus.Failed => "Failed",
        _ => status.ToString()
    };

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
