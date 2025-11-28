using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the checkout page with address step.
/// </summary>
[Authorize(Roles = "Buyer")]
public class CheckoutModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IDeliveryAddressService _deliveryAddressService;
    private readonly ILogger<CheckoutModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckoutModel"/> class.
    /// </summary>
    /// <param name="cartService">The cart service.</param>
    /// <param name="deliveryAddressService">The delivery address service.</param>
    /// <param name="logger">The logger.</param>
    public CheckoutModel(
        ICartService cartService,
        IDeliveryAddressService deliveryAddressService,
        ILogger<CheckoutModel> logger)
    {
        _cartService = cartService;
        _deliveryAddressService = deliveryAddressService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the cart result containing cart items.
    /// </summary>
    public GetCartResult CartResult { get; private set; } = null!;

    /// <summary>
    /// Gets the list of saved delivery addresses.
    /// </summary>
    public IReadOnlyList<DeliveryAddressDto> SavedAddresses { get; private set; } = [];

    /// <summary>
    /// Gets or sets the selected address ID.
    /// </summary>
    [BindProperty]
    public Guid? SelectedAddressId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use a new address.
    /// </summary>
    [BindProperty]
    public bool UseNewAddress { get; set; }

    /// <summary>
    /// Gets or sets the new address input model.
    /// </summary>
    [BindProperty]
    public AddressInputModel NewAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to save the new address to profile.
    /// </summary>
    [BindProperty]
    public bool SaveAddressToProfile { get; set; } = true;

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the list of allowed shipping countries.
    /// </summary>
    public IReadOnlyList<string> AllowedCountries => _deliveryAddressService.AllowedShippingCountries;

    /// <summary>
    /// Gets a value indicating whether the cart is empty.
    /// </summary>
    public bool IsCartEmpty => CartResult?.ItemsByStore == null || CartResult.ItemsByStore.Count == 0;

    /// <summary>
    /// Handles GET requests for the checkout page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
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

        // Load saved addresses
        var addressesResult = await _deliveryAddressService.GetAddressesAsync(
            new GetDeliveryAddressesQuery { BuyerId = buyerId });

        if (addressesResult.Succeeded)
        {
            SavedAddresses = addressesResult.Addresses;

            // Auto-select default address if available
            var defaultAddress = SavedAddresses.FirstOrDefault(a => a.IsDefault);
            if (defaultAddress != null)
            {
                SelectedAddressId = defaultAddress.Id;
            }
            else if (SavedAddresses.Count > 0)
            {
                SelectedAddressId = SavedAddresses[0].Id;
            }
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to proceed with the selected or new address.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Load cart first
        CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });

        if (!CartResult.Succeeded || IsCartEmpty)
        {
            return RedirectToPage("/Cart/Index");
        }

        // Load saved addresses for display
        var addressesResult = await _deliveryAddressService.GetAddressesAsync(
            new GetDeliveryAddressesQuery { BuyerId = buyerId });

        if (addressesResult.Succeeded)
        {
            SavedAddresses = addressesResult.Addresses;
        }

        if (UseNewAddress)
        {
            return await HandleNewAddressAsync(buyerId);
        }
        else
        {
            return await HandleSelectedAddressAsync(buyerId);
        }
    }

    /// <summary>
    /// Handles POST requests to delete a saved address.
    /// </summary>
    /// <param name="addressId">The address ID to delete.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostDeleteAddressAsync(Guid addressId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        var result = await _deliveryAddressService.DeleteAddressAsync(new DeleteDeliveryAddressCommand
        {
            AddressId = addressId,
            BuyerId = buyerId
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
            TempData["Success"] = "Address deleted successfully.";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST requests to set an address as default.
    /// </summary>
    /// <param name="addressId">The address ID to set as default.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostSetDefaultAddressAsync(Guid addressId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        var result = await _deliveryAddressService.SetDefaultAddressAsync(new SetDefaultDeliveryAddressCommand
        {
            AddressId = addressId,
            BuyerId = buyerId
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
            TempData["Success"] = "Default address updated.";
        }

        return RedirectToPage();
    }

    private async Task<IActionResult> HandleNewAddressAsync(string buyerId)
    {
        // Validate new address model state (data annotations handle required fields)
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if region is allowed
        if (!_deliveryAddressService.IsShippingAllowedToRegion(NewAddress.Country))
        {
            ModelState.AddModelError(nameof(NewAddress.Country),
                $"Shipping to {NewAddress.Country} is not available. Please choose a different delivery region.");
            return Page();
        }

        // Save address if requested
        if (SaveAddressToProfile)
        {
            var saveResult = await _deliveryAddressService.SaveAddressAsync(new SaveDeliveryAddressCommand
            {
                BuyerId = buyerId,
                Label = NewAddress.Label,
                FullName = NewAddress.FullName,
                AddressLine1 = NewAddress.AddressLine1,
                AddressLine2 = NewAddress.AddressLine2,
                City = NewAddress.City,
                State = NewAddress.State,
                PostalCode = NewAddress.PostalCode,
                Country = NewAddress.Country,
                PhoneNumber = NewAddress.PhoneNumber,
                SetAsDefault = NewAddress.SetAsDefault
            });

            if (!saveResult.Succeeded)
            {
                if (saveResult.IsRegionNotAllowed)
                {
                    ModelState.AddModelError(nameof(NewAddress.Country), saveResult.Errors.FirstOrDefault() ?? "Region not allowed.");
                }
                else
                {
                    foreach (var error in saveResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }
                return Page();
            }

            SelectedAddressId = saveResult.AddressId;
        }

        // Store address in TempData for next step
        StoreAddressInTempData(
            NewAddress.FullName,
            NewAddress.AddressLine1,
            NewAddress.AddressLine2,
            NewAddress.City,
            NewAddress.State,
            NewAddress.PostalCode,
            NewAddress.Country,
            NewAddress.PhoneNumber);

        TempData["Success"] = "Address confirmed. Proceed to payment.";

        // TODO: Navigate to payment step when implemented
        // For now, redirect back with success message
        return RedirectToPage();
    }

    private async Task<IActionResult> HandleSelectedAddressAsync(string buyerId)
    {
        if (!SelectedAddressId.HasValue || SelectedAddressId.Value == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Please select a delivery address or enter a new one.");
            return Page();
        }

        // Verify the selected address belongs to the buyer
        var addressesResult = await _deliveryAddressService.GetAddressesAsync(
            new GetDeliveryAddressesQuery { BuyerId = buyerId });

        if (!addressesResult.Succeeded)
        {
            ErrorMessage = "Unable to verify address.";
            return Page();
        }

        var selectedAddress = addressesResult.Addresses.FirstOrDefault(a => a.Id == SelectedAddressId.Value);
        if (selectedAddress == null)
        {
            ModelState.AddModelError(string.Empty, "Selected address not found.");
            return Page();
        }

        // Check if region is allowed
        if (!_deliveryAddressService.IsShippingAllowedToRegion(selectedAddress.Country))
        {
            ModelState.AddModelError(string.Empty,
                $"Shipping to {selectedAddress.Country} is not available. Please choose a different delivery address.");
            return Page();
        }

        // Store address in TempData for next step
        StoreAddressInTempData(
            selectedAddress.FullName,
            selectedAddress.AddressLine1,
            selectedAddress.AddressLine2,
            selectedAddress.City,
            selectedAddress.State,
            selectedAddress.PostalCode,
            selectedAddress.Country,
            selectedAddress.PhoneNumber);

        TempData["Success"] = "Address confirmed. Proceed to payment.";

        // TODO: Navigate to payment step when implemented
        // For now, redirect back with success message
        return RedirectToPage();
    }

    private void StoreAddressInTempData(
        string fullName,
        string addressLine1,
        string? addressLine2,
        string city,
        string? state,
        string postalCode,
        string country,
        string? phoneNumber)
    {
        TempData["CheckoutAddress"] = System.Text.Json.JsonSerializer.Serialize(new
        {
            FullName = fullName,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country,
            PhoneNumber = phoneNumber
        });
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

/// <summary>
/// Input model for a new delivery address.
/// </summary>
public class AddressInputModel
{
    /// <summary>
    /// Gets or sets the label for the address.
    /// </summary>
    [StringLength(50)]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary address line.
    /// </summary>
    [Required(ErrorMessage = "Address line 1 is required.")]
    [StringLength(500)]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary address line.
    /// </summary>
    [StringLength(500)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    [StringLength(100)]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    [Required(ErrorMessage = "Postal code is required.")]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO country code.
    /// </summary>
    [Required(ErrorMessage = "Country is required.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter ISO code.")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set this as the default address.
    /// </summary>
    public bool SetAsDefault { get; set; }
}
