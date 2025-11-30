using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Categories.Attributes;

/// <summary>
/// Page model for listing and managing category attributes.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    /// <summary>
    /// Maximum length for list options preview text.
    /// </summary>
    public const int ListOptionsPreviewMaxLength = 50;

    private readonly ICategoryAttributeService _attributeService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="attributeService">The category attribute service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        ICategoryAttributeService attributeService,
        ICategoryService categoryService,
        ILogger<IndexModel> logger)
    {
        _attributeService = attributeService;
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the category being managed.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the list of attributes for the category.
    /// </summary>
    public IReadOnlyList<CategoryAttribute> Attributes { get; set; } = [];

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load category attributes.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid categoryId)
    {
        _logger.LogInformation("Admin accessing attribute management for category {CategoryId}", categoryId);

        Category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (Category == null)
        {
            return NotFound();
        }

        Attributes = await _attributeService.GetAttributesByCategoryIdAsync(categoryId);

        SuccessMessage = TempData["SuccessMessage"]?.ToString();
        ErrorMessage = TempData["ErrorMessage"]?.ToString();

        return Page();
    }

    /// <summary>
    /// Handles POST requests to deprecate an attribute.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="attributeId">The attribute ID.</param>
    public async Task<IActionResult> OnPostDeprecateAsync(Guid categoryId, Guid attributeId)
    {
        _logger.LogInformation("Admin deprecating attribute {AttributeId} in category {CategoryId}", attributeId, categoryId);

        var result = await _attributeService.DeprecateAttributeAsync(attributeId);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Attribute has been deprecated. It will be hidden from new product creation.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage(new { categoryId });
    }

    /// <summary>
    /// Handles POST requests to restore a deprecated attribute.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="attributeId">The attribute ID.</param>
    public async Task<IActionResult> OnPostRestoreAsync(Guid categoryId, Guid attributeId)
    {
        _logger.LogInformation("Admin restoring attribute {AttributeId} in category {CategoryId}", attributeId, categoryId);

        var result = await _attributeService.RestoreAttributeAsync(attributeId);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Attribute has been restored and will appear in new product creation.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage(new { categoryId });
    }

    /// <summary>
    /// Handles POST requests to delete an attribute.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="attributeId">The attribute ID.</param>
    public async Task<IActionResult> OnPostDeleteAsync(Guid categoryId, Guid attributeId)
    {
        _logger.LogInformation("Admin deleting attribute {AttributeId} in category {CategoryId}", attributeId, categoryId);

        var command = new DeleteCategoryAttributeCommand
        {
            AttributeId = attributeId
        };

        var result = await _attributeService.DeleteAttributeAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Attribute has been deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage(new { categoryId });
    }
}
