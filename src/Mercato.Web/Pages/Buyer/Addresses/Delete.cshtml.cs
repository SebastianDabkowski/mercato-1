using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Buyer.Addresses;

/// <summary>
/// Page model for deleting a delivery address.
/// </summary>
[Authorize(Roles = "Buyer")]
public class DeleteModel : PageModel
{
    private readonly IDeliveryAddressService _deliveryAddressService;
    private readonly ILogger<DeleteModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteModel"/> class.
    /// </summary>
    /// <param name="deliveryAddressService">The delivery address service.</param>
    /// <param name="logger">The logger.</param>
    public DeleteModel(
        IDeliveryAddressService deliveryAddressService,
        ILogger<DeleteModel> logger)
    {
        _deliveryAddressService = deliveryAddressService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the address ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the address details for display.
    /// </summary>
    public DeliveryAddressDto? Address { get; private set; }

    /// <summary>
    /// Gets or sets whether the address was found.
    /// </summary>
    public bool AddressFound { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests to display the delete confirmation.
    /// </summary>
    /// <param name="id">The address ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            _logger.LogWarning("Buyer ID not found in claims");
            return RedirectToPage("/Account/Login");
        }

        _logger.LogInformation("Buyer {BuyerId} accessing delete address page for {AddressId}", buyerId, id);

        Id = id;

        try
        {
            var query = new GetDeliveryAddressesQuery { BuyerId = buyerId };
            var result = await _deliveryAddressService.GetAddressesAsync(query);

            if (!result.Succeeded)
            {
                AddressFound = false;
                ErrorMessage = string.Join(" ", result.Errors);
                return Page();
            }

            Address = result.Addresses.FirstOrDefault(a => a.Id == id);
            if (Address == null)
            {
                _logger.LogWarning("Address {AddressId} not found for buyer {BuyerId}", id, buyerId);
                return NotFound();
            }

            AddressFound = true;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading address {AddressId} for buyer {BuyerId}", id, buyerId);
            AddressFound = false;
            ErrorMessage = "An error occurred while loading the address. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to delete the address.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            _logger.LogWarning("Buyer ID not found in claims");
            return RedirectToPage("/Account/Login");
        }

        _logger.LogInformation("Buyer {BuyerId} deleting delivery address {AddressId}", buyerId, Id);

        try
        {
            var command = new DeleteDeliveryAddressCommand
            {
                AddressId = Id,
                BuyerId = buyerId
            };

            var result = await _deliveryAddressService.DeleteAddressAsync(command);

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (result.Succeeded)
            {
                _logger.LogInformation("Delivery address {AddressId} deleted successfully for buyer {BuyerId}", Id, buyerId);
                TempData["SuccessMessage"] = "Address deleted successfully.";
                return RedirectToPage("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            // Reload address for display
            await ReloadAddressAsync(buyerId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery address {AddressId} for buyer {BuyerId}", Id, buyerId);
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the address. Please try again.");
            await ReloadAddressAsync(buyerId);
            return Page();
        }
    }

    private async Task ReloadAddressAsync(string buyerId)
    {
        try
        {
            var query = new GetDeliveryAddressesQuery { BuyerId = buyerId };
            var result = await _deliveryAddressService.GetAddressesAsync(query);

            if (result.Succeeded)
            {
                Address = result.Addresses.FirstOrDefault(a => a.Id == Id);
                AddressFound = Address != null;
            }
            else
            {
                AddressFound = false;
            }
        }
        catch
        {
            AddressFound = false;
        }
    }
}
