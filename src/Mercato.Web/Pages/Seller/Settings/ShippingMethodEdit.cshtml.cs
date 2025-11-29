using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Settings;

/// <summary>
/// Page model for editing a shipping method.
/// </summary>
[Authorize(Roles = "Seller")]
public class ShippingMethodEditModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ShippingMethodEditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodEditModel"/> class.
    /// </summary>
    /// <param name="shippingMethodService">The shipping method service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public ShippingMethodEditModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService,
        ILogger<ShippingMethodEditModel> logger)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the shipping method ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the shipping method name.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Shipping method name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Shipping method name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shipping method description.
    /// </summary>
    [BindProperty]
    [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the available countries (comma-separated ISO codes).
    /// </summary>
    [BindProperty]
    [StringLength(1000, ErrorMessage = "Available countries must be at most 1000 characters.")]
    public string? AvailableCountries { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the shipping method is active.
    /// </summary>
    [BindProperty]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the base shipping cost (flat rate).
    /// </summary>
    [BindProperty]
    [Range(0, 999999.99, ErrorMessage = "Base cost must be between 0 and 999,999.99.")]
    public decimal BaseCost { get; set; }

    /// <summary>
    /// Gets or sets the minimum estimated delivery time in business days.
    /// </summary>
    [BindProperty]
    [Range(0, 365, ErrorMessage = "Minimum delivery days must be between 0 and 365.")]
    public int? EstimatedDeliveryMinDays { get; set; }

    /// <summary>
    /// Gets or sets the maximum estimated delivery time in business days.
    /// </summary>
    [BindProperty]
    [Range(0, 365, ErrorMessage = "Maximum delivery days must be between 0 and 365.")]
    public int? EstimatedDeliveryMaxDays { get; set; }

    /// <summary>
    /// Gets the current store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets a success message.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Handles GET requests to display the edit form.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                return RedirectToPage("ShippingMethods");
            }

            var shippingMethod = await _shippingMethodService.GetByIdAsync(Id);
            if (shippingMethod == null)
            {
                return RedirectToPage("ShippingMethods");
            }

            // Verify the shipping method belongs to the seller's store
            if (shippingMethod.StoreId != Store.Id)
            {
                _logger.LogWarning(
                    "Seller {SellerId} attempted to access shipping method {ShippingMethodId} that doesn't belong to their store",
                    sellerId, Id);
                return Forbid();
            }

            // Pre-fill the form
            Name = shippingMethod.Name;
            Description = shippingMethod.Description;
            AvailableCountries = shippingMethod.AvailableCountries;
            IsActive = shippingMethod.IsActive;
            BaseCost = shippingMethod.BaseCost;
            EstimatedDeliveryMinDays = shippingMethod.EstimatedDeliveryMinDays;
            EstimatedDeliveryMaxDays = shippingMethod.EstimatedDeliveryMaxDays;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit shipping method page for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to update the shipping method.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                return RedirectToPage("ShippingMethods");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var command = new UpdateShippingMethodCommand
            {
                Id = Id,
                StoreId = Store.Id,
                Name = Name,
                Description = Description,
                AvailableCountries = AvailableCountries,
                IsActive = IsActive,
                BaseCost = BaseCost,
                EstimatedDeliveryMinDays = EstimatedDeliveryMinDays,
                EstimatedDeliveryMaxDays = EstimatedDeliveryMaxDays
            };

            var result = await _shippingMethodService.UpdateAsync(command);
            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Updated shipping method {ShippingMethodId} for seller {SellerId}",
                    Id, sellerId);
                SuccessMessage = "Shipping method updated successfully.";
                return Page();
            }

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping method {ShippingMethodId} for seller {SellerId}", Id, sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the shipping method. Please try again.");
            return Page();
        }
    }
}
