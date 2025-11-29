using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for requesting a return on a delivered sub-order.
/// </summary>
[Authorize(Roles = "Buyer")]
public class RequestReturnModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ReturnSettings _returnSettings;
    private readonly ILogger<RequestReturnModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestReturnModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="returnSettings">The return settings.</param>
    /// <param name="logger">The logger.</param>
    public RequestReturnModel(
        IOrderService orderService,
        IOptions<ReturnSettings> returnSettings,
        ILogger<RequestReturnModel> logger)
    {
        _orderService = orderService;
        _returnSettings = returnSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the seller sub-order.
    /// </summary>
    public SellerSubOrder? SubOrder { get; private set; }

    /// <summary>
    /// Gets or sets the return reason.
    /// </summary>
    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets the return window days from settings.
    /// </summary>
    public int ReturnWindowDays => _returnSettings.ReturnWindowDays;

    /// <summary>
    /// Handles GET requests for the request return page.
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

        // Check if return can be initiated
        var canInitiateResult = await _orderService.CanInitiateReturnAsync(subOrderId, buyerId);
        if (!canInitiateResult.Succeeded)
        {
            if (canInitiateResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", canInitiateResult.Errors);
            return Page();
        }

        if (!canInitiateResult.CanInitiate)
        {
            ErrorMessage = canInitiateResult.BlockedReason;
            return Page();
        }

        // Load sub-order for display
        await LoadSubOrderAsync(subOrderId, buyerId);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to submit a return request.
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

        try
        {
            var command = new CreateReturnRequestCommand
            {
                SellerSubOrderId = subOrderId,
                BuyerId = buyerId,
                Reason = Reason
            };

            var result = await _orderService.CreateReturnRequestAsync(command);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }

                ErrorMessage = string.Join(", ", result.Errors);
                await LoadSubOrderAsync(subOrderId, buyerId);
                return Page();
            }

            SuccessMessage = "Your return request has been submitted successfully. The seller will review your request and respond within 3-5 business days.";
            await LoadSubOrderAsync(subOrderId, buyerId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting return request for sub-order {SubOrderId}", subOrderId);
            ErrorMessage = "An error occurred while submitting your return request. Please try again.";
            await LoadSubOrderAsync(subOrderId, buyerId);
            return Page();
        }
    }

    private async Task LoadSubOrderAsync(Guid subOrderId, string buyerId)
    {
        // We need to get the sub-order through the order service
        // Since there's no direct buyer-facing method, we'll use a workaround
        // by getting all orders for the buyer and finding the matching sub-order
        var ordersResult = await _orderService.GetOrdersForBuyerAsync(buyerId);
        if (ordersResult.Succeeded)
        {
            foreach (var order in ordersResult.Orders)
            {
                var matchingSubOrder = order.SellerSubOrders.FirstOrDefault(s => s.Id == subOrderId);
                if (matchingSubOrder != null)
                {
                    SubOrder = matchingSubOrder;
                    break;
                }
            }
        }
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
