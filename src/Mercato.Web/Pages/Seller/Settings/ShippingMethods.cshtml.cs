using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Settings;

/// <summary>
/// Page model for managing shipping methods.
/// </summary>
[Authorize(Roles = "Seller")]
public class ShippingMethodsModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ShippingMethodsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodsModel"/> class.
    /// </summary>
    /// <param name="shippingMethodService">The shipping method service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public ShippingMethodsModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService,
        ILogger<ShippingMethodsModel> logger)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of shipping methods for the store.
    /// </summary>
    public IReadOnlyList<ShippingMethod> ShippingMethods { get; private set; } = [];

    /// <summary>
    /// Gets or sets the current store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets or sets a success message.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests to display the list of shipping methods.
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
                ErrorMessage = "You must create a store before configuring shipping methods.";
                return Page();
            }

            ShippingMethods = await _shippingMethodService.GetByStoreIdAsync(Store.Id);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shipping methods for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your shipping methods. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to delete a shipping method.
    /// </summary>
    /// <param name="id">The shipping method ID to delete.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
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
                ErrorMessage = "Store not found.";
                return Page();
            }

            var command = new DeleteShippingMethodCommand
            {
                Id = id,
                StoreId = Store.Id
            };

            var result = await _shippingMethodService.DeleteAsync(command);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted shipping method {ShippingMethodId} for seller {SellerId}", id, sellerId);
                SuccessMessage = "Shipping method deleted successfully.";
            }
            else if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            else
            {
                ErrorMessage = string.Join(" ", result.Errors);
            }

            // Reload the shipping methods
            ShippingMethods = await _shippingMethodService.GetByStoreIdAsync(Store.Id);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shipping method {ShippingMethodId} for seller {SellerId}", id, sellerId);
            ErrorMessage = "An error occurred while deleting the shipping method. Please try again.";
            
            // Reload the shipping methods
            if (Store != null)
            {
                ShippingMethods = await _shippingMethodService.GetByStoreIdAsync(Store.Id);
            }
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to toggle a shipping method's active status.
    /// </summary>
    /// <param name="id">The shipping method ID to toggle.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id)
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
                ErrorMessage = "Store not found.";
                return Page();
            }

            var shippingMethod = await _shippingMethodService.GetByIdAsync(id);
            if (shippingMethod == null)
            {
                ErrorMessage = "Shipping method not found.";
                ShippingMethods = await _shippingMethodService.GetByStoreIdAsync(Store.Id);
                return Page();
            }

            var command = new UpdateShippingMethodCommand
            {
                Id = id,
                StoreId = Store.Id,
                Name = shippingMethod.Name,
                Description = shippingMethod.Description,
                AvailableCountries = shippingMethod.AvailableCountries,
                IsActive = !shippingMethod.IsActive
            };

            var result = await _shippingMethodService.UpdateAsync(command);
            if (result.Succeeded)
            {
                var status = command.IsActive ? "enabled" : "disabled";
                _logger.LogInformation("Toggled shipping method {ShippingMethodId} to {Status} for seller {SellerId}", id, status, sellerId);
                SuccessMessage = $"Shipping method {status} successfully.";
            }
            else if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            else
            {
                ErrorMessage = string.Join(" ", result.Errors);
            }

            // Reload the shipping methods
            ShippingMethods = await _shippingMethodService.GetByStoreIdAsync(Store.Id);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling shipping method {ShippingMethodId} status for seller {SellerId}", id, sellerId);
            ErrorMessage = "An error occurred while updating the shipping method. Please try again.";
            
            // Reload the shipping methods
            if (Store != null)
            {
                ShippingMethods = await _shippingMethodService.GetByStoreIdAsync(Store.Id);
            }
            return Page();
        }
    }
}
