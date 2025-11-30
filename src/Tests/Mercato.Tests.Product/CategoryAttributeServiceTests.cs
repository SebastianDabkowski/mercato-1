using Mercato.Product.Application.Commands;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

/// <summary>
/// Unit tests for the CategoryAttributeService.
/// </summary>
public class CategoryAttributeServiceTests
{
    private static readonly Guid TestCategoryId = Guid.NewGuid();
    private static readonly Guid TestAttributeId = Guid.NewGuid();

    private readonly Mock<ICategoryAttributeRepository> _mockAttributeRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<ILogger<CategoryAttributeService>> _mockLogger;
    private readonly CategoryAttributeService _service;

    public CategoryAttributeServiceTests()
    {
        _mockAttributeRepository = new Mock<ICategoryAttributeRepository>(MockBehavior.Strict);
        _mockCategoryRepository = new Mock<ICategoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CategoryAttributeService>>();
        _service = new CategoryAttributeService(
            _mockAttributeRepository.Object,
            _mockCategoryRepository.Object,
            _mockLogger.Object);
    }

    #region CreateAttributeAsync Tests

    [Fact]
    public async Task CreateAttributeAsync_ValidTextCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidCreateCommand();

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(CreateTestCategory());
        _mockAttributeRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.CategoryId, null))
            .ReturnsAsync(false);
        _mockAttributeRepository.Setup(r => r.AddAsync(It.IsAny<CategoryAttribute>()))
            .ReturnsAsync((CategoryAttribute a) => a);

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.AttributeId);
        Assert.NotEqual(Guid.Empty, result.AttributeId.Value);
        _mockAttributeRepository.Verify(r => r.AddAsync(It.Is<CategoryAttribute>(a =>
            a.Name == command.Name &&
            a.Type == CategoryAttributeType.Text &&
            a.IsRequired == command.IsRequired &&
            !a.IsDeprecated
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAttributeAsync_ValidListCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateCategoryAttributeCommand
        {
            CategoryId = TestCategoryId,
            Name = "Size",
            Type = CategoryAttributeType.List,
            IsRequired = true,
            ListOptions = "[\"Small\", \"Medium\", \"Large\"]"
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(CreateTestCategory());
        _mockAttributeRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.CategoryId, null))
            .ReturnsAsync(false);
        _mockAttributeRepository.Setup(r => r.AddAsync(It.IsAny<CategoryAttribute>()))
            .ReturnsAsync((CategoryAttribute a) => a);

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockAttributeRepository.Verify(r => r.AddAsync(It.Is<CategoryAttribute>(a =>
            a.Type == CategoryAttributeType.List &&
            a.ListOptions == command.ListOptions
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAttributeAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Category not found.", result.Errors);
        _mockAttributeRepository.Verify(r => r.AddAsync(It.IsAny<CategoryAttribute>()), Times.Never);
    }

    [Fact]
    public async Task CreateAttributeAsync_EmptyName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.Name = string.Empty;

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Name is required.", result.Errors);
    }

    [Fact]
    public async Task CreateAttributeAsync_NameTooShort_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        command.Name = "A";

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Name must be between {ProductValidationConstants.CategoryAttributeNameMinLength} and {ProductValidationConstants.CategoryAttributeNameMaxLength} characters.", result.Errors);
    }

    [Fact]
    public async Task CreateAttributeAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCreateCommand();

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(command.CategoryId))
            .ReturnsAsync(CreateTestCategory());
        _mockAttributeRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.CategoryId, null))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("An attribute with this name already exists in this category.", result.Errors);
    }

    [Fact]
    public async Task CreateAttributeAsync_ListTypeWithoutOptions_ReturnsFailure()
    {
        // Arrange
        var command = new CreateCategoryAttributeCommand
        {
            CategoryId = TestCategoryId,
            Name = "Size",
            Type = CategoryAttributeType.List,
            IsRequired = false,
            ListOptions = null
        };

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("List options are required for List type attributes.", result.Errors);
    }

    [Fact]
    public async Task CreateAttributeAsync_ListTypeWithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var command = new CreateCategoryAttributeCommand
        {
            CategoryId = TestCategoryId,
            Name = "Size",
            Type = CategoryAttributeType.List,
            IsRequired = false,
            ListOptions = "not valid json"
        };

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("List options must be a valid JSON array of strings.", result.Errors);
    }

    [Fact]
    public async Task CreateAttributeAsync_ListTypeWithEmptyArray_ReturnsFailure()
    {
        // Arrange
        var command = new CreateCategoryAttributeCommand
        {
            CategoryId = TestCategoryId,
            Name = "Size",
            Type = CategoryAttributeType.List,
            IsRequired = false,
            ListOptions = "[]"
        };

        // Act
        var result = await _service.CreateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("List options must contain at least one option.", result.Errors);
    }

    #endregion

    #region GetAttributesByCategoryIdAsync Tests

    [Fact]
    public async Task GetAttributesByCategoryIdAsync_ReturnsAllAttributes()
    {
        // Arrange
        var attributes = new List<CategoryAttribute>
        {
            CreateTestAttribute(),
            CreateTestAttribute(Guid.NewGuid())
        };

        _mockAttributeRepository.Setup(r => r.GetByCategoryIdAsync(TestCategoryId))
            .ReturnsAsync(attributes);

        // Act
        var result = await _service.GetAttributesByCategoryIdAsync(TestCategoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockAttributeRepository.Verify(r => r.GetByCategoryIdAsync(TestCategoryId), Times.Once);
    }

    [Fact]
    public async Task GetAttributesByCategoryIdAsync_WhenNoAttributes_ReturnsEmptyList()
    {
        // Arrange
        _mockAttributeRepository.Setup(r => r.GetByCategoryIdAsync(TestCategoryId))
            .ReturnsAsync(new List<CategoryAttribute>());

        // Act
        var result = await _service.GetAttributesByCategoryIdAsync(TestCategoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetActiveAttributesByCategoryIdAsync Tests

    [Fact]
    public async Task GetActiveAttributesByCategoryIdAsync_ReturnsOnlyActiveAttributes()
    {
        // Arrange
        var activeAttribute = CreateTestAttribute();
        activeAttribute.IsDeprecated = false;

        _mockAttributeRepository.Setup(r => r.GetActiveByCategoryIdAsync(TestCategoryId))
            .ReturnsAsync(new List<CategoryAttribute> { activeAttribute });

        // Act
        var result = await _service.GetActiveAttributesByCategoryIdAsync(TestCategoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, a => Assert.False(a.IsDeprecated));
    }

    #endregion

    #region GetAttributeByIdAsync Tests

    [Fact]
    public async Task GetAttributeByIdAsync_WhenAttributeExists_ReturnsAttribute()
    {
        // Arrange
        var expectedAttribute = CreateTestAttribute();
        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync(expectedAttribute);

        // Act
        var result = await _service.GetAttributeByIdAsync(TestAttributeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAttribute.Id, result.Id);
        Assert.Equal(expectedAttribute.Name, result.Name);
    }

    [Fact]
    public async Task GetAttributeByIdAsync_WhenAttributeNotExists_ReturnsNull()
    {
        // Arrange
        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync((CategoryAttribute?)null);

        // Act
        var result = await _service.GetAttributeByIdAsync(TestAttributeId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateAttributeAsync Tests

    [Fact]
    public async Task UpdateAttributeAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var existingAttribute = CreateTestAttribute();

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(command.AttributeId))
            .ReturnsAsync(existingAttribute);
        _mockAttributeRepository.Setup(r => r.ExistsByNameAsync(command.Name, existingAttribute.CategoryId, command.AttributeId))
            .ReturnsAsync(false);
        _mockAttributeRepository.Setup(r => r.UpdateAsync(It.IsAny<CategoryAttribute>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAttributeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockAttributeRepository.Verify(r => r.UpdateAsync(It.Is<CategoryAttribute>(a =>
            a.Name == command.Name &&
            a.Type == command.Type &&
            a.IsRequired == command.IsRequired &&
            a.IsDeprecated == command.IsDeprecated
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateAttributeAsync_AttributeNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(command.AttributeId))
            .ReturnsAsync((CategoryAttribute?)null);

        // Act
        var result = await _service.UpdateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute not found.", result.Errors);
    }

    [Fact]
    public async Task UpdateAttributeAsync_EmptyAttributeId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.AttributeId = Guid.Empty;

        // Act
        var result = await _service.UpdateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute ID is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateAttributeAsync_NegativeDisplayOrder_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.DisplayOrder = -1;

        // Act
        var result = await _service.UpdateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Display order cannot be negative.", result.Errors);
    }

    [Fact]
    public async Task UpdateAttributeAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var existingAttribute = CreateTestAttribute();

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(command.AttributeId))
            .ReturnsAsync(existingAttribute);
        _mockAttributeRepository.Setup(r => r.ExistsByNameAsync(command.Name, existingAttribute.CategoryId, command.AttributeId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("An attribute with this name already exists in this category.", result.Errors);
    }

    #endregion

    #region DeleteAttributeAsync Tests

    [Fact]
    public async Task DeleteAttributeAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidDeleteCommand();
        var existingAttribute = CreateTestAttribute();

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(command.AttributeId))
            .ReturnsAsync(existingAttribute);
        _mockAttributeRepository.Setup(r => r.DeleteAsync(command.AttributeId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAttributeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockAttributeRepository.Verify(r => r.DeleteAsync(command.AttributeId), Times.Once);
    }

    [Fact]
    public async Task DeleteAttributeAsync_AttributeNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidDeleteCommand();

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(command.AttributeId))
            .ReturnsAsync((CategoryAttribute?)null);

        // Act
        var result = await _service.DeleteAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute not found.", result.Errors);
    }

    [Fact]
    public async Task DeleteAttributeAsync_EmptyAttributeId_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteCategoryAttributeCommand
        {
            AttributeId = Guid.Empty
        };

        // Act
        var result = await _service.DeleteAttributeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute ID is required.", result.Errors);
    }

    #endregion

    #region DeprecateAttributeAsync Tests

    [Fact]
    public async Task DeprecateAttributeAsync_ValidAttribute_ReturnsSuccess()
    {
        // Arrange
        var existingAttribute = CreateTestAttribute();
        existingAttribute.IsDeprecated = false;

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync(existingAttribute);
        _mockAttributeRepository.Setup(r => r.UpdateAsync(It.IsAny<CategoryAttribute>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeprecateAttributeAsync(TestAttributeId);

        // Assert
        Assert.True(result.Succeeded);
        _mockAttributeRepository.Verify(r => r.UpdateAsync(It.Is<CategoryAttribute>(a =>
            a.IsDeprecated == true
        )), Times.Once);
    }

    [Fact]
    public async Task DeprecateAttributeAsync_AttributeNotFound_ReturnsFailure()
    {
        // Arrange
        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync((CategoryAttribute?)null);

        // Act
        var result = await _service.DeprecateAttributeAsync(TestAttributeId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute not found.", result.Errors);
    }

    [Fact]
    public async Task DeprecateAttributeAsync_AlreadyDeprecated_ReturnsFailure()
    {
        // Arrange
        var existingAttribute = CreateTestAttribute();
        existingAttribute.IsDeprecated = true;

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync(existingAttribute);

        // Act
        var result = await _service.DeprecateAttributeAsync(TestAttributeId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute is already deprecated.", result.Errors);
    }

    #endregion

    #region RestoreAttributeAsync Tests

    [Fact]
    public async Task RestoreAttributeAsync_DeprecatedAttribute_ReturnsSuccess()
    {
        // Arrange
        var existingAttribute = CreateTestAttribute();
        existingAttribute.IsDeprecated = true;

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync(existingAttribute);
        _mockAttributeRepository.Setup(r => r.UpdateAsync(It.IsAny<CategoryAttribute>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RestoreAttributeAsync(TestAttributeId);

        // Assert
        Assert.True(result.Succeeded);
        _mockAttributeRepository.Verify(r => r.UpdateAsync(It.Is<CategoryAttribute>(a =>
            a.IsDeprecated == false
        )), Times.Once);
    }

    [Fact]
    public async Task RestoreAttributeAsync_AttributeNotFound_ReturnsFailure()
    {
        // Arrange
        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync((CategoryAttribute?)null);

        // Act
        var result = await _service.RestoreAttributeAsync(TestAttributeId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute not found.", result.Errors);
    }

    [Fact]
    public async Task RestoreAttributeAsync_NotDeprecated_ReturnsFailure()
    {
        // Arrange
        var existingAttribute = CreateTestAttribute();
        existingAttribute.IsDeprecated = false;

        _mockAttributeRepository.Setup(r => r.GetByIdAsync(TestAttributeId))
            .ReturnsAsync(existingAttribute);

        // Act
        var result = await _service.RestoreAttributeAsync(TestAttributeId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Attribute is not deprecated.", result.Errors);
    }

    #endregion

    #region Helper Methods

    private static CreateCategoryAttributeCommand CreateValidCreateCommand()
    {
        return new CreateCategoryAttributeCommand
        {
            CategoryId = TestCategoryId,
            Name = "Brand",
            Type = CategoryAttributeType.Text,
            IsRequired = false,
            ListOptions = null
        };
    }

    private static UpdateCategoryAttributeCommand CreateValidUpdateCommand()
    {
        return new UpdateCategoryAttributeCommand
        {
            AttributeId = TestAttributeId,
            Name = "Updated Attribute",
            Type = CategoryAttributeType.Text,
            IsRequired = true,
            IsDeprecated = false,
            ListOptions = null,
            DisplayOrder = 1
        };
    }

    private static DeleteCategoryAttributeCommand CreateValidDeleteCommand()
    {
        return new DeleteCategoryAttributeCommand
        {
            AttributeId = TestAttributeId
        };
    }

    private static Category CreateTestCategory(Guid? id = null)
    {
        return new Category
        {
            Id = id ?? TestCategoryId,
            Name = "Test Category",
            Slug = "test-category",
            Description = "Test category description",
            ParentId = null,
            DisplayOrder = 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static CategoryAttribute CreateTestAttribute(Guid? id = null)
    {
        return new CategoryAttribute
        {
            Id = id ?? TestAttributeId,
            CategoryId = TestCategoryId,
            Name = "Test Attribute",
            Type = CategoryAttributeType.Text,
            IsRequired = false,
            IsDeprecated = false,
            ListOptions = null,
            DisplayOrder = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
