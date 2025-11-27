using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Mercato.Tests.Identity;

public class GoogleLoginServiceTests
{
    private const string BuyerRole = "Buyer";

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithExistingBuyerUser_ReturnsSuccess()
    {
        // Arrange
        var email = "buyer@example.com";
        var googleId = "google-123";
        var name = "Test Buyer";

        var user = new IdentityUser { Id = "user-1", Email = email, UserName = email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo> { new UserLoginInfo("Google", googleId, "Google") });

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, name);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(email, result.Email);
        Assert.False(result.IsNewUser);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithExistingBuyerUserWithoutGoogleLogin_LinksGoogleAccount()
    {
        // Arrange
        var email = "buyer@example.com";
        var googleId = "google-123";
        var name = "Test Buyer";

        var user = new IdentityUser { Id = "user-1", Email = email, UserName = email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>()); // No existing logins
        mockUserManager.Setup(x => x.AddLoginAsync(user, It.Is<UserLoginInfo>(l => l.LoginProvider == "Google" && l.ProviderKey == googleId)))
            .ReturnsAsync(IdentityResult.Success);

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, name);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(email, result.Email);
        Assert.False(result.IsNewUser);
        mockUserManager.Verify(x => x.AddLoginAsync(user, It.IsAny<UserLoginInfo>()), Times.Once);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithExistingNonBuyerUser_ReturnsNotABuyer()
    {
        // Arrange
        var email = "seller@example.com";
        var googleId = "google-123";

        var user = new IdentityUser { Id = "user-1", Email = email, UserName = email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(false);

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("buyers only", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithNewUser_CreatesUserAndAssignsBuyerRole()
    {
        // Arrange
        var email = "newbuyer@example.com";
        var googleId = "google-456";
        var name = "New Buyer";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.Is<IdentityUser>(u => u.Email == email)))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<IdentityUser>(u => u.Id = "new-user-id");
        mockUserManager.Setup(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.Is<UserLoginInfo>(l => l.LoginProvider == "Google")))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), BuyerRole))
            .ReturnsAsync(IdentityResult.Success);

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, name);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsNewUser);
        Assert.Equal(email, result.Email);
        mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>()), Times.Once);
        mockUserManager.Verify(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.IsAny<UserLoginInfo>()), Times.Once);
        mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), BuyerRole), Times.Once);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WhenUserCreationFails_ReturnsFailure()
    {
        // Arrange
        var email = "newbuyer@example.com";
        var googleId = "google-456";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Failed to create account", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WhenLinkingLoginFails_RollsBackUserCreation()
    {
        // Arrange
        var email = "newbuyer@example.com";
        var googleId = "google-456";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "External login failed" }));
        mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Failed to link Google account", result.ErrorMessage);
        mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WhenRoleAssignmentFails_RollsBackUserCreation()
    {
        // Arrange
        var email = "newbuyer@example.com";
        var googleId = "google-456";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), BuyerRole))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));
        mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Failed to assign buyer role", result.ErrorMessage);
        mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithNullEmail_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new GoogleLoginService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessGoogleLoginAsync(null!, "google-123", null));
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new GoogleLoginService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessGoogleLoginAsync("", "google-123", null));
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithNullGoogleId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new GoogleLoginService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessGoogleLoginAsync("test@example.com", null!, null));
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WithEmptyGoogleId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new GoogleLoginService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessGoogleLoginAsync("test@example.com", "", null));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GoogleLoginService(null!));
    }

    [Fact]
    public async Task ProcessGoogleLoginAsync_WhenLinkingExistingAccountFails_ReturnsFailure()
    {
        // Arrange
        var email = "buyer@example.com";
        var googleId = "google-123";

        var user = new IdentityUser { Id = "user-1", Email = email, UserName = email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>()); // No existing logins
        mockUserManager.Setup(x => x.AddLoginAsync(user, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Linking failed" }));

        var service = new GoogleLoginService(mockUserManager.Object);

        // Act
        var result = await service.ProcessGoogleLoginAsync(email, googleId, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Failed to link Google account", result.ErrorMessage);
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
