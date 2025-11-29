using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Services;
using Mercato.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Buyer.Addresses;

/// <summary>
/// Page model for creating a new delivery address.
/// </summary>
[Authorize(Roles = "Buyer")]
public class CreateModel : PageModel
{
    private readonly IDeliveryAddressService _deliveryAddressService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="deliveryAddressService">The delivery address service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IDeliveryAddressService deliveryAddressService,
        ILogger<CreateModel> logger)
    {
        _deliveryAddressService = deliveryAddressService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the label for the address (e.g., "Home", "Work").
    /// </summary>
    [BindProperty]
    [StringLength(50, ErrorMessage = "Label must be at most 50 characters.")]
    [Display(Name = "Label")]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary address line.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Address line 1 is required.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Address line 1 must be between 5 and 200 characters.")]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary address line.
    /// </summary>
    [BindProperty]
    [StringLength(200, ErrorMessage = "Address line 2 must be at most 200 characters.")]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    [BindProperty]
    [StringLength(100, ErrorMessage = "State must be at most 100 characters.")]
    [Display(Name = "State/Province")]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Postal code is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Postal code must be between 3 and 20 characters.")]
    [Display(Name = "Postal Code")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO country code.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Country is required.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Please select a valid country.")]
    [Display(Name = "Country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "Phone number must be between 7 and 20 characters.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to set this as the default address.
    /// </summary>
    [BindProperty]
    [Display(Name = "Set as default address")]
    public bool SetAsDefault { get; set; }

    /// <summary>
    /// Gets the list of allowed shipping countries.
    /// </summary>
    public List<SelectListItem> Countries { get; private set; } = [];

    /// <summary>
    /// Handles GET requests to display the create form.
    /// </summary>
    public IActionResult OnGet()
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            _logger.LogWarning("Buyer ID not found in claims");
            return RedirectToPage("/Account/Login");
        }

        _logger.LogInformation("Buyer {BuyerId} accessing create address page", buyerId);
        LoadCountries();
        return Page();
    }

    /// <summary>
    /// Handles POST requests to create a new address.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            _logger.LogWarning("Buyer ID not found in claims");
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            LoadCountries();
            return Page();
        }

        if (!_deliveryAddressService.IsShippingAllowedToRegion(Country))
        {
            ModelState.AddModelError(nameof(Country), $"Shipping to {Country} is not available. Please choose a different country.");
            LoadCountries();
            return Page();
        }

        _logger.LogInformation("Buyer {BuyerId} creating new delivery address", buyerId);

        try
        {
            var command = new SaveDeliveryAddressCommand
            {
                AddressId = null,
                BuyerId = buyerId,
                Label = Label,
                FullName = FullName,
                AddressLine1 = AddressLine1,
                AddressLine2 = AddressLine2,
                City = City,
                State = State,
                PostalCode = PostalCode,
                Country = Country,
                PhoneNumber = PhoneNumber,
                SetAsDefault = SetAsDefault
            };

            var result = await _deliveryAddressService.SaveAddressAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Delivery address created successfully for buyer {BuyerId}", buyerId);
                TempData["SuccessMessage"] = "Address added successfully.";
                return RedirectToPage("Index");
            }

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (result.IsRegionNotAllowed)
            {
                ModelState.AddModelError(nameof(Country), result.Errors.FirstOrDefault() ?? "Shipping to this region is not available.");
                LoadCountries();
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            LoadCountries();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery address for buyer {BuyerId}", buyerId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving the address. Please try again.");
            LoadCountries();
            return Page();
        }
    }

    private void LoadCountries()
    {
        Countries = CountryHelper.GetCountrySelectList(_deliveryAddressService.AllowedShippingCountries);
    }
}
