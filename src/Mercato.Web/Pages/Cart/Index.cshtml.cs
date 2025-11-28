using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Cart
{
    /// <summary>
    /// Page model for the shopping cart index page.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly IPromoCodeService _promoCodeService;
        private readonly ILogger<IndexModel> _logger;
        private const string GuestCartCookieName = "GuestCartId";

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="cartService">The cart service.</param>
        /// <param name="promoCodeService">The promo code service.</param>
        /// <param name="logger">The logger.</param>
        public IndexModel(
            ICartService cartService,
            IPromoCodeService promoCodeService,
            ILogger<IndexModel> logger)
        {
            _cartService = cartService;
            _promoCodeService = promoCodeService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the cart result containing items grouped by store.
        /// </summary>
        public GetCartResult CartResult { get; private set; } = null!;

        /// <summary>
        /// Gets a value indicating whether the user is a guest.
        /// </summary>
        public bool IsGuest { get; private set; }

        /// <summary>
        /// Gets or sets the promo code input from the form.
        /// </summary>
        [BindProperty]
        public string? PromoCodeInput { get; set; }

        /// <summary>
        /// Handles GET requests for the cart page.
        /// </summary>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (!string.IsNullOrEmpty(buyerId))
            {
                // Authenticated user
                IsGuest = false;
                CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });
            }
            else
            {
                // Guest user
                IsGuest = true;
                var guestCartId = Request.Cookies[GuestCartCookieName];
                if (!string.IsNullOrEmpty(guestCartId))
                {
                    CartResult = await _cartService.GetGuestCartAsync(guestCartId);
                }
                else
                {
                    // No guest cart exists
                    CartResult = GetCartResult.Success(null);
                }
            }

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
            
            UpdateCartItemQuantityResult result;

            if (!string.IsNullOrEmpty(buyerId))
            {
                // Authenticated user
                result = await _cartService.UpdateQuantityAsync(new UpdateCartItemQuantityCommand
                {
                    BuyerId = buyerId,
                    CartItemId = cartItemId,
                    Quantity = quantity
                });
            }
            else
            {
                // Guest user
                var guestCartId = Request.Cookies[GuestCartCookieName];
                if (string.IsNullOrEmpty(guestCartId))
                {
                    TempData["Error"] = "Cart not found.";
                    return RedirectToPage();
                }

                result = await _cartService.UpdateGuestQuantityAsync(
                    new UpdateCartItemQuantityCommand
                    {
                        CartItemId = cartItemId,
                        Quantity = quantity
                    },
                    guestCartId);
            }

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
            
            RemoveCartItemResult result;

            if (!string.IsNullOrEmpty(buyerId))
            {
                // Authenticated user
                result = await _cartService.RemoveItemAsync(new RemoveCartItemCommand
                {
                    BuyerId = buyerId,
                    CartItemId = cartItemId
                });
            }
            else
            {
                // Guest user
                var guestCartId = Request.Cookies[GuestCartCookieName];
                if (string.IsNullOrEmpty(guestCartId))
                {
                    TempData["Error"] = "Cart not found.";
                    return RedirectToPage();
                }

                result = await _cartService.RemoveGuestItemAsync(
                    new RemoveCartItemCommand
                    {
                        CartItemId = cartItemId
                    },
                    guestCartId);
            }

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

        /// <summary>
        /// Handles POST requests to apply a promo code.
        /// </summary>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnPostApplyPromoCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(PromoCodeInput))
            {
                TempData["Error"] = "Please enter a promo code.";
                return RedirectToPage();
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            ApplyPromoCodeResult result;

            if (!string.IsNullOrEmpty(buyerId))
            {
                // Authenticated user
                result = await _promoCodeService.ApplyPromoCodeAsync(new ApplyPromoCodeCommand
                {
                    BuyerId = buyerId,
                    PromoCode = PromoCodeInput
                });
            }
            else
            {
                // Guest user
                var guestCartId = Request.Cookies[GuestCartCookieName];
                if (string.IsNullOrEmpty(guestCartId))
                {
                    TempData["Error"] = "Cart not found.";
                    return RedirectToPage();
                }

                result = await _promoCodeService.ApplyPromoCodeToGuestCartAsync(new ApplyPromoCodeCommand
                {
                    GuestCartId = guestCartId,
                    PromoCode = PromoCodeInput
                });
            }

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
                TempData["Success"] = $"Promo code applied! You saved {result.DiscountAmount:C}.";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Handles POST requests to remove a promo code.
        /// </summary>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnPostRemovePromoCodeAsync()
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            RemovePromoCodeResult result;

            if (!string.IsNullOrEmpty(buyerId))
            {
                // Authenticated user
                result = await _promoCodeService.RemovePromoCodeAsync(new RemovePromoCodeCommand
                {
                    BuyerId = buyerId
                });
            }
            else
            {
                // Guest user
                var guestCartId = Request.Cookies[GuestCartCookieName];
                if (string.IsNullOrEmpty(guestCartId))
                {
                    TempData["Error"] = "Cart not found.";
                    return RedirectToPage();
                }

                result = await _promoCodeService.RemovePromoCodeFromGuestCartAsync(new RemovePromoCodeCommand
                {
                    GuestCartId = guestCartId
                });
            }

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
                TempData["Success"] = "Promo code removed.";
            }

            return RedirectToPage();
        }
    }
}
