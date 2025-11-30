using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Pages.Admin.Categories.Attributes;

/// <summary>
/// Page model for editing an existing category attribute.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ICategoryAttributeService _attributeService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="attributeService">The category attribute service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        ICategoryAttributeService attributeService,
        ICategoryService categoryService,
        ILogger<EditModel> logger)
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
    /// Gets or sets the update attribute command.
    /// </summary>
    [BindProperty]
    public UpdateCategoryAttributeCommand Command { get; set; } = new();

    /// <summary>
    /// Gets or sets the category ID for routing.
    /// </summary>
    [BindProperty]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the available attribute types.
    /// </summary>
    public List<SelectListItem> AttributeTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the attribute created date for display.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the attribute last updated date for display.
    /// </summary>
    public DateTimeOffset? LastUpdatedAt { get; set; }

    /// <summary>
    /// Handles GET requests to display the edit form.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="attributeId">The attribute ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid categoryId, Guid attributeId)
    {
        _logger.LogInformation("Admin accessing edit attribute page for attribute {AttributeId} in category {CategoryId}", attributeId, categoryId);

        Category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (Category == null)
        {
            return NotFound();
        }

        var attribute = await _attributeService.GetAttributeByIdAsync(attributeId);
        if (attribute == null || attribute.CategoryId != categoryId)
        {
            return NotFound();
        }

        CategoryId = categoryId;
        Command = new UpdateCategoryAttributeCommand
        {
            AttributeId = attribute.Id,
            Name = attribute.Name,
            Type = attribute.Type,
            IsRequired = attribute.IsRequired,
            IsDeprecated = attribute.IsDeprecated,
            ListOptions = attribute.ListOptions,
            DisplayOrder = attribute.DisplayOrder
        };

        CreatedAt = attribute.CreatedAt;
        LastUpdatedAt = attribute.LastUpdatedAt;

        LoadAttributeTypes();

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the attribute.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Category = await _categoryService.GetCategoryByIdAsync(CategoryId);
            LoadAttributeTypes();
            await LoadDatesAsync(Command.AttributeId);
            return Page();
        }

        _logger.LogInformation("Admin updating attribute {AttributeId}", Command.AttributeId);

        var result = await _attributeService.UpdateAttributeAsync(Command);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Attribute '{Command.Name}' has been updated successfully.";
            return RedirectToPage("Index", new { categoryId = CategoryId });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        Category = await _categoryService.GetCategoryByIdAsync(CategoryId);
        LoadAttributeTypes();
        await LoadDatesAsync(Command.AttributeId);
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

    private async Task LoadDatesAsync(Guid attributeId)
    {
        var attribute = await _attributeService.GetAttributeByIdAsync(attributeId);
        if (attribute != null)
        {
            CreatedAt = attribute.CreatedAt;
            LastUpdatedAt = attribute.LastUpdatedAt;
        }
    }
}
