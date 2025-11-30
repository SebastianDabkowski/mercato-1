using Mercato.Identity.Application.Queries;
using Mercato.Identity.Application.Services;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Mercato.Tests.Identity;

public class UserDataExportServiceTests
{
    [Fact]
    public async Task ExportUserDataAsync_WithValidUser_ReturnsSuccessWithIdentityData()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser 
        { 
            Id = userId, 
            Email = "test@example.com",
            EmailConfirmed = true,
            TwoFactorEnabled = false,
            LockoutEnd = null
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var service = new UserDataExportService(mockUserManager.Object);

        // Act
        var result = await service.ExportUserDataAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ExportData);
        Assert.NotNull(result.ExportedAt);
        Assert.Contains("test@example.com", result.ExportData);
        Assert.Contains("Buyer", result.ExportData);
    }

    [Fact]
    public async Task ExportUserDataAsync_WithNonExistentUser_ReturnsUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser?)null);

        var service = new UserDataExportService(mockUserManager.Object);

        // Act
        var result = await service.ExportUserDataAsync(userId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsUserNotFound);
        Assert.Contains("User not found.", result.Errors);
    }

    [Fact]
    public async Task ExportUserDataAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new UserDataExportService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExportUserDataAsync(string.Empty));
    }

    [Fact]
    public async Task ExportUserDataAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new UserDataExportService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ExportUserDataAsync(null!));
    }

    [Fact]
    public async Task ExportUserDataAsync_WithExternalLogins_IncludesExternalLoginData()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser 
        { 
            Id = userId, 
            Email = "test@example.com"
        };

        var externalLogins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-key", "Google")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(externalLogins);

        var service = new UserDataExportService(mockUserManager.Object);

        // Act
        var result = await service.ExportUserDataAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("Google", result.ExportData!);
    }

    [Fact]
    public async Task ExportUserDataAsync_WithLockedOutUser_IncludesLockoutStatus()
    {
        // Arrange
        var userId = "user-123";
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
        var user = new IdentityUser 
        { 
            Id = userId, 
            Email = "test@example.com",
            LockoutEnd = lockoutEnd
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var service = new UserDataExportService(mockUserManager.Object);

        // Act
        var result = await service.ExportUserDataAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        // JSON uses camelCase and true (not True)
        Assert.Contains("\"isLockedOut\": true", result.ExportData!);
    }

    [Fact]
    public async Task ExportUserDataAsync_WithUserDataProvider_IncludesAdditionalData()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser 
        { 
            Id = userId, 
            Email = "test@example.com"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var mockUserDataProvider = new Mock<IUserDataProvider>(MockBehavior.Strict);
        mockUserDataProvider.Setup(x => x.GetDeliveryAddressesAsync(userId))
            .ReturnsAsync(new List<DeliveryAddressData>
            {
                new DeliveryAddressData { Label = "Home", City = "New York" }
            });
        mockUserDataProvider.Setup(x => x.GetOrdersAsync(userId))
            .ReturnsAsync(new List<OrderData>
            {
                new OrderData { OrderNumber = "ORD-001", TotalAmount = 99.99m }
            });
        mockUserDataProvider.Setup(x => x.GetStoreAsync(userId))
            .ReturnsAsync((StoreData?)null);
        mockUserDataProvider.Setup(x => x.GetConsentsAsync(userId))
            .ReturnsAsync(new List<ConsentData>());

        var service = new UserDataExportService(mockUserManager.Object, mockUserDataProvider.Object);

        // Act
        var result = await service.ExportUserDataAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("New York", result.ExportData!);
        Assert.Contains("ORD-001", result.ExportData!);
        mockUserDataProvider.VerifyAll();
    }

    [Fact]
    public async Task ExportUserDataAsync_WithSellerUser_IncludesStoreData()
    {
        // Arrange
        var userId = "seller-123";
        var user = new IdentityUser 
        { 
            Id = userId, 
            Email = "seller@example.com"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Seller" });
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var mockUserDataProvider = new Mock<IUserDataProvider>(MockBehavior.Strict);
        mockUserDataProvider.Setup(x => x.GetDeliveryAddressesAsync(userId))
            .ReturnsAsync(new List<DeliveryAddressData>());
        mockUserDataProvider.Setup(x => x.GetOrdersAsync(userId))
            .ReturnsAsync(new List<OrderData>());
        mockUserDataProvider.Setup(x => x.GetStoreAsync(userId))
            .ReturnsAsync(new StoreData 
            { 
                Name = "Test Store", 
                ContactEmail = "store@example.com" 
            });
        mockUserDataProvider.Setup(x => x.GetConsentsAsync(userId))
            .ReturnsAsync(new List<ConsentData>());

        var service = new UserDataExportService(mockUserManager.Object, mockUserDataProvider.Object);

        // Act
        var result = await service.ExportUserDataAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("Test Store", result.ExportData!);
        Assert.Contains("store@example.com", result.ExportData!);
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UserDataExportService(null!));
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
