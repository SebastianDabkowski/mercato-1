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
/// Page model for creating a new shipping method.
/// </summary>
[Authorize(Roles = "Seller")]
public class ShippingMethodCreateModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ShippingMethodCreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodCreateModel"/> class.
    /// </summary>
    /// <param name="shippingMethodService">The shipping method service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public ShippingMethodCreateModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService,
        ILogger<ShippingMethodCreateModel> logger)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

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
    /// Gets the current store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests to display the create form.
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

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create shipping method page for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to create a new shipping method.
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

            var command = new CreateShippingMethodCommand
            {
                StoreId = Store.Id,
                Name = Name,
                Description = Description,
                AvailableCountries = AvailableCountries
            };

            var result = await _shippingMethodService.CreateAsync(command);
            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Created shipping method {ShippingMethodId} for seller {SellerId}",
                    result.ShippingMethodId, sellerId);
                return RedirectToPage("ShippingMethods");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipping method for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the shipping method. Please try again.");
            return Page();
        }
    }
}
