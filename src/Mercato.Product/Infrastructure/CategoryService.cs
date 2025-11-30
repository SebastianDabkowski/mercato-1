using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for category management operations.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly ILogger<CategoryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService"/> class.
    /// </summary>
    /// <param name="repository">The category repository.</param>
    /// <param name="logger">The logger.</param>
    public CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateCategoryResult> CreateCategoryAsync(CreateCategoryCommand command)
    {
        var validationErrors = await ValidateCreateCommandAsync(command);
        if (validationErrors.Count > 0)
        {
            return CreateCategoryResult.Failure(validationErrors);
        }

        try
        {
            // Generate slug from name
            var baseSlug = GenerateSlug(command.Name);
            var slug = await EnsureUniqueSlugAsync(baseSlug, null);

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Slug = slug,
                Description = command.Description,
                ParentId = command.ParentId,
                DisplayOrder = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            await _repository.AddAsync(category);

            _logger.LogInformation(
                "Category {CategoryId} created with name '{CategoryName}' and slug '{Slug}' under parent {ParentId}",
                category.Id,
                category.Name,
                category.Slug,
                command.ParentId?.ToString() ?? "root");

            return CreateCategoryResult.Success(category.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category '{CategoryName}'", command.Name);
            return CreateCategoryResult.Failure("An error occurred while creating the category.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync()
    {
        return await _repository.GetAllAsync();
    }

    /// <inheritdoc />
    public async Task<Category?> GetCategoryByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<Category?> GetCategoryBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return await _repository.GetBySlugAsync(slug);
    }

    /// <inheritdoc />
    public async Task<UpdateCategoryResult> UpdateCategoryAsync(UpdateCategoryCommand command)
    {
        var validationErrors = await ValidateUpdateCommandAsync(command);
        if (validationErrors.Count > 0)
        {
            return UpdateCategoryResult.Failure(validationErrors);
        }

        try
        {
            var category = await _repository.GetByIdAsync(command.CategoryId);
            if (category == null)
            {
                return UpdateCategoryResult.Failure("Category not found.");
            }

            // Prevent circular parent references
            if (command.ParentId.HasValue && await WouldCreateCircularReferenceAsync(command.CategoryId, command.ParentId.Value))
            {
                return UpdateCategoryResult.Failure("Cannot set parent category to a descendant of this category.");
            }

            // Store old slug for logging
            var oldSlug = category.Slug;

            category.Name = command.Name;
            category.Slug = command.Slug;
            category.Description = command.Description;
            category.ParentId = command.ParentId;
            category.DisplayOrder = command.DisplayOrder;
            category.IsActive = command.IsActive;
            category.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdateAsync(category);

            if (oldSlug != command.Slug)
            {
                _logger.LogInformation(
                    "Category {CategoryId} slug changed from '{OldSlug}' to '{NewSlug}'",
                    command.CategoryId,
                    oldSlug,
                    command.Slug);
            }

            _logger.LogInformation(
                "Category {CategoryId} updated with name '{CategoryName}'",
                command.CategoryId,
                command.Name);

            return UpdateCategoryResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", command.CategoryId);
            return UpdateCategoryResult.Failure("An error occurred while updating the category.");
        }
    }

    /// <inheritdoc />
    public async Task<DeleteCategoryResult> DeleteCategoryAsync(DeleteCategoryCommand command)
    {
        var validationErrors = ValidateDeleteCommand(command);
        if (validationErrors.Count > 0)
        {
            return DeleteCategoryResult.Failure(validationErrors);
        }

        try
        {
            var category = await _repository.GetByIdAsync(command.CategoryId);
            if (category == null)
            {
                return DeleteCategoryResult.Failure("Category not found.");
            }

            // Check if category has products assigned
            var productCount = await _repository.GetProductCountAsync(command.CategoryId);
            if (productCount > 0)
            {
                return DeleteCategoryResult.Failure(
                    $"Cannot delete category. There are {productCount} product(s) assigned to this category. " +
                    "Please reassign or remove the products first, or deactivate the category instead.");
            }

            // Check if category has child categories
            var childCount = await _repository.GetChildCountAsync(command.CategoryId);
            if (childCount > 0)
            {
                return DeleteCategoryResult.Failure(
                    $"Cannot delete category. There are {childCount} child categories under this category. " +
                    "Please delete or move the child categories first, or deactivate the category instead.");
            }

            await _repository.DeleteAsync(command.CategoryId);

            _logger.LogInformation(
                "Category {CategoryId} deleted",
                command.CategoryId);

            return DeleteCategoryResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", command.CategoryId);
            return DeleteCategoryResult.Failure("An error occurred while deleting the category.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetCategoriesByParentIdAsync(Guid? parentId)
    {
        return await _repository.GetByParentIdAsync(parentId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetActiveCategoriesByParentIdAsync(Guid? parentId)
    {
        return await _repository.GetActiveByParentIdAsync(parentId);
    }

    /// <inheritdoc />
    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return await _repository.GetByNameAsync(name);
    }

    private async Task<List<string>> ValidateCreateCommandAsync(CreateCategoryCommand command)
    {
        var errors = new List<string>();

        ValidateName(command.Name, errors);
        ValidateDescription(command.Description, errors);

        // Return early if there are basic validation errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Validate parent exists if provided
        if (command.ParentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(command.ParentId.Value);
            if (parent == null)
            {
                errors.Add("Parent category not found.");
                return errors;
            }
        }

        // Check name uniqueness within parent
        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            var exists = await _repository.ExistsByNameAsync(command.Name, command.ParentId);
            if (exists)
            {
                errors.Add("A category with this name already exists under the same parent.");
            }
        }

        return errors;
    }

    private async Task<List<string>> ValidateUpdateCommandAsync(UpdateCategoryCommand command)
    {
        var errors = new List<string>();

        if (command.CategoryId == Guid.Empty)
        {
            errors.Add("Category ID is required.");
        }

        ValidateName(command.Name, errors);
        ValidateSlug(command.Slug, errors);
        ValidateDescription(command.Description, errors);

        if (command.DisplayOrder < 0)
        {
            errors.Add("Display order cannot be negative.");
        }

        // Validate parent exists if provided
        if (command.ParentId.HasValue)
        {
            if (command.ParentId.Value == command.CategoryId)
            {
                errors.Add("A category cannot be its own parent.");
            }
        }

        // Return early if there are basic validation errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Async validations that require database calls
        if (command.ParentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(command.ParentId.Value);
            if (parent == null)
            {
                errors.Add("Parent category not found.");
            }
        }

        // Check name uniqueness within parent (excluding current category)
        if (!string.IsNullOrWhiteSpace(command.Name) && command.CategoryId != Guid.Empty)
        {
            var exists = await _repository.ExistsByNameAsync(command.Name, command.ParentId, command.CategoryId);
            if (exists)
            {
                errors.Add("A category with this name already exists under the same parent.");
            }
        }

        // Check slug uniqueness (excluding current category)
        if (!string.IsNullOrWhiteSpace(command.Slug) && command.CategoryId != Guid.Empty)
        {
            var slugExists = await _repository.ExistsBySlugAsync(command.Slug, command.CategoryId);
            if (slugExists)
            {
                errors.Add("A category with this slug already exists. Please choose a different slug.");
            }
        }

        return errors;
    }

    private static List<string> ValidateDeleteCommand(DeleteCategoryCommand command)
    {
        var errors = new List<string>();

        if (command.CategoryId == Guid.Empty)
        {
            errors.Add("Category ID is required.");
        }

        return errors;
    }

    private static void ValidateName(string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }
        else if (name.Length < ProductValidationConstants.CategoryNameMinLength ||
                 name.Length > ProductValidationConstants.CategoryNameMaxLength)
        {
            errors.Add($"Name must be between {ProductValidationConstants.CategoryNameMinLength} and {ProductValidationConstants.CategoryNameMaxLength} characters.");
        }
    }

    private static void ValidateSlug(string slug, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            errors.Add("Slug is required.");
            return;
        }

        if (slug.Length < ProductValidationConstants.CategorySlugMinLength ||
            slug.Length > ProductValidationConstants.CategorySlugMaxLength)
        {
            errors.Add($"Slug must be between {ProductValidationConstants.CategorySlugMinLength} and {ProductValidationConstants.CategorySlugMaxLength} characters.");
            return;
        }

        // Validate slug format (lowercase, alphanumeric, hyphens only)
        if (!Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            errors.Add("Slug must be lowercase, alphanumeric, and use hyphens to separate words.");
        }
    }

    private static void ValidateDescription(string? description, List<string> errors)
    {
        if (description != null && description.Length > ProductValidationConstants.CategoryDescriptionMaxLength)
        {
            errors.Add($"Description cannot exceed {ProductValidationConstants.CategoryDescriptionMaxLength} characters.");
        }
    }

    /// <summary>
    /// Generates a URL-friendly slug from a given name.
    /// </summary>
    /// <param name="name">The name to convert to a slug.</param>
    /// <returns>A lowercase, hyphenated slug.</returns>
    internal static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // Convert to lowercase
        var slug = name.ToLowerInvariant();

        // Normalize unicode characters and remove diacritics
        slug = RemoveDiacritics(slug);

        // Replace spaces and underscores with hyphens
        slug = Regex.Replace(slug, @"[\s_]+", "-");

        // Remove any characters that are not alphanumeric or hyphen
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Replace multiple consecutive hyphens with single hyphen
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Ensure minimum length
        if (slug.Length < ProductValidationConstants.CategorySlugMinLength)
        {
            slug = slug.PadRight(ProductValidationConstants.CategorySlugMinLength, '0');
        }

        // Truncate if too long
        if (slug.Length > ProductValidationConstants.CategorySlugMaxLength)
        {
            slug = slug.Substring(0, ProductValidationConstants.CategorySlugMaxLength).TrimEnd('-');
        }

        return slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? excludeCategoryId)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await _repository.ExistsBySlugAsync(slug, excludeCategoryId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;

            // Safety check to prevent infinite loops
            if (counter > 100)
            {
                slug = $"{baseSlug}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                break;
            }
        }

        return slug;
    }

    private async Task<bool> WouldCreateCircularReferenceAsync(Guid categoryId, Guid newParentId)
    {
        // Load all categories once to avoid N+1 query pattern
        var allCategories = await _repository.GetAllAsync();
        var categoryLookup = allCategories.ToDictionary(c => c.Id);

        // Check if newParentId is a descendant of categoryId by traversing up the tree
        var currentId = newParentId;
        var visited = new HashSet<Guid> { categoryId };

        while (currentId != Guid.Empty)
        {
            if (visited.Contains(currentId))
            {
                return true;
            }

            visited.Add(currentId);

            if (!categoryLookup.TryGetValue(currentId, out var current) || current.ParentId == null)
            {
                break;
            }

            currentId = current.ParentId.Value;
        }

        return false;
    }
}
