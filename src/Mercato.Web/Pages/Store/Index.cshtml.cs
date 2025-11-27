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

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
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
    /// Gets whether the store is unavailable (suspended or pending verification).
    /// </summary>
    public bool IsUnavailable { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the public store page.
    /// </summary>
    /// <param name="slug">The store's SEO-friendly URL slug.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            IsNotFound = true;
            return Page();
        }

        try
        {
            Store = await _storeProfileService.GetPublicStoreBySlugAsync(slug);

            if (Store == null)
            {
                // Check if store exists but is unavailable (suspended or pending verification)
                var storeExists = await _storeProfileService.StoreExistsBySlugAsync(slug);
                if (storeExists)
                {
                    IsUnavailable = true;
                }
                else
                {
                    IsNotFound = true;
                }
            }

            return Page();
        }
        catch (ArgumentException)
        {
            IsNotFound = true;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store with slug {Slug}", slug);
            ErrorMessage = "An error occurred while loading the store. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Gets a sanitized URL that only allows http and https schemes.
    /// Returns null if the URL is invalid or uses an unsafe scheme.
    /// </summary>
    /// <param name="url">The URL to sanitize.</param>
    /// <returns>The sanitized URL or null if invalid.</returns>
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
