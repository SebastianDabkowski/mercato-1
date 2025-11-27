using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Store;

/// <summary>
/// Page model for the public store view page.
/// </summary>
[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IStoreProfileService storeProfileService,
        ILogger<IndexModel> logger)
    {
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the store being displayed.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets whether the store was not found.
    /// </summary>
    public bool IsNotFound { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Store = await _storeProfileService.GetStoreByIdAsync(id);

            if (Store == null)
            {
                IsNotFound = true;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store {StoreId}", id);
            ErrorMessage = "An error occurred while loading the store. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Gets a sanitized URL that only allows http and https schemes.
    /// Returns null if the URL is invalid or uses an unsafe scheme.
    /// </summary>
    public string? GetSafeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.AbsoluteUri;
        }

        return null;
    }
}
