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
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                ParentId = command.ParentId,
                DisplayOrder = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            await _repository.AddAsync(category);

            _logger.LogInformation(
                "Category {CategoryId} created with name '{CategoryName}' under parent {ParentId}",
                category.Id,
                category.Name,
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

            category.Name = command.Name;
            category.ParentId = command.ParentId;
            category.DisplayOrder = command.DisplayOrder;
            category.IsActive = command.IsActive;
            category.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdateAsync(category);

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

    private async Task<List<string>> ValidateCreateCommandAsync(CreateCategoryCommand command)
    {
        var errors = new List<string>();

        ValidateName(command.Name, errors);

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
