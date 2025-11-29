using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for submitting a seller rating for a delivered sub-order.
/// </summary>
[Authorize(Roles = "Buyer")]
public class SubmitSellerRatingModel : PageModel
{
    private readonly ISellerRatingService _sellerRatingService;
    private readonly ISellerSubOrderRepository _sellerSubOrderRepository;
    private readonly ILogger<SubmitSellerRatingModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitSellerRatingModel"/> class.
    /// </summary>
    /// <param name="sellerRatingService">The seller rating service.</param>
    /// <param name="sellerSubOrderRepository">The seller sub-order repository.</param>
    /// <param name="logger">The logger.</param>
    public SubmitSellerRatingModel(
        ISellerRatingService sellerRatingService,
        ISellerSubOrderRepository sellerSubOrderRepository,
        ILogger<SubmitSellerRatingModel> logger)
    {
        _sellerRatingService = sellerRatingService;
        _sellerSubOrderRepository = sellerSubOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets the seller sub-order being rated.
    /// </summary>
    public SellerSubOrder? SubOrder { get; private set; }

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    [BindProperty]
    public int Rating { get; set; } = 5;

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the submit seller rating page.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid subOrderId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Check if rating can be submitted
        var canSubmitResult = await _sellerRatingService.CanSubmitRatingAsync(subOrderId, buyerId);
        if (!canSubmitResult.Succeeded)
        {
            if (canSubmitResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", canSubmitResult.Errors);
            return Page();
        }

        if (!canSubmitResult.CanSubmit)
        {
            ErrorMessage = canSubmitResult.BlockedReason;
            return Page();
        }

        // Load sub-order for display
        await LoadSubOrderAsync(subOrderId);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to submit a seller rating.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(Guid subOrderId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Load sub-order first for display on errors
        await LoadSubOrderAsync(subOrderId);

        try
        {
            var command = new SubmitSellerRatingCommand
            {
                SellerSubOrderId = subOrderId,
                BuyerId = buyerId,
                Rating = Rating
            };

            var result = await _sellerRatingService.SubmitRatingAsync(command);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }

                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            SuccessMessage = "Your rating has been submitted successfully. Thank you for your feedback!";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting rating for sub-order {SubOrderId}", subOrderId);
            ErrorMessage = "An error occurred while submitting your rating. Please try again.";
            return Page();
        }
    }

    private async Task LoadSubOrderAsync(Guid subOrderId)
    {
        var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
        if (subOrder != null)
        {
            SubOrder = subOrder;
        }
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
