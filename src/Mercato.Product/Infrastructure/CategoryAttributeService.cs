using System.Text.Json;
using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for category attribute management operations.
/// </summary>
public class CategoryAttributeService : ICategoryAttributeService
{
    private readonly ICategoryAttributeRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryAttributeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryAttributeService"/> class.
    /// </summary>
    /// <param name="repository">The category attribute repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="logger">The logger.</param>
    public CategoryAttributeService(
        ICategoryAttributeRepository repository,
        ICategoryRepository categoryRepository,
        ILogger<CategoryAttributeService> logger)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateCategoryAttributeResult> CreateAttributeAsync(CreateCategoryAttributeCommand command)
    {
        var validationErrors = await ValidateCreateCommandAsync(command);
        if (validationErrors.Count > 0)
        {
            return CreateCategoryAttributeResult.Failure(validationErrors);
        }

        try
        {
            var attribute = new CategoryAttribute
            {
                Id = Guid.NewGuid(),
                CategoryId = command.CategoryId,
                Name = command.Name,
                Type = command.Type,
                IsRequired = command.IsRequired,
                IsDeprecated = false,
                ListOptions = command.Type == CategoryAttributeType.List ? command.ListOptions : null,
                DisplayOrder = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            await _repository.AddAsync(attribute);

            _logger.LogInformation(
                "Category attribute {AttributeId} created with name '{AttributeName}' for category {CategoryId}",
                attribute.Id,
                attribute.Name,
                command.CategoryId);

            return CreateCategoryAttributeResult.Success(attribute.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category attribute '{AttributeName}' for category {CategoryId}",
                command.Name, command.CategoryId);
            return CreateCategoryAttributeResult.Failure("An error occurred while creating the attribute.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryAttribute>> GetAttributesByCategoryIdAsync(Guid categoryId)
    {
        return await _repository.GetByCategoryIdAsync(categoryId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryAttribute>> GetActiveAttributesByCategoryIdAsync(Guid categoryId)
    {
        return await _repository.GetActiveByCategoryIdAsync(categoryId);
    }

    /// <inheritdoc />
    public async Task<CategoryAttribute?> GetAttributeByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<UpdateCategoryAttributeResult> UpdateAttributeAsync(UpdateCategoryAttributeCommand command)
    {
        var validationErrors = await ValidateUpdateCommandAsync(command);
        if (validationErrors.Count > 0)
        {
            return UpdateCategoryAttributeResult.Failure(validationErrors);
        }

        try
        {
            var attribute = await _repository.GetByIdAsync(command.AttributeId);
            if (attribute == null)
            {
                return UpdateCategoryAttributeResult.Failure("Attribute not found.");
            }

            attribute.Name = command.Name;
            attribute.Type = command.Type;
            attribute.IsRequired = command.IsRequired;
            attribute.IsDeprecated = command.IsDeprecated;
            attribute.ListOptions = command.Type == CategoryAttributeType.List ? command.ListOptions : null;
            attribute.DisplayOrder = command.DisplayOrder;
            attribute.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdateAsync(attribute);

            _logger.LogInformation(
                "Category attribute {AttributeId} updated with name '{AttributeName}'",
                command.AttributeId,
                command.Name);

            return UpdateCategoryAttributeResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category attribute {AttributeId}", command.AttributeId);
            return UpdateCategoryAttributeResult.Failure("An error occurred while updating the attribute.");
        }
    }

    /// <inheritdoc />
    public async Task<DeleteCategoryAttributeResult> DeleteAttributeAsync(DeleteCategoryAttributeCommand command)
    {
        var validationErrors = ValidateDeleteCommand(command);
        if (validationErrors.Count > 0)
        {
            return DeleteCategoryAttributeResult.Failure(validationErrors);
        }

        try
        {
            var attribute = await _repository.GetByIdAsync(command.AttributeId);
            if (attribute == null)
            {
                return DeleteCategoryAttributeResult.Failure("Attribute not found.");
            }

            await _repository.DeleteAsync(command.AttributeId);

            _logger.LogInformation(
                "Category attribute {AttributeId} deleted",
                command.AttributeId);

            return DeleteCategoryAttributeResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category attribute {AttributeId}", command.AttributeId);
            return DeleteCategoryAttributeResult.Failure("An error occurred while deleting the attribute.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateCategoryAttributeResult> DeprecateAttributeAsync(Guid attributeId)
    {
        try
        {
            var attribute = await _repository.GetByIdAsync(attributeId);
            if (attribute == null)
            {
                return UpdateCategoryAttributeResult.Failure("Attribute not found.");
            }

            if (attribute.IsDeprecated)
            {
                return UpdateCategoryAttributeResult.Failure("Attribute is already deprecated.");
            }

            attribute.IsDeprecated = true;
            attribute.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdateAsync(attribute);

            _logger.LogInformation(
                "Category attribute {AttributeId} deprecated",
                attributeId);

            return UpdateCategoryAttributeResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deprecating category attribute {AttributeId}", attributeId);
            return UpdateCategoryAttributeResult.Failure("An error occurred while deprecating the attribute.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateCategoryAttributeResult> RestoreAttributeAsync(Guid attributeId)
    {
        try
        {
            var attribute = await _repository.GetByIdAsync(attributeId);
            if (attribute == null)
            {
                return UpdateCategoryAttributeResult.Failure("Attribute not found.");
            }

            if (!attribute.IsDeprecated)
            {
                return UpdateCategoryAttributeResult.Failure("Attribute is not deprecated.");
            }

            attribute.IsDeprecated = false;
            attribute.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdateAsync(attribute);

            _logger.LogInformation(
                "Category attribute {AttributeId} restored",
                attributeId);

            return UpdateCategoryAttributeResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring category attribute {AttributeId}", attributeId);
            return UpdateCategoryAttributeResult.Failure("An error occurred while restoring the attribute.");
        }
    }

    private async Task<List<string>> ValidateCreateCommandAsync(CreateCategoryAttributeCommand command)
    {
        var errors = new List<string>();

        if (command.CategoryId == Guid.Empty)
        {
            errors.Add("Category ID is required.");
        }

        ValidateName(command.Name, errors);
        ValidateListOptions(command.Type, command.ListOptions, errors);

        // Return early if there are basic validation errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Validate category exists
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId);
        if (category == null)
        {
            errors.Add("Category not found.");
            return errors;
        }

        // Check name uniqueness within category
        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            var exists = await _repository.ExistsByNameAsync(command.Name, command.CategoryId);
            if (exists)
            {
                errors.Add("An attribute with this name already exists in this category.");
            }
        }

        return errors;
    }

    private async Task<List<string>> ValidateUpdateCommandAsync(UpdateCategoryAttributeCommand command)
    {
        var errors = new List<string>();

        if (command.AttributeId == Guid.Empty)
        {
            errors.Add("Attribute ID is required.");
        }

        ValidateName(command.Name, errors);
        ValidateListOptions(command.Type, command.ListOptions, errors);

        if (command.DisplayOrder < 0)
        {
            errors.Add("Display order cannot be negative.");
        }

        // Return early if there are basic validation errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Get existing attribute to validate
        var existingAttribute = await _repository.GetByIdAsync(command.AttributeId);
        if (existingAttribute == null)
        {
            errors.Add("Attribute not found.");
            return errors;
        }

        // Check name uniqueness within category (excluding current attribute)
        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            var exists = await _repository.ExistsByNameAsync(command.Name, existingAttribute.CategoryId, command.AttributeId);
            if (exists)
            {
                errors.Add("An attribute with this name already exists in this category.");
            }
        }

        return errors;
    }

    private static List<string> ValidateDeleteCommand(DeleteCategoryAttributeCommand command)
    {
        var errors = new List<string>();

        if (command.AttributeId == Guid.Empty)
        {
            errors.Add("Attribute ID is required.");
        }

        return errors;
    }

    private static void ValidateName(string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }
        else if (name.Length < ProductValidationConstants.CategoryAttributeNameMinLength ||
                 name.Length > ProductValidationConstants.CategoryAttributeNameMaxLength)
        {
            errors.Add($"Name must be between {ProductValidationConstants.CategoryAttributeNameMinLength} and {ProductValidationConstants.CategoryAttributeNameMaxLength} characters.");
        }
    }

    private static void ValidateListOptions(CategoryAttributeType type, string? listOptions, List<string> errors)
    {
        if (type == CategoryAttributeType.List)
        {
            if (string.IsNullOrWhiteSpace(listOptions))
            {
                errors.Add("List options are required for List type attributes.");
                return;
            }

            // Validate JSON array format
            try
            {
                var options = JsonSerializer.Deserialize<string[]>(listOptions);
                if (options == null || options.Length == 0)
                {
                    errors.Add("List options must contain at least one option.");
                }
            }
            catch (JsonException)
            {
                errors.Add("List options must be a valid JSON array of strings.");
            }
        }
    }
}
