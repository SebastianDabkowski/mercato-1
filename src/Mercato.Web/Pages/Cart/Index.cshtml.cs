using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Cart
{
    /// <summary>
    /// Page model for the shopping cart index page.
    /// </summary>
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ILogger<IndexModel> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="cartService">The cart service.</param>
        /// <param name="logger">The logger.</param>
        public IndexModel(ICartService cartService, ILogger<IndexModel> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the cart result containing items grouped by store.
        /// </summary>
        public GetCartResult CartResult { get; private set; } = null!;

        /// <summary>
        /// Handles GET requests for the cart page.
        /// </summary>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId))
            {
                return RedirectToPage("/Account/Login");
            }

            CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });
            return Page();
        }

        /// <summary>
        /// Handles POST requests to update cart item quantity.
        /// </summary>
        /// <param name="cartItemId">The cart item ID.</param>
        /// <param name="quantity">The new quantity.</param>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnPostUpdateQuantityAsync(Guid cartItemId, int quantity)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId))
            {
                return RedirectToPage("/Account/Login");
            }

            var result = await _cartService.UpdateQuantityAsync(new UpdateCartItemQuantityCommand
            {
                BuyerId = buyerId,
                CartItemId = cartItemId,
                Quantity = quantity
            });

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors);
            }
            else
            {
                TempData["Success"] = "Cart updated successfully.";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Handles POST requests to remove a cart item.
        /// </summary>
        /// <param name="cartItemId">The cart item ID to remove.</param>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnPostRemoveItemAsync(Guid cartItemId)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId))
            {
                return RedirectToPage("/Account/Login");
            }

            var result = await _cartService.RemoveItemAsync(new RemoveCartItemCommand
            {
                BuyerId = buyerId,
                CartItemId = cartItemId
            });

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors);
            }
            else
            {
                TempData["Success"] = "Item removed from cart.";
            }

            return RedirectToPage();
        }
    }
}
