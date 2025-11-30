using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Web.Pages.Admin;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class UserAnalyticsModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsAnalyticsWithDefaultPeriod()
    {
        // Arrange
        var mockService = new Mock<IUserAnalyticsService>(MockBehavior.Strict);
        mockService.Setup(x => x.GetAnalyticsAsync(
                It.Is<UserAnalyticsQuery>(q => q.StartDate < DateTimeOffset.UtcNow),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAnalyticsResult
            {
                NewBuyerAccounts = 10,
                NewSellerAccounts = 5,
                TotalActiveUsers = 100,
                UsersWhoLoggedIn = 100,
                UsersWhoPlacedOrders = 50,
                HasBuyerRegistrationData = true,
                HasSellerRegistrationData = true,
                HasLoginActivityData = true,
                HasOrderActivityData = true
            });

        var mockLogger = new Mock<ILogger<UserAnalyticsModel>>();

        var model = new UserAnalyticsModel(mockService.Object, mockLogger.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.NotNull(model.AnalyticsResult);
        Assert.Equal(10, model.AnalyticsResult.NewBuyerAccounts);
        Assert.Equal(5, model.AnalyticsResult.NewSellerAccounts);
        Assert.Equal(100, model.AnalyticsResult.TotalActiveUsers);
    }

    [Theory]
    [InlineData("today", "Today")]
    [InlineData("7d", "Last 7 Days")]
    [InlineData("30d", "Last 30 Days")]
    [InlineData("90d", "Last 90 Days")]
    [InlineData("invalid", "Last 30 Days")]
    public void GetTimePeriodDisplay_ReturnsCorrectDisplay(string period, string expected)
    {
        // Act
        var result = UserAnalyticsModel.GetTimePeriodDisplay(period);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(false, "opacity-50")]
    public void GetCardClass_ReturnsCorrectClass(bool hasData, string expected)
    {
        // Act
        var result = UserAnalyticsModel.GetCardClass(hasData);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<UserAnalyticsModel>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAnalyticsModel(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockService = new Mock<IUserAnalyticsService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAnalyticsModel(mockService.Object, null!));
    }

    [Theory]
    [InlineData("today")]
    [InlineData("7d")]
    [InlineData("30d")]
    [InlineData("90d")]
    public async Task OnGetAsync_WithDifferentTimePeriods_CalculatesCorrectDateRange(string timePeriod)
    {
        // Arrange
        var mockService = new Mock<IUserAnalyticsService>(MockBehavior.Strict);
        mockService.Setup(x => x.GetAnalyticsAsync(
                It.IsAny<UserAnalyticsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAnalyticsResult());

        var mockLogger = new Mock<ILogger<UserAnalyticsModel>>();

        var model = new UserAnalyticsModel(mockService.Object, mockLogger.Object)
        {
            TimePeriod = timePeriod
        };

        // Act
        await model.OnGetAsync();

        // Assert
        mockService.Verify(
            x => x.GetAnalyticsAsync(
                It.Is<UserAnalyticsQuery>(q => 
                    q.EndDate <= DateTimeOffset.UtcNow && 
                    q.StartDate < q.EndDate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
