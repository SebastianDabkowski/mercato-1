using Mercato.Product.Application.Queries;

namespace Mercato.Tests.Product;

/// <summary>
/// Tests for the PaginationInfo class.
/// </summary>
public class PaginationInfoTests
{
    [Theory]
    [InlineData(1, 100, 10, 10)] // 100 items, 10 per page = 10 pages
    [InlineData(1, 25, 10, 3)]   // 25 items, 10 per page = 3 pages
    [InlineData(1, 10, 10, 1)]   // 10 items, 10 per page = 1 page
    [InlineData(1, 0, 10, 0)]    // 0 items = 0 pages
    [InlineData(1, 5, 10, 1)]    // 5 items, 10 per page = 1 page
    public void Create_ComputesTotalPagesCorrectly(int currentPage, int totalCount, int pageSize, int expectedTotalPages)
    {
        // Act
        var pagination = PaginationInfo.Create(currentPage, totalCount, pageSize);

        // Assert
        Assert.Equal(expectedTotalPages, pagination.TotalPages);
    }

    [Fact]
    public void HasMultiplePages_WhenTotalPagesGreaterThanOne_ReturnsTrue()
    {
        // Arrange
        var pagination = PaginationInfo.Create(1, 25, 10); // 3 pages

        // Assert
        Assert.True(pagination.HasMultiplePages);
    }

