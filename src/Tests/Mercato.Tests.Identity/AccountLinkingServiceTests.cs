using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Mercato.Tests.Identity;

public class AccountLinkingServiceTests
{
    private const string BuyerRole = "Buyer";

    [Fact]
    public async Task GetLinkedAccountsAsync_WithValidUser_ReturnsLinkedAccounts()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var logins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-123", "Google"),
            new UserLoginInfo("Facebook", "facebook-456", "Facebook")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.GetLinkedAccountsAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.ProviderName == "Google");
        Assert.Contains(result, a => a.ProviderName == "Facebook");
    }

    [Fact]
    public async Task GetLinkedAccountsAsync_WithNonExistentUser_ReturnsEmptyList()
    {
        // Arrange
        var userId = "non-existent";
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.GetLinkedAccountsAsync(userId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinkedAccountsAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetLinkedAccountsAsync(null!));
    }

    [Fact]
    public async Task LinkAccountAsync_WithValidBuyer_LinksAccount()
    {
        // Arrange
        var userId = "user-1";
        var provider = "Google";
        var providerKey = "google-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());
        mockUserManager.Setup(x => x.AddLoginAsync(user, It.Is<UserLoginInfo>(l => l.LoginProvider == provider && l.ProviderKey == providerKey)))
            .ReturnsAsync(IdentityResult.Success);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.LinkAccountAsync(userId, provider, providerKey);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.WasAlreadyLinked);
        mockUserManager.Verify(x => x.AddLoginAsync(user, It.IsAny<UserLoginInfo>()), Times.Once);
    }

    [Fact]
    public async Task LinkAccountAsync_WhenAlreadyLinked_ReturnsAlreadyLinked()
    {
        // Arrange
        var userId = "user-1";
        var provider = "Google";
        var providerKey = "google-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var existingLogins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-existing", "Google")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(existingLogins);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.LinkAccountAsync(userId, provider, providerKey);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.WasAlreadyLinked);
        mockUserManager.Verify(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
    }

    [Fact]
    public async Task LinkAccountAsync_WithNonBuyer_ReturnsNotABuyer()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(false);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.LinkAccountAsync(userId, "Google", "google-123");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("only available for buyers", result.ErrorMessage);
    }

    [Fact]
    public async Task LinkAccountAsync_WithNonExistentUser_ReturnsUserNotFound()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("non-existent"))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.LinkAccountAsync("non-existent", "Google", "google-123");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task LinkAccountAsync_WhenAddLoginFails_ReturnsFailure()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());
        mockUserManager.Setup(x => x.AddLoginAsync(user, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Linking failed" }));

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.LinkAccountAsync(userId, "Google", "google-123");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Failed to link", result.ErrorMessage);
    }

    [Fact]
    public async Task UnlinkAccountAsync_WithValidBuyer_UnlinksAccount()
    {
        // Arrange
        var userId = "user-1";
        var provider = "Google";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var existingLogins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-123", "Google"),
            new UserLoginInfo("Facebook", "facebook-456", "Facebook")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(existingLogins);
        mockUserManager.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.RemoveLoginAsync(user, provider, "google-123"))
            .ReturnsAsync(IdentityResult.Success);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.UnlinkAccountAsync(userId, provider);

        // Assert
        Assert.True(result.Succeeded);
        mockUserManager.Verify(x => x.RemoveLoginAsync(user, provider, "google-123"), Times.Once);
    }

    [Fact]
    public async Task UnlinkAccountAsync_WhenOnlyLoginMethodWithoutPassword_ReturnsFailure()
    {
        // Arrange
        var userId = "user-1";
        var provider = "Google";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var existingLogins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-123", "Google")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(existingLogins);
        mockUserManager.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(false);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.UnlinkAccountAsync(userId, provider);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot unlink the only login method", result.ErrorMessage);
    }

    [Fact]
    public async Task UnlinkAccountAsync_WhenProviderNotLinked_ReturnsNotLinked()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var existingLogins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Facebook", "facebook-456", "Facebook")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(existingLogins);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.UnlinkAccountAsync(userId, "Google");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("not linked", result.ErrorMessage);
    }

    [Fact]
    public async Task UnlinkAccountAsync_WithNonExistentUser_ReturnsUserNotFound()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("non-existent"))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.UnlinkAccountAsync("non-existent", "Google");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task UnlinkAccountAsync_WithNonBuyer_ReturnsNotABuyer()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, BuyerRole))
            .ReturnsAsync(false);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.UnlinkAccountAsync(userId, "Google");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("only available for buyers", result.ErrorMessage);
    }

    [Fact]
    public async Task IsProviderLinkedAsync_WhenLinked_ReturnsTrue()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var logins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-123", "Google")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.IsProviderLinkedAsync(userId, "Google");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsProviderLinkedAsync_WhenNotLinked_ReturnsFalse()
    {
        // Arrange
        var userId = "user-1";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var logins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-123", "Google")
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.IsProviderLinkedAsync(userId, "Facebook");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsProviderLinkedAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("non-existent"))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountLinkingService(mockUserManager.Object);

        // Act
        var result = await service.IsProviderLinkedAsync("non-existent", "Google");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AccountLinkingService(null!));
    }

    [Fact]
    public async Task LinkAccountAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.LinkAccountAsync(null!, "Google", "key"));
    }

    [Fact]
    public async Task LinkAccountAsync_WithEmptyProvider_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.LinkAccountAsync("user-1", "", "key"));
    }

    [Fact]
    public async Task LinkAccountAsync_WithNullProviderKey_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.LinkAccountAsync("user-1", "Google", null!));
    }

    [Fact]
    public async Task UnlinkAccountAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.UnlinkAccountAsync("", "Google"));
    }

    [Fact]
    public async Task UnlinkAccountAsync_WithNullProvider_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UnlinkAccountAsync("user-1", null!));
    }

    [Fact]
    public async Task IsProviderLinkedAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.IsProviderLinkedAsync("", "Google"));
    }

    [Fact]
    public async Task IsProviderLinkedAsync_WithNullProvider_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new AccountLinkingService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.IsProviderLinkedAsync("user-1", null!));
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
