using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Mercato.Tests.Identity;

public class BuyerRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new RegisterBuyerCommand
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Success);

        var service = new BuyerRegistrationService(mockUserManager.Object);

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password), Times.Once);
        mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsError()
    {
        // Arrange
        var existingUser = new IdentityUser { Email = "test@example.com" };
        var command = new RegisterBuyerCommand
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        var service = new BuyerRegistrationService(mockUserManager.Object);

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("already exists", result.Errors[0]);
        mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ReturnsPasswordErrors()
    {
        // Arrange
        var command = new RegisterBuyerCommand
        {
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        var passwordErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 6 characters." },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Password requires at least one digit." }
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(passwordErrors));

        var service = new BuyerRegistrationService(mockUserManager.Object);

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("at least 6 characters", result.Errors[0]);
        Assert.Contains("requires at least one digit", result.Errors[1]);
    }

    [Fact]
    public async Task RegisterAsync_WhenRoleAssignmentFails_DeletesUserAndReturnsError()
    {
        // Arrange
        var command = new RegisterBuyerCommand
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role not found." }));
        mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new BuyerRegistrationService(mockUserManager.Object);

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("assign buyer role", result.Errors[0]);
        mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var service = new BuyerRegistrationService(mockUserManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BuyerRegistrationService(null!));
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