    [Fact]
    public void HasMultiplePages_WhenTotalPagesIsOne_ReturnsFalse()
    {
        // Arrange
        var pagination = PaginationInfo.Create(1, 10, 10); // 1 page

        // Assert
        Assert.False(pagination.HasMultiplePages);
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ReturnsFalse()
    {
        // Arrange
        var pagination = PaginationInfo.Create(1, 50, 10);

        // Assert
        Assert.False(pagination.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ReturnsTrue()
    {
        // Arrange
        var pagination = PaginationInfo.Create(2, 50, 10);

        // Assert
        Assert.True(pagination.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        // Arrange
        var pagination = PaginationInfo.Create(5, 50, 10); // 5 pages, on page 5

        // Assert
        Assert.False(pagination.HasNextPage);
    }

    [Fact]
    public void HasNextPage_NotOnLastPage_ReturnsTrue()
    {
        // Arrange
        var pagination = PaginationInfo.Create(3, 50, 10); // 5 pages, on page 3

        // Assert
        Assert.True(pagination.HasNextPage);
    }

    [Fact]
    public void IsLastPage_OnLastPage_ReturnsTrue()
    {
        // Arrange
        var pagination = PaginationInfo.Create(5, 50, 10); // 5 pages, on page 5

        // Assert
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void IsLastPage_NotOnLastPage_ReturnsFalse()
    {
        // Arrange
        var pagination = PaginationInfo.Create(3, 50, 10); // 5 pages, on page 3

        // Assert
        Assert.False(pagination.IsLastPage);
    }

    [Theory]
    [InlineData(1, 100, 10, 1)]   // Page 1: items 1-10
    [InlineData(2, 100, 10, 11)]  // Page 2: items 11-20
    [InlineData(5, 100, 10, 41)]  // Page 5: items 41-50
    public void FirstItemIndex_ReturnsCorrectIndex(int currentPage, int totalCount, int pageSize, int expectedFirstIndex)
    {
        // Act
        var pagination = PaginationInfo.Create(currentPage, totalCount, pageSize);

        // Assert
        Assert.Equal(expectedFirstIndex, pagination.FirstItemIndex);
    }

    [Theory]
    [InlineData(1, 100, 10, 10)]  // Page 1: items 1-10
    [InlineData(2, 100, 10, 20)]  // Page 2: items 11-20
    [InlineData(5, 50, 10, 50)]   // Page 5 of 5: items 41-50
    [InlineData(3, 25, 10, 25)]   // Page 3 of 3: items 21-25 (partial page)
    public void LastItemIndex_ReturnsCorrectIndex(int currentPage, int totalCount, int pageSize, int expectedLastIndex)
    {
        // Act
        var pagination = PaginationInfo.Create(currentPage, totalCount, pageSize);

        // Assert
        Assert.Equal(expectedLastIndex, pagination.LastItemIndex);
    }

    [Fact]
    public void FirstItemIndex_WhenNoItems_ReturnsZero()
    {
        // Arrange
        var pagination = PaginationInfo.Create(1, 0, 10);

        // Assert
        Assert.Equal(0, pagination.FirstItemIndex);
    }

    [Fact]
    public void LastItemIndex_WhenNoItems_ReturnsZero()
    {
        // Arrange
        var pagination = PaginationInfo.Create(1, 0, 10);

        // Assert
        Assert.Equal(0, pagination.LastItemIndex);
    }

    [Theory]
    [InlineData(1, 50, 10, 1)]   // On page 1, previous should be 1
    [InlineData(3, 50, 10, 2)]   // On page 3, previous should be 2
    public void PreviousPage_ReturnsCorrectPage(int currentPage, int totalCount, int pageSize, int expectedPrevious)
    {
        // Act
        var pagination = PaginationInfo.Create(currentPage, totalCount, pageSize);

        // Assert
        Assert.Equal(expectedPrevious, pagination.PreviousPage);
    }

    [Theory]
    [InlineData(5, 50, 10, 5)]   // On last page, next should be 5
    [InlineData(3, 50, 10, 4)]   // On page 3, next should be 4
    public void NextPage_ReturnsCorrectPage(int currentPage, int totalCount, int pageSize, int expectedNext)
    {
        // Act
        var pagination = PaginationInfo.Create(currentPage, totalCount, pageSize);

        // Assert
        Assert.Equal(expectedNext, pagination.NextPage);
    }

    [Fact]
    public void NextPage_WhenNoItems_ReturnsOne()
    {
        // Arrange - 0 items means 0 pages
        var pagination = PaginationInfo.Create(1, 0, 10);

        // Act & Assert - should return 1 to avoid invalid page 0
        Assert.Equal(1, pagination.NextPage);
    }

    [Fact]
    public void GetVisiblePageNumbers_SmallPageCount_ReturnsAllPages()
    {
        // Arrange - 5 pages, which is less than max visible (7)
        var pagination = PaginationInfo.Create(1, 50, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should return all 5 pages with no ellipses
        Assert.Equal(5, visiblePages.Count);
        Assert.Equal([1, 2, 3, 4, 5], visiblePages.Cast<int?>().ToArray());
    }

    [Fact]
    public void GetVisiblePageNumbers_LargePageCount_OnFirstPage_ShowsWindowWithEllipsis()
    {
        // Arrange - 20 pages, on page 1
        var pagination = PaginationInfo.Create(1, 200, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should show: 1, 2, ..., 20
        Assert.Contains(1, visiblePages);
        Assert.Contains(2, visiblePages);
        Assert.Contains(null, visiblePages); // Ellipsis
        Assert.Contains(20, visiblePages);
    }

    [Fact]
    public void GetVisiblePageNumbers_LargePageCount_OnLastPage_ShowsWindowWithEllipsis()
    {
        // Arrange - 20 pages, on page 20
        var pagination = PaginationInfo.Create(20, 200, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should show: 1, ..., 19, 20
        Assert.Contains(1, visiblePages);
        Assert.Contains(null, visiblePages); // Ellipsis
        Assert.Contains(19, visiblePages);
        Assert.Contains(20, visiblePages);
    }

    [Fact]
    public void GetVisiblePageNumbers_LargePageCount_InMiddle_ShowsWindowWithBothEllipses()
    {
        // Arrange - 20 pages, on page 10
        var pagination = PaginationInfo.Create(10, 200, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should show: 1, ..., 9, 10, 11, ..., 20
        Assert.Contains(1, visiblePages);
        Assert.Contains(9, visiblePages);
        Assert.Contains(10, visiblePages);
        Assert.Contains(11, visiblePages);
        Assert.Contains(20, visiblePages);
        // Should have two ellipses
        Assert.Equal(2, visiblePages.Count(p => p == null));
    }

    [Fact]
    public void GetVisiblePageNumbers_ExactlyMaxVisiblePages_NoEllipsis()
    {
        // Arrange - exactly 7 pages (max visible)
        var pagination = PaginationInfo.Create(4, 70, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should return all 7 pages with no ellipses
        Assert.Equal(7, visiblePages.Count);
        Assert.DoesNotContain(null, visiblePages);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], visiblePages.Cast<int?>().ToArray());
    }

    [Fact]
    public void GetVisiblePageNumbers_NoDuplicatePages()
    {
        // Test case: when on page 2 of 20 pages, page 2 is in the window and should not duplicate
        var pagination = PaginationInfo.Create(2, 200, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should not have duplicate page numbers
        var pageNumbers = visiblePages.Where(p => p.HasValue).Select(p => p!.Value).ToList();
        Assert.Equal(pageNumbers.Distinct().Count(), pageNumbers.Count);
    }

    [Fact]
    public void GetVisiblePageNumbers_WhenOnSecondToLastPage_NoDuplicateLastPage()
    {
        // Test case: when on page 19 of 20 pages, page 20 is in the window and should not duplicate
        var pagination = PaginationInfo.Create(19, 200, 10);

        // Act
        var visiblePages = pagination.GetVisiblePageNumbers();

        // Assert - should not have duplicate page numbers
        var pageNumbers = visiblePages.Where(p => p.HasValue).Select(p => p!.Value).ToList();
        Assert.Equal(pageNumbers.Distinct().Count(), pageNumbers.Count);
    }
}
