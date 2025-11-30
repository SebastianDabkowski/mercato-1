using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Pages.Admin.Categories.Attributes;

/// <summary>
/// Page model for creating a new category attribute.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ICategoryAttributeService _attributeService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="attributeService">The category attribute service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        ICategoryAttributeService attributeService,
        ICategoryService categoryService,
        ILogger<CreateModel> logger)
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
    /// Gets or sets the create attribute command.
    /// </summary>
    [BindProperty]
    public CreateCategoryAttributeCommand Command { get; set; } = new();

    /// <summary>
    /// Gets or sets the available attribute types.
    /// </summary>
    public List<SelectListItem> AttributeTypes { get; set; } = [];

    /// <summary>
    /// Handles GET requests to display the create form.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid categoryId)
    {
        _logger.LogInformation("Admin accessing create attribute page for category {CategoryId}", categoryId);

        Category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (Category == null)
        {
            return NotFound();
        }

        Command.CategoryId = categoryId;
        LoadAttributeTypes();

        return Page();
    }

    /// <summary>
    /// Handles POST requests to create the attribute.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Category = await _categoryService.GetCategoryByIdAsync(Command.CategoryId);
            LoadAttributeTypes();
            return Page();
        }

        _logger.LogInformation("Admin creating attribute for category {CategoryId}", Command.CategoryId);

        var result = await _attributeService.CreateAttributeAsync(Command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Attribute '{Command.Name}' has been created successfully.";
            return RedirectToPage("Index", new { categoryId = Command.CategoryId });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        Category = await _categoryService.GetCategoryByIdAsync(Command.CategoryId);
        LoadAttributeTypes();
        return Page();
    }

    private void LoadAttributeTypes()
    {
        AttributeTypes =
        [
            new SelectListItem("Text - Free-form text input", ((int)CategoryAttributeType.Text).ToString()),
            new SelectListItem("Number - Numeric value input", ((int)CategoryAttributeType.Number).ToString()),
            new SelectListItem("List - Selection from predefined options", ((int)CategoryAttributeType.List).ToString())
        ];
    }
}
