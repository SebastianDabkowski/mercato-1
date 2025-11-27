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
/// Page model for managing store profile settings.
/// </summary>
[Authorize(Roles = "Seller")]
public class StoreProfileModel : PageModel
{
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<StoreProfileModel> _logger;

    public StoreProfileModel(
        IStoreProfileService storeProfileService,
        ILogger<StoreProfileModel> logger)
    {
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Store name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Store name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    [BindProperty]
    [StringLength(2000, ErrorMessage = "Store description must be at most 2000 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the store logo URL.
    /// </summary>
    [BindProperty]
    [StringLength(500, ErrorMessage = "Store logo URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    [BindProperty]
    [StringLength(254, ErrorMessage = "Contact email must be at most 254 characters.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number.
    /// </summary>
    [BindProperty]
    [StringLength(20, ErrorMessage = "Phone number must be at most 20 characters.")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the website URL.
    /// </summary>
    [BindProperty]
    [StringLength(500, ErrorMessage = "Website URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid website URL.")]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets the current store record.
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

            if (Store != null)
            {
                // Pre-fill form with existing data
                Name = Store.Name;
                Description = Store.Description;
                LogoUrl = Store.LogoUrl;
                ContactEmail = Store.ContactEmail;
                ContactPhone = Store.ContactPhone;
                WebsiteUrl = Store.WebsiteUrl;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store profile for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your store profile. Please try again.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var command = new UpdateStoreProfileCommand
            {
                SellerId = sellerId,
                Name = Name,
                Description = Description,
                LogoUrl = LogoUrl,
                ContactEmail = ContactEmail,
                ContactPhone = ContactPhone,
                WebsiteUrl = WebsiteUrl
            };

            var result = await _storeProfileService.CreateOrUpdateStoreProfileAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Store profile updated for seller {SellerId}", sellerId);
                SuccessMessage = "Store profile updated successfully.";
                
                // Reload store data
                Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving store profile for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving your store profile. Please try again.");
            return Page();
        }
    }
}
