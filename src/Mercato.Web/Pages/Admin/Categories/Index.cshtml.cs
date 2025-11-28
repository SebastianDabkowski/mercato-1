using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Categories;

/// <summary>
/// Page model for listing and managing all product categories.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        ICategoryService categoryService,
        ILogger<IndexModel> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of categories.
    /// </summary>
    public IReadOnlyList<Category> Categories { get; set; } = [];

    /// <summary>
    /// Gets the category lookup dictionary for efficient parent name resolution.
    /// </summary>
    public Dictionary<Guid, Category> CategoryLookup { get; private set; } = [];

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load all categories.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing category management");

        Categories = await _categoryService.GetAllCategoriesAsync();
        CategoryLookup = Categories.ToDictionary(c => c.Id);

        SuccessMessage = TempData["SuccessMessage"]?.ToString();
        ErrorMessage = TempData["ErrorMessage"]?.ToString();
    }

    /// <summary>
    /// Handles POST requests to deactivate a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        _logger.LogInformation("Admin deactivating category {CategoryId}", id);

        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToPage();
        }

        var command = new UpdateCategoryCommand
        {
            CategoryId = id,
            Name = category.Name,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            IsActive = false
        };

        var result = await _categoryService.UpdateCategoryAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Category '{category.Name}' has been deactivated.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST requests to activate a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        _logger.LogInformation("Admin activating category {CategoryId}", id);

        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToPage();
        }

        var command = new UpdateCategoryCommand
        {
            CategoryId = id,
            Name = category.Name,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            IsActive = true
        };

        var result = await _categoryService.UpdateCategoryAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Category '{category.Name}' has been activated.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST requests to delete a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        _logger.LogInformation("Admin deleting category {CategoryId}", id);

        var command = new DeleteCategoryCommand
        {
            CategoryId = id
        };

        var result = await _categoryService.DeleteCategoryAsync(command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Category has been deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }
}
