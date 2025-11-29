using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Payouts;

/// <summary>
/// Page model for the seller payout details page.
/// </summary>
[Authorize(Roles = "Seller")]
public class DetailsModel : PageModel
{
    private readonly IPayoutService _payoutService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="payoutService">The payout service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IPayoutService payoutService,
        IStoreProfileService storeProfileService,
        ILogger<DetailsModel> logger)
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
    /// Gets the payout details.
    /// </summary>
    public Payout? Payout { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the list of escrow entry IDs parsed from the payout.
    /// </summary>
    public IReadOnlyList<string> EscrowEntryIds { get; private set; } = [];

    /// <summary>
    /// Handles GET requests for the seller payout details page.
    /// </summary>
    /// <param name="id">The payout ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
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
                ErrorMessage = "Store not found.";
                return Page();
            }

            var result = await _payoutService.GetPayoutAsync(id);
            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            Payout = result.Payout;

            // Verify the payout belongs to this seller's store
            if (Payout != null && Payout.SellerId != Store.Id)
            {
                return Forbid();
            }

            // Parse escrow entry IDs
            if (Payout != null && !string.IsNullOrEmpty(Payout.EscrowEntryIds))
            {
                EscrowEntryIds = Payout.EscrowEntryIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .ToList();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payout details for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading the payout details.";
            return Page();
        }
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

    /// <summary>
    /// Gets the display text for a payout schedule frequency.
    /// </summary>
    /// <param name="frequency">The payout schedule frequency.</param>
    /// <returns>The display text.</returns>
    public static string GetFrequencyDisplayText(PayoutScheduleFrequency frequency) => frequency switch
    {
        PayoutScheduleFrequency.Weekly => "Weekly",
        _ => frequency.ToString()
    };

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
