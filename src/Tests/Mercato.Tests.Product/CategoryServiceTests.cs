using Mercato.Product.Application.Commands;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

/// <summary>
/// Unit tests for the CategoryService.
/// </summary>
public class CategoryServiceTests
{
    private static readonly Guid TestCategoryId = Guid.NewGuid();
    private static readonly Guid TestParentCategoryId = Guid.NewGuid();

    private readonly Mock<ICategoryRepository> _mockRepository;
    private readonly Mock<ILogger<CategoryService>> _mockLogger;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _mockRepository = new Mock<ICategoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CategoryService>>();
        _service = new CategoryService(_mockRepository.Object, _mockLogger.Object);
    }

    #region CreateCategoryAsync Tests

    [Fact]
    public async Task CreateCategoryAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidCreateCommand();

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, null))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.CategoryId);
        Assert.NotEqual(Guid.Empty, result.CategoryId.Value);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Name == command.Name &&
            c.ParentId == command.ParentId &&
            c.IsActive
        )), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_WithParent_ValidatesParentExists()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.ParentId = TestParentCategoryId;
        var parentCategory = CreateTestCategory(TestParentCategoryId);

        _mockRepository.Setup(r => r.GetByIdAsync(TestParentCategoryId))
            .ReturnsAsync(parentCategory);
        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, null))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.GetByIdAsync(TestParentCategoryId), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_ParentNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.ParentId = TestParentCategoryId;

        _mockRepository.Setup(r => r.GetByIdAsync(TestParentCategoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Parent category not found.", result.Errors);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateCategoryAsync_EmptyName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.Name = string.Empty;

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Name is required.", result.Errors);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateCategoryAsync_NameTooShort_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.Name = "A";

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Name must be between {ProductValidationConstants.CategoryNameMinLength} and {ProductValidationConstants.CategoryNameMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateCategoryAsync_NameTooLong_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.Name = new string('A', ProductValidationConstants.CategoryNameMaxLength + 1);

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Name must be between {ProductValidationConstants.CategoryNameMinLength} and {ProductValidationConstants.CategoryNameMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, null))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("A category with this name already exists under the same parent.", result.Errors);
    }

    #endregion

    #region GetAllCategoriesAsync Tests

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            CreateTestCategory(),
            CreateTestCategory(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WhenNoCategories_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetCategoryByIdAsync Tests

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCategoryExists_ReturnsCategory()
    {
        // Arrange
        var expectedCategory = CreateTestCategory();
        _mockRepository.Setup(r => r.GetByIdAsync(TestCategoryId))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _service.GetCategoryByIdAsync(TestCategoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCategory.Id, result.Id);
        Assert.Equal(expectedCategory.Name, result.Name);
        _mockRepository.Verify(r => r.GetByIdAsync(TestCategoryId), Times.Once);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCategoryNotExists_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(TestCategoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.GetCategoryByIdAsync(TestCategoryId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateCategoryAsync Tests

    [Fact]
    public async Task UpdateCategoryAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var category = CreateTestCategory();

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, command.CategoryId))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(category);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Category>(c =>
            c.Name == command.Name &&
            c.DisplayOrder == command.DisplayOrder &&
            c.IsActive == command.IsActive
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, command.CategoryId))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Category not found.", result.Errors);
    }

    [Fact]
    public async Task UpdateCategoryAsync_EmptyCategoryId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.CategoryId = Guid.Empty;

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Category ID is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateCategoryAsync_SelfAsParent_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.ParentId = command.CategoryId; // Set self as parent

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("A category cannot be its own parent.", result.Errors);
    }

    [Fact]
    public async Task UpdateCategoryAsync_NegativeDisplayOrder_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.DisplayOrder = -1;

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Display order cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task UpdateCategoryAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, command.CategoryId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("A category with this name already exists under the same parent.", result.Errors);
    }

    [Fact]
    public async Task UpdateCategoryAsync_DeactivateCategory_Succeeds()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.IsActive = false;
        var category = CreateTestCategory();

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.ParentId, command.CategoryId))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(category);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCategoryAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Category>(c => !c.IsActive)), Times.Once);
    }

    #endregion

    #region DeleteCategoryAsync Tests

    [Fact]
    public async Task DeleteCategoryAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var category = CreateTestCategory();

        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(category);
        _mockRepository.Setup(r => r.GetProductCountAsync(command.CategoryId))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetChildCountAsync(command.CategoryId))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.DeleteAsync(command.CategoryId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteCategoryAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRepository.Verify(r => r.DeleteAsync(command.CategoryId), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidDeleteCommand();

        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.DeleteCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Category not found.", result.Errors);
    }

    [Fact]
    public async Task DeleteCategoryAsync_EmptyCategoryId_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteCategoryCommand
        {
            CategoryId = Guid.Empty
        };

        // Act
        var result = await _service.DeleteCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Category ID is required.", result.Errors);
    }

    [Fact]
    public async Task DeleteCategoryAsync_HasProducts_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var category = CreateTestCategory();

        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(category);
        _mockRepository.Setup(r => r.GetProductCountAsync(command.CategoryId))
            .ReturnsAsync(5);

        // Act
        var result = await _service.DeleteCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot delete category. There are 5 product(s) assigned to this category.", result.Errors[0]);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_HasChildCategories_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var category = CreateTestCategory();

        _mockRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(category);
        _mockRepository.Setup(r => r.GetProductCountAsync(command.CategoryId))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetChildCountAsync(command.CategoryId))
            .ReturnsAsync(3);

        // Act
        var result = await _service.DeleteCategoryAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot delete category. There are 3 child categories under this category.", result.Errors[0]);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region GetCategoriesByParentIdAsync Tests

    [Fact]
    public async Task GetCategoriesByParentIdAsync_ReturnsChildCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            CreateTestCategory(),
            CreateTestCategory(Guid.NewGuid())
        };

        _mockRepository.Setup(r => r.GetByParentIdAsync(TestParentCategoryId))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetCategoriesByParentIdAsync(TestParentCategoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetByParentIdAsync(TestParentCategoryId), Times.Once);
    }

    [Fact]
    public async Task GetCategoriesByParentIdAsync_NullParentId_ReturnsRootCategories()
    {
        // Arrange
        var rootCategories = new List<Category> { CreateTestCategory() };

        _mockRepository.Setup(r => r.GetByParentIdAsync(null))
            .ReturnsAsync(rootCategories);

        // Act
        var result = await _service.GetCategoriesByParentIdAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        _mockRepository.Verify(r => r.GetByParentIdAsync(null), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static CreateCategoryCommand CreateValidCreateCommand()
    {
        return new CreateCategoryCommand
        {
            Name = "Test Category",
            ParentId = null
        };
    }

    private static UpdateCategoryCommand CreateValidUpdateCommand()
    {
        return new UpdateCategoryCommand
        {
            CategoryId = TestCategoryId,
            Name = "Updated Category",
            ParentId = null,
            DisplayOrder = 1,
            IsActive = true
        };
    }

    private static DeleteCategoryCommand CreateValidDeleteCommand()
    {
        return new DeleteCategoryCommand
        {
            CategoryId = TestCategoryId
        };
    }

    private static Category CreateTestCategory(Guid? id = null)
    {
        return new Category
        {
            Id = id ?? TestCategoryId,
            Name = "Test Category",
            ParentId = null,
            DisplayOrder = 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
