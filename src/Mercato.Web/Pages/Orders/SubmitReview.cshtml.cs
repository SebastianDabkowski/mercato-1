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
/// Page model for submitting a product review for a delivered order item.
/// </summary>
[Authorize(Roles = "Buyer")]
public class SubmitReviewModel : PageModel
{
    private readonly IProductReviewService _productReviewService;
    private readonly ISellerSubOrderRepository _sellerSubOrderRepository;
    private readonly ILogger<SubmitReviewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitReviewModel"/> class.
    /// </summary>
    /// <param name="productReviewService">The product review service.</param>
    /// <param name="sellerSubOrderRepository">The seller sub-order repository.</param>
    /// <param name="logger">The logger.</param>
    public SubmitReviewModel(
        IProductReviewService productReviewService,
        ISellerSubOrderRepository sellerSubOrderRepository,
        ILogger<SubmitReviewModel> logger)
    {
        _productReviewService = productReviewService;
        _sellerSubOrderRepository = sellerSubOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets the seller sub-order item being reviewed.
    /// </summary>
    public SellerSubOrderItem? Item { get; private set; }

    /// <summary>
    /// Gets the seller sub-order containing the item.
    /// </summary>
    public SellerSubOrder? SubOrder { get; private set; }

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    [BindProperty]
    public int Rating { get; set; } = 5;

    /// <summary>
    /// Gets or sets the review text content.
    /// </summary>
    [BindProperty]
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the submit review page.
    /// </summary>
    /// <param name="itemId">The seller sub-order item ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid itemId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Check if review can be submitted
        var canSubmitResult = await _productReviewService.CanSubmitReviewAsync(itemId, buyerId);
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

        // Load item for display
        await LoadItemAsync(itemId);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to submit a product review.
    /// </summary>
    /// <param name="itemId">The seller sub-order item ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(Guid itemId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Load item first for display on errors
        await LoadItemAsync(itemId);

        try
        {
            var command = new SubmitProductReviewCommand
            {
                SellerSubOrderItemId = itemId,
                BuyerId = buyerId,
                Rating = Rating,
                ReviewText = ReviewText
            };

            var result = await _productReviewService.SubmitReviewAsync(command);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }

                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            SuccessMessage = "Your review has been submitted successfully. Thank you for your feedback!";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review for item {ItemId}", itemId);
            ErrorMessage = "An error occurred while submitting your review. Please try again.";
            return Page();
        }
    }

    private async Task LoadItemAsync(Guid itemId)
    {
        // Use the repository method directly for efficient item lookup
        var item = await _sellerSubOrderRepository.GetItemByIdAsync(itemId);
        if (item != null)
        {
            Item = item;
            SubOrder = item.SellerSubOrder;
        }
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
