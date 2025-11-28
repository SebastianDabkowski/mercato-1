using Mercato.Product.Application.Queries;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

/// <summary>
/// Unit tests for the SearchSuggestionService.
/// </summary>
public class SearchSuggestionServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<ILogger<SearchSuggestionService>> _mockLogger;
    private readonly SearchSuggestionService _service;

    public SearchSuggestionServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        _mockCategoryRepository = new Mock<ICategoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<SearchSuggestionService>>();
        _service = new SearchSuggestionService(
            _mockProductRepository.Object,
            _mockCategoryRepository.Object,
            _mockLogger.Object);
    }

    #region GetSuggestionsAsync Tests

    [Fact]
    public async Task GetSuggestionsAsync_EmptySearchTerm_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "",
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
        Assert.Empty(result.Products);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WhitespaceSearchTerm_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "   ",
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
        Assert.Empty(result.Products);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetSuggestionsAsync_SingleCharSearchTerm_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "a",
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
        Assert.Empty(result.Products);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ValidSearchTerm_ReturnsProductsAndCategories()
    {
        // Arrange
        var searchTerm = "laptop";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid(), "Laptop Pro"),
            CreateTestProduct(Guid.NewGuid(), "Gaming Laptop")
        };

        var categories = new List<Category>
        {
            CreateTestCategory(Guid.NewGuid(), "Laptops"),
            CreateTestCategory(Guid.NewGuid(), "Laptop Accessories")
        };

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(products);
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.True(result.HasSuggestions);
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(2, result.Categories.Count);
        Assert.Equal("Laptop Pro", result.Products[0].Title);
        Assert.Equal("Gaming Laptop", result.Products[1].Title);
        Assert.Equal("Laptops", result.Categories[0].Name);
        Assert.Equal("Laptop Accessories", result.Categories[1].Name);
    }

    [Fact]
    public async Task GetSuggestionsAsync_OnlyProductsFound_ReturnsOnlyProducts()
    {
        // Arrange
        var searchTerm = "widget";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        var products = new List<Mercato.Product.Domain.Entities.Product>
        {
            CreateTestProduct(Guid.NewGuid(), "Super Widget")
        };

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(products);
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.True(result.HasSuggestions);
        Assert.Single(result.Products);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetSuggestionsAsync_OnlyCategoriesFound_ReturnsOnlyCategories()
    {
        // Arrange
        var searchTerm = "electronics";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        var categories = new List<Category>
        {
            CreateTestCategory(Guid.NewGuid(), "Electronics")
        };

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.True(result.HasSuggestions);
        Assert.Empty(result.Products);
        Assert.Single(result.Categories);
    }

    [Fact]
    public async Task GetSuggestionsAsync_NoResults_ReturnsEmptyResult()
    {
        // Arrange
        var searchTerm = "nonexistent";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
        Assert.Empty(result.Products);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetSuggestionsAsync_SearchTermWithLeadingWhitespace_TrimsAndSearches()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "  phone  ",
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync("phone", 5))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync("phone", 3))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        _mockProductRepository.Verify(r => r.SearchProductTitlesAsync("phone", 5), Times.Once);
        _mockCategoryRepository.Verify(r => r.SearchCategoriesAsync("phone", 3), Times.Once);
    }

    [Fact]
    public async Task GetSuggestionsAsync_NegativeMaxProductSuggestions_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "test",
            MaxProductSuggestions = -1,
            MaxCategorySuggestions = 3
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
    }

    [Fact]
    public async Task GetSuggestionsAsync_NegativeMaxCategorySuggestions_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "test",
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = -1
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ExceedsMaxProductSuggestions_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "test",
            MaxProductSuggestions = 25,
            MaxCategorySuggestions = 3
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ExceedsMaxCategorySuggestions_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SearchSuggestionQuery
        {
            SearchTerm = "test",
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 15
        };

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.False(result.HasSuggestions);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ProductMapsAllFields()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var searchTerm = "tablet";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        var product = CreateTestProduct(productId, "Tablet Pro");
        product.Price = 499.99m;
        product.Category = "Tablets";

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product> { product });
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.Single(result.Products);
        var suggestion = result.Products[0];
        Assert.Equal(productId, suggestion.Id);
        Assert.Equal("Tablet Pro", suggestion.Title);
        Assert.Equal(499.99m, suggestion.Price);
        Assert.Equal("Tablets", suggestion.Category);
    }

    [Fact]
    public async Task GetSuggestionsAsync_CategoryMapsAllFields()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var searchTerm = "accessories";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        var category = CreateTestCategory(categoryId, "Phone Accessories");

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(new List<Category> { category });

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        Assert.Single(result.Categories);
        var suggestion = result.Categories[0];
        Assert.Equal(categoryId, suggestion.Id);
        Assert.Equal("Phone Accessories", suggestion.Name);
    }

    [Fact]
    public async Task GetSuggestionsAsync_MinSearchTermLength_TwoCharsWorks()
    {
        // Arrange
        var searchTerm = "ab";
        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        _mockProductRepository.Setup(r => r.SearchProductTitlesAsync(searchTerm, 5))
            .ReturnsAsync(new List<Mercato.Product.Domain.Entities.Product>());
        _mockCategoryRepository.Setup(r => r.SearchCategoriesAsync(searchTerm, 3))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetSuggestionsAsync(query);

        // Assert
        _mockProductRepository.Verify(r => r.SearchProductTitlesAsync(searchTerm, 5), Times.Once);
        _mockCategoryRepository.Verify(r => r.SearchCategoriesAsync(searchTerm, 3), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Mercato.Product.Domain.Entities.Product CreateTestProduct(Guid id, string title)
    {
        return new Mercato.Product.Domain.Entities.Product
        {
            Id = id,
            StoreId = Guid.NewGuid(),
            Title = title,
            Description = "Test description",
            Price = 99.99m,
            Stock = 10,
            Category = "Test Category",
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static Category CreateTestCategory(Guid id, string name)
    {
        return new Category
        {
            Id = id,
            Name = name,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
