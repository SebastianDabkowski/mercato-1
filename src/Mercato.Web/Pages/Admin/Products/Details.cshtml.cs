using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Products;

/// <summary>
/// Page model for viewing and moderating individual product details.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IProductModerationService _productModerationService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="productModerationService">The product moderation service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IProductModerationService productModerationService,
        ILogger<DetailsModel> logger)
    {
        _productModerationService = productModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the product details.
    /// </summary>
    public ProductModerationDetails? ProductDetails { get; private set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the rejection reason.
    /// </summary>
    [BindProperty]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets the approval reason.
    /// </summary>
    [BindProperty]
    public string? ApprovalReason { get; set; }

    /// <summary>
    /// Handles GET requests to load product details.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _productModerationService.GetProductDetailsAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        ProductDetails = result.ProductDetails;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to approve a product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to approve product {ProductId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new ApproveProductCommand
        {
            ProductId = id,
            AdminUserId = userId,
            Reason = ApprovalReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _productModerationService.ApproveProductAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Product has been approved successfully. The seller has been notified.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Handles POST requests to reject a product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RejectionReason))
        {
            ErrorMessage = "Rejection reason is required.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims when attempting to reject product {ProductId}", id);
            ErrorMessage = "Unable to identify admin user. Please re-authenticate and try again.";
            return await OnGetAsync(id);
        }

        var command = new RejectProductCommand
        {
            ProductId = id,
            AdminUserId = userId,
            Reason = RejectionReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _productModerationService.RejectProductAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return await OnGetAsync(id);
        }

        SuccessMessage = "Product has been rejected. The seller has been notified with the reason.";
        return await OnGetAsync(id);
    }

    /// <summary>
    /// Gets the CSS class for a moderation status badge.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetModerationStatusBadgeClass(ProductModerationStatus status) => 
        ProductModerationDisplayHelpers.GetModerationStatusBadgeClass(status);

    /// <summary>
    /// Gets the display text for a moderation status.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The display text.</returns>
    public static string GetModerationStatusDisplayText(ProductModerationStatus status) => 
        ProductModerationDisplayHelpers.GetModerationStatusDisplayText(status);

    /// <summary>
    /// Gets the CSS class for a product status badge.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetProductStatusBadgeClass(ProductStatus status) => 
        ProductModerationDisplayHelpers.GetProductStatusBadgeClass(status);

    /// <summary>
    /// Gets the display text for a product status.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The display text.</returns>
    public static string GetProductStatusDisplayText(ProductStatus status) => 
        ProductModerationDisplayHelpers.GetProductStatusDisplayText(status);
}
