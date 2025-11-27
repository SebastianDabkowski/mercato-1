using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Identity;

public class PasswordChangeServiceTests
{
    [Fact]
    public async Task ChangePasswordAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsIncorrectCurrentPassword);
        Assert.False(result.IsUserNotFound);
        Assert.Empty(result.Errors);
        mockUserManager.Verify(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword), Times.Once);
        mockUserManager.Verify(x => x.UpdateSecurityStampAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ReturnsIncorrectCurrentPassword()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "WrongP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." }));

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsIncorrectCurrentPassword);
        Assert.False(result.IsUserNotFound);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ReturnsUserNotFound()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "non-existent-user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync((IdentityUser?)null);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsUserNotFound);
        Assert.False(result.IsIncorrectCurrentPassword);
        mockUserManager.Verify(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedConfirmation_ReturnsFailure()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "DifferentP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.IsIncorrectCurrentPassword);
        Assert.False(result.IsUserNotFound);
        Assert.Contains("do not match", result.Errors.First(), StringComparison.OrdinalIgnoreCase);
        mockUserManager.Verify(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWeakPassword_ReturnsFailure()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 8 characters." }));

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.IsIncorrectCurrentPassword);
        Assert.False(result.IsUserNotFound);
        Assert.Contains("Passwords must be at least 8 characters.", result.Errors);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("unexpected error", result.Errors.First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ChangePasswordAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PasswordChangeService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PasswordChangeService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PasswordChangeService(mockUserManager.Object, null!));
    }

    [Fact]
    public async Task ChangePasswordAsync_LogsPasswordChangeAttempt()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        await service.ChangePasswordAsync(command);

        // Assert - verify audit logging was called
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password change attempt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_SuccessfulChange_LogsSuccess()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        await service.ChangePasswordAsync(command);

        // Assert - verify audit logging was called for successful change
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password changed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HasPasswordAsync_WithValidUserWithPassword_ReturnsTrue()
    {
        // Arrange
        var userId = "user-id";
        var user = new IdentityUser { Id = userId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(true);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.HasPasswordAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasPasswordAsync_WithValidUserWithoutPassword_ReturnsFalse()
    {
        // Arrange
        var userId = "user-id";
        var user = new IdentityUser { Id = userId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(false);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.HasPasswordAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var userId = "non-existent-user-id";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser?)null);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.HasPasswordAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasPasswordAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.HasPasswordAsync(string.Empty));
    }

    [Fact]
    public async Task HasPasswordAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.HasPasswordAsync(null!));
    }

    [Fact]
    public async Task ChangePasswordAsync_SuccessfulChange_UpdatesSecurityStamp()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "user-id",
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = command.UserId, Email = "user@example.com", UserName = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<PasswordChangeService>>();
        var service = new PasswordChangeService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ChangePasswordAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        // Verify that UpdateSecurityStampAsync was called to invalidate other sessions
        mockUserManager.Verify(x => x.UpdateSecurityStampAsync(user), Times.Once);
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
