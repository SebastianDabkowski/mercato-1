using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Mercato.Tests.Identity;

public class BuyerLoginServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentialsAndBuyerRole_ReturnsSuccess()
    {
        // Arrange
        var command = new LoginBuyerCommand
        {
            Email = "buyer@example.com",
            Password = "Test@123",
            RememberMe = false
        };

        var user = new IdentityUser { Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, "Buyer"))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var service = new BuyerLoginService(mockUserManager.Object);

        // Act
        var result = await service.LoginAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.IsLockedOut);
        mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsInvalidCredentials()
    {
        // Arrange
        var command = new LoginBuyerCommand
        {
            Email = "nonexistent@example.com",
            Password = "Test@123"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);

        var service = new BuyerLoginService(mockUserManager.Object);

        // Act
        var result = await service.LoginAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Invalid email or password", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsInvalidCredentialsAndIncrementsFailedCount()
    {
        // Arrange
        var command = new LoginBuyerCommand
        {
            Email = "buyer@example.com",
            Password = "WrongPassword"
        };

        var user = new IdentityUser { Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);
        mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var service = new BuyerLoginService(mockUserManager.Object);

        // Act
        var result = await service.LoginAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Invalid email or password", result.ErrorMessage);
        mockUserManager.Verify(x => x.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithLockedOutUser_ReturnsLockedOut()
    {
        // Arrange
        var command = new LoginBuyerCommand
        {
            Email = "buyer@example.com",
            Password = "Test@123"
        };

        var user = new IdentityUser { Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        var service = new BuyerLoginService(mockUserManager.Object);

        // Act
        var result = await service.LoginAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsLockedOut);
        Assert.Contains("locked", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordFailureCausesLockout_ReturnsLockedOut()
    {
        // Arrange
        var command = new LoginBuyerCommand
        {
            Email = "buyer@example.com",
            Password = "WrongPassword"
        };

        var user = new IdentityUser { Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.SetupSequence(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false)  // First check before password verification
            .ReturnsAsync(true);  // Second check after failed attempt
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);
        mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var service = new BuyerLoginService(mockUserManager.Object);

        // Act
        var result = await service.LoginAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsLockedOut);
        Assert.Contains("locked", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithUserNotInBuyerRole_ReturnsNotABuyer()
    {
        // Arrange
        var command = new LoginBuyerCommand
        {
            Email = "seller@example.com",
            Password = "Test@123"
        };

        var user = new IdentityUser { Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, "Buyer"))
            .ReturnsAsync(false);

        var service = new BuyerLoginService(mockUserManager.Object);

        // Act
        var result = await service.LoginAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("buyers only", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new BuyerLoginService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.LoginAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BuyerLoginService(null!));
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
