using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Pages.Admin.Categories;

/// <summary>
/// Page model for editing an existing product category.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        ICategoryService categoryService,
        ILogger<EditModel> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the update category command.
    /// </summary>
    [BindProperty]
    public UpdateCategoryCommand Command { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of available parent categories.
    /// </summary>
    public List<SelectListItem> ParentCategories { get; set; } = [];

    /// <summary>
    /// Gets or sets the category created date for display.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the category last updated date for display.
    /// </summary>
    public DateTimeOffset? LastUpdatedAt { get; set; }

    /// <summary>
    /// Handles GET requests to display the edit form.
    /// </summary>
    /// <param name="id">The category ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        _logger.LogInformation("Admin accessing edit category page for {CategoryId}", id);

        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            _logger.LogWarning("Category {CategoryId} not found", id);
            return NotFound();
        }

        Command = new UpdateCategoryCommand
        {
            CategoryId = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };

        CreatedAt = category.CreatedAt;
        LastUpdatedAt = category.LastUpdatedAt;

        await LoadParentCategoriesAsync(id);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the category.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync(Command.CategoryId);
            await LoadDatesAsync(Command.CategoryId);
            return Page();
        }

        _logger.LogInformation("Admin updating category {CategoryId}", Command.CategoryId);

        var result = await _categoryService.UpdateCategoryAsync(Command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Category '{Command.Name}' has been updated successfully.";
            return RedirectToPage("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await LoadParentCategoriesAsync(Command.CategoryId);
        await LoadDatesAsync(Command.CategoryId);
        return Page();
    }

    private async Task LoadParentCategoriesAsync(Guid excludeCategoryId)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        
        // Exclude the current category and its descendants from parent options
        var excludeIds = await GetDescendantIdsAsync(excludeCategoryId, categories);
        excludeIds.Add(excludeCategoryId);

        ParentCategories = categories
            .Where(c => c.IsActive && !excludeIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.ParentId.HasValue 
                    ? $"  └─ {c.Name}" 
                    : c.Name
            })
            .ToList();
    }

    private static Task<HashSet<Guid>> GetDescendantIdsAsync(
        Guid parentId, 
        IReadOnlyList<Mercato.Product.Domain.Entities.Category> allCategories)
    {
        var descendants = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = allCategories.Where(c => c.ParentId == currentId);
            
            foreach (var child in children)
            {
                descendants.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return Task.FromResult(descendants);
    }

    private async Task LoadDatesAsync(Guid categoryId)
    {
        var category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (category != null)
        {
            CreatedAt = category.CreatedAt;
            LastUpdatedAt = category.LastUpdatedAt;
        }
    }
}
