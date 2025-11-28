using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Pages.Admin.Categories;

/// <summary>
/// Page model for creating a new product category.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        ICategoryService categoryService,
        ILogger<CreateModel> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the create category command.
    /// </summary>
    [BindProperty]
    public CreateCategoryCommand Command { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of available parent categories.
    /// </summary>
    public List<SelectListItem> ParentCategories { get; set; } = [];

    /// <summary>
    /// Handles GET requests to display the create form.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing create category page");
        await LoadParentCategoriesAsync();
    }

    /// <summary>
    /// Handles POST requests to create a new category.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync();
            return Page();
        }

        _logger.LogInformation("Admin creating category with name '{CategoryName}'", Command.Name);

        var result = await _categoryService.CreateCategoryAsync(Command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Category '{Command.Name}' has been created successfully.";
            return RedirectToPage("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await LoadParentCategoriesAsync();
        return Page();
    }

    private async Task LoadParentCategoriesAsync()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        ParentCategories = categories
            .Where(c => c.IsActive)
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
}
