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
/// Page model for submitting a return or complaint case for a delivered sub-order.
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
    /// Gets or sets the case type (Return or Complaint).
    /// </summary>
    [BindProperty]
    public CaseType CaseType { get; set; } = CaseType.Return;

    /// <summary>
    /// Gets or sets the reason for the return or complaint.
    /// </summary>
    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected item IDs for the case.
    /// </summary>
    [BindProperty]
    public List<Guid> SelectedItemIds { get; set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets the case number after successful submission.
    /// </summary>
    public string? CaseNumber { get; private set; }

    /// <summary>
    /// Gets the return window days from settings.
    /// </summary>
    public int ReturnWindowDays => _returnSettings.ReturnWindowDays;

    /// <summary>
    /// Gets the set of item IDs that already have open cases.
    /// </summary>
    public HashSet<Guid> ItemsWithOpenCases { get; private set; } = [];

    /// <summary>
    /// Handles GET requests for the submit case page.
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
        
        // Load items with open cases
        await LoadItemsWithOpenCasesAsync(subOrderId, buyerId);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to submit a return or complaint case.
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
        await LoadSubOrderAsync(subOrderId, buyerId);
        await LoadItemsWithOpenCasesAsync(subOrderId, buyerId);

        // Validate that at least one item is selected
        if (SelectedItemIds.Count == 0)
        {
            ErrorMessage = "Please select at least one item to include in your case.";
            return Page();
        }

        try
        {
            var selectedItems = SubOrder?.Items
                .Where(i => SelectedItemIds.Contains(i.Id))
                .Select(i => new CaseItemSelection
                {
                    ItemId = i.Id,
                    Quantity = i.Quantity
                })
                .ToList() ?? [];

            var command = new CreateReturnRequestCommand
            {
                SellerSubOrderId = subOrderId,
                BuyerId = buyerId,
                CaseType = CaseType,
                Reason = Reason,
                SelectedItems = selectedItems
            };

            var result = await _orderService.CreateReturnRequestAsync(command);

            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }

                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            CaseNumber = result.CaseNumber;
            SuccessMessage = $"Your {(CaseType == CaseType.Complaint ? "complaint" : "return request")} has been submitted successfully. " +
                             $"The seller will review your case and respond within 3-5 business days. " +
                             $"The case status is now 'Pending seller review'.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting case for sub-order {SubOrderId}", subOrderId);
            ErrorMessage = "An error occurred while submitting your case. Please try again.";
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

    private async Task LoadItemsWithOpenCasesAsync(Guid subOrderId, string buyerId)
    {
        if (SubOrder == null)
        {
            return;
        }

        // Get all return requests for this buyer to find items with open cases
        var returnRequestsResult = await _orderService.GetReturnRequestsForBuyerAsync(buyerId);
        if (returnRequestsResult.Succeeded)
        {
            var openCaseStatuses = new[] { ReturnStatus.Requested, ReturnStatus.UnderReview, ReturnStatus.Approved };
            var openCases = returnRequestsResult.ReturnRequests
                .Where(r => r.SellerSubOrderId == subOrderId && openCaseStatuses.Contains(r.Status));

            foreach (var openCase in openCases)
            {
                foreach (var caseItem in openCase.CaseItems)
                {
                    ItemsWithOpenCases.Add(caseItem.SellerSubOrderItemId);
                }
            }
        }
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
