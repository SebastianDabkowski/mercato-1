using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the shipping method selection step.
/// </summary>
[Authorize(Roles = "Buyer")]
public class ShippingModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly ILogger<ShippingModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingModel"/> class.
    /// </summary>
    /// <param name="cartService">The cart service.</param>
    /// <param name="shippingMethodService">The shipping method service.</param>
    /// <param name="logger">The logger.</param>
    public ShippingModel(
        ICartService cartService,
        IShippingMethodService shippingMethodService,
        ILogger<ShippingModel> logger)
    {
        _cartService = cartService;
        _shippingMethodService = shippingMethodService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the cart result containing cart items.
    /// </summary>
    public GetCartResult CartResult { get; private set; } = null!;

    /// <summary>
    /// Gets the available shipping methods grouped by store.
    /// </summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<ShippingMethodDto>> ShippingMethodsByStore { get; private set; } =
        new Dictionary<Guid, IReadOnlyList<ShippingMethodDto>>();

    /// <summary>
    /// Gets or sets the selected shipping methods by store.
    /// </summary>
    [BindProperty]
    public Dictionary<Guid, string> SelectedShippingMethods { get; set; } = new();

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the cart is empty.
    /// </summary>
    public bool IsCartEmpty => CartResult?.ItemsByStore == null || CartResult.ItemsByStore.Count == 0;

    /// <summary>
    /// Gets the delivery address from session.
    /// </summary>
    public CheckoutAddressData? DeliveryAddress { get; private set; }

    /// <summary>
    /// Handles GET requests for the shipping page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Check for address data from previous step
        if (!TryLoadDeliveryAddress())
        {
            TempData["Error"] = "Please select a delivery address first.";
            return RedirectToPage("Checkout");
        }

        // Load cart
        CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });

        if (!CartResult.Succeeded)
        {
            ErrorMessage = string.Join(", ", CartResult.Errors);
            return Page();
        }

        if (IsCartEmpty)
        {
            return RedirectToPage("/Cart/Index");
        }

        // Get available shipping methods
        var storeIds = CartResult.ItemsByStore.Select(s => s.StoreId).ToList();
        var shippingResult = await _shippingMethodService.GetShippingMethodsAsync(storeIds, CartResult.ItemsByStore);

        if (!shippingResult.Succeeded)
        {
            ErrorMessage = string.Join(", ", shippingResult.Errors);
            return Page();
        }

        ShippingMethodsByStore = shippingResult.MethodsByStore;

        // Pre-select default methods
        foreach (var storeGroup in ShippingMethodsByStore)
        {
            var defaultMethod = storeGroup.Value.FirstOrDefault(m => m.IsDefault) ?? storeGroup.Value.FirstOrDefault();
            if (defaultMethod != null)
            {
                SelectedShippingMethods[storeGroup.Key] = defaultMethod.Id;
            }
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to proceed with selected shipping methods.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Check for address data from previous step
        if (!TryLoadDeliveryAddress())
        {
            TempData["Error"] = "Please select a delivery address first.";
            return RedirectToPage("Checkout");
        }

        // Load cart
        CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });

        if (!CartResult.Succeeded || IsCartEmpty)
        {
            return RedirectToPage("/Cart/Index");
        }

        // Get available shipping methods for display
        var storeIds = CartResult.ItemsByStore.Select(s => s.StoreId).ToList();
        var shippingResult = await _shippingMethodService.GetShippingMethodsAsync(storeIds, CartResult.ItemsByStore);

        if (!shippingResult.Succeeded)
        {
            ErrorMessage = string.Join(", ", shippingResult.Errors);
            return Page();
        }

        ShippingMethodsByStore = shippingResult.MethodsByStore;

        // Validate shipping method selection
        if (!_shippingMethodService.ValidateShippingMethodSelection(SelectedShippingMethods, storeIds))
        {
            ModelState.AddModelError(string.Empty, "Please select a shipping method for each store.");
            return Page();
        }

        // Calculate total shipping cost
        var totalShipping = await _shippingMethodService.GetTotalShippingCostAsync(
            SelectedShippingMethods,
            CartResult.ItemsByStore);

        // Store shipping selection in TempData for next step
        StoreShippingInTempData(totalShipping);

        TempData["Success"] = "Shipping methods selected. Proceed to payment.";

        return RedirectToPage("Payment");
    }

    private bool TryLoadDeliveryAddress()
    {
        if (TempData.Peek("CheckoutAddress") is string addressJson)
        {
            try
            {
                DeliveryAddress = JsonSerializer.Deserialize<CheckoutAddressData>(addressJson);
                return DeliveryAddress != null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize checkout address");
            }
        }

        return false;
    }

    private void StoreShippingInTempData(decimal totalShipping)
    {
        // Build shipping method names and costs by store
        var methodNames = new Dictionary<Guid, string>();
        var costsByStore = new Dictionary<Guid, decimal>();

        foreach (var (storeId, methodId) in SelectedShippingMethods)
        {
            if (ShippingMethodsByStore.TryGetValue(storeId, out var methods))
            {
                var selectedMethod = methods.FirstOrDefault(m => m.Id == methodId);
                if (selectedMethod != null)
                {
                    methodNames[storeId] = selectedMethod.Name;
                    costsByStore[storeId] = selectedMethod.Cost;
                }
            }
        }

        var shippingData = new CheckoutShippingData
        {
            SelectedMethods = SelectedShippingMethods,
            TotalShippingCost = totalShipping,
            ShippingMethodNames = methodNames,
            ShippingCostsByStore = costsByStore
        };

        TempData["CheckoutShipping"] = JsonSerializer.Serialize(shippingData);

        // Keep the address for the next step
        if (TempData.Peek("CheckoutAddress") is string addressJson)
        {
            TempData.Keep("CheckoutAddress");
        }
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

/// <summary>
/// Data class for checkout address stored in TempData.
/// </summary>
public class CheckoutAddressData
{
    /// <summary>
    /// Gets or sets the full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address line 1.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address line 2.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets the formatted address.
    /// </summary>
    public string FormattedAddress => string.IsNullOrEmpty(AddressLine2)
        ? $"{AddressLine1}, {City}, {State} {PostalCode}, {Country}"
        : $"{AddressLine1}, {AddressLine2}, {City}, {State} {PostalCode}, {Country}";
}

/// <summary>
/// Data class for checkout shipping stored in TempData.
/// </summary>
public class CheckoutShippingData
{
    /// <summary>
    /// Gets or sets the selected shipping methods by store ID.
    /// </summary>
    public Dictionary<Guid, string> SelectedMethods { get; set; } = new();

    /// <summary>
    /// Gets or sets the total shipping cost.
    /// </summary>
    public decimal TotalShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the shipping method names by store ID.
    /// </summary>
    public Dictionary<Guid, string> ShippingMethodNames { get; set; } = new();

    /// <summary>
    /// Gets or sets the shipping costs by store ID.
    /// </summary>
    public Dictionary<Guid, decimal> ShippingCostsByStore { get; set; } = new();
}
