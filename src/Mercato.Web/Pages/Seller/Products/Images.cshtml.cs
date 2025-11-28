using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Products;

/// <summary>
/// Page model for managing product images.
/// </summary>
[Authorize(Roles = "Seller")]
public class ImagesModel : PageModel
{
    private readonly IProductImageService _imageService;
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ImagesModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImagesModel"/> class.
    /// </summary>
    public ImagesModel(
        IProductImageService imageService,
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<ImagesModel> logger)
    {
        _imageService = imageService;
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of product images.
    /// </summary>
    public IReadOnlyList<ProductImage> Images { get; set; } = [];

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets a success message.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets whether the user has access to this product.
    /// </summary>
    public bool HasAccess { get; private set; }

    /// <summary>
    /// Handles GET requests to display product images.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                HasAccess = false;
                ErrorMessage = "You must configure your store profile first.";
                return Page();
            }

            var product = await _productService.GetProductByIdAsync(ProductId);
            if (product == null)
            {
                HasAccess = false;
                ErrorMessage = "Product not found.";
                return Page();
            }

            if (product.StoreId != store.Id)
            {
                return Forbid();
            }

            HasAccess = true;
            ProductTitle = product.Title;
            Images = await _imageService.GetImagesByProductIdAsync(ProductId);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading images for product {ProductId}", ProductId);
            HasAccess = false;
            ErrorMessage = "An error occurred while loading images.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to upload a new image.
    /// </summary>
    public async Task<IActionResult> OnPostUploadAsync(IFormFile file, bool setAsMain = false)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return new JsonResult(new { success = false, error = "Not authenticated" }) { StatusCode = 401 };
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return new JsonResult(new { success = false, error = "Store not configured" }) { StatusCode = 400 };
            }

            if (file == null || file.Length == 0)
            {
                return new JsonResult(new { success = false, error = "No file provided" }) { StatusCode = 400 };
            }

            var command = new UploadProductImageCommand
            {
                ProductId = ProductId,
                StoreId = store.Id,
                SellerId = sellerId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileStream = file.OpenReadStream(),
                SetAsMain = setAsMain
            };

            var result = await _imageService.UploadImageAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Image {ImageId} uploaded for product {ProductId}", result.ImageId, ProductId);
                return new JsonResult(new
                {
                    success = true,
                    imageId = result.ImageId,
                    imageUrl = result.ImageUrl,
                    thumbnailUrl = result.ThumbnailUrl,
                    optimizedUrl = result.OptimizedUrl
                });
            }

            if (result.IsNotAuthorized)
            {
                return new JsonResult(new { success = false, error = "Not authorized" }) { StatusCode = 403 };
            }

            return new JsonResult(new { success = false, errors = result.Errors }) { StatusCode = 400 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for product {ProductId}", ProductId);
            return new JsonResult(new { success = false, error = "An error occurred while uploading the image" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Handles POST requests to delete an image.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid imageId)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return new JsonResult(new { success = false, error = "Not authenticated" }) { StatusCode = 401 };
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return new JsonResult(new { success = false, error = "Store not configured" }) { StatusCode = 400 };
            }

            var command = new DeleteProductImageCommand
            {
                ImageId = imageId,
                ProductId = ProductId,
                StoreId = store.Id,
                SellerId = sellerId
            };

            var result = await _imageService.DeleteImageAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Image {ImageId} deleted for product {ProductId}", imageId, ProductId);
                return new JsonResult(new { success = true });
            }

            if (result.IsNotAuthorized)
            {
                return new JsonResult(new { success = false, error = "Not authorized" }) { StatusCode = 403 };
            }

            return new JsonResult(new { success = false, errors = result.Errors }) { StatusCode = 400 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId} for product {ProductId}", imageId, ProductId);
            return new JsonResult(new { success = false, error = "An error occurred while deleting the image" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Handles POST requests to set an image as main.
    /// </summary>
    public async Task<IActionResult> OnPostSetMainAsync(Guid imageId)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return new JsonResult(new { success = false, error = "Not authenticated" }) { StatusCode = 401 };
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return new JsonResult(new { success = false, error = "Store not configured" }) { StatusCode = 400 };
            }

            var command = new SetMainProductImageCommand
            {
                ImageId = imageId,
                ProductId = ProductId,
                StoreId = store.Id,
                SellerId = sellerId
            };

            var result = await _imageService.SetMainImageAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Image {ImageId} set as main for product {ProductId}", imageId, ProductId);
                return new JsonResult(new { success = true });
            }

            if (result.IsNotAuthorized)
            {
                return new JsonResult(new { success = false, error = "Not authorized" }) { StatusCode = 403 };
            }

            return new JsonResult(new { success = false, errors = result.Errors }) { StatusCode = 400 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting main image {ImageId} for product {ProductId}", imageId, ProductId);
            return new JsonResult(new { success = false, error = "An error occurred while setting the main image" }) { StatusCode = 500 };
        }
    }
}
