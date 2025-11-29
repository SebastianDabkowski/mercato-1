using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Buyer.Addresses;

/// <summary>
/// Page model for listing and managing buyer delivery addresses.
/// </summary>
[Authorize(Roles = "Buyer")]
public class IndexModel : PageModel
{
    private readonly IDeliveryAddressService _deliveryAddressService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="deliveryAddressService">The delivery address service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IDeliveryAddressService deliveryAddressService,
        ILogger<IndexModel> logger)
    {
        _deliveryAddressService = deliveryAddressService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of delivery addresses.
    /// </summary>
    public IReadOnlyList<DeliveryAddressDto> Addresses { get; set; } = [];

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load all addresses.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            _logger.LogWarning("Buyer ID not found in claims");
            return RedirectToPage("/Account/Login");
        }

        _logger.LogInformation("Buyer {BuyerId} accessing delivery address list", buyerId);

        SuccessMessage = TempData["SuccessMessage"]?.ToString();
        ErrorMessage = TempData["ErrorMessage"]?.ToString();

        try
        {
            var query = new GetDeliveryAddressesQuery { BuyerId = buyerId };
            var result = await _deliveryAddressService.GetAddressesAsync(query);

            if (result.Succeeded)
            {
                Addresses = result.Addresses;
            }
            else
            {
                ErrorMessage = string.Join(" ", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading delivery addresses for buyer {BuyerId}", buyerId);
            ErrorMessage = "An error occurred while loading your addresses. Please try again.";
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to set an address as default.
    /// </summary>
    /// <param name="id">The address ID to set as default.</param>
    public async Task<IActionResult> OnPostSetDefaultAsync(Guid id)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            _logger.LogWarning("Buyer ID not found in claims");
            return RedirectToPage("/Account/Login");
        }

        _logger.LogInformation("Buyer {BuyerId} setting address {AddressId} as default", buyerId, id);

        try
        {
            var command = new SetDefaultDeliveryAddressCommand
            {
                AddressId = id,
                BuyerId = buyerId
            };

            var result = await _deliveryAddressService.SetDefaultAddressAsync(command);

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Default address updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default address {AddressId} for buyer {BuyerId}", id, buyerId);
            TempData["ErrorMessage"] = "An error occurred while updating the default address. Please try again.";
        }

        return RedirectToPage();
    }
}
