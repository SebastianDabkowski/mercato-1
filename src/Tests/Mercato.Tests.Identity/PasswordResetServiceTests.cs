using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Identity;

public class PasswordResetServiceTests
{
    [Fact]
    public async Task RequestPasswordResetAsync_WithExistingUser_ReturnsSuccessWithToken()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = "user@example.com"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };
        var expectedToken = "reset-token-123";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(expectedToken);

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.RequestPasswordResetAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedToken, result.ResetToken);
        Assert.Equal(user.Id, result.UserId);
        Assert.Null(result.ErrorMessage);
        mockUserManager.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistentUser_ReturnsSuccessToPreventEnumeration()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = "nonexistent@example.com"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.RequestPasswordResetAsync(command);

        // Assert
        // Should return success to prevent email enumeration attacks
        Assert.True(result.Succeeded);
        Assert.Null(result.ResetToken);
        Assert.Null(result.UserId);
        Assert.Null(result.ErrorMessage);
        mockUserManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenTokenGenerationFails_ReturnsFailure()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = "user@example.com"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ThrowsAsync(new InvalidOperationException("Token generation failed"));

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.RequestPasswordResetAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ResetToken);
        Assert.Contains("unexpected error", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RequestPasswordResetAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PasswordResetService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PasswordResetService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PasswordResetService(mockUserManager.Object, null!));
    }

    [Fact]
    public async Task RequestPasswordResetAsync_LogsPasswordResetRequest()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = "user@example.com"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };
        var expectedToken = "reset-token-123";

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(expectedToken);

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        await service.RequestPasswordResetAsync(command);

        // Assert - verify logging was called (audit logging requirement)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password reset requested")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ResetPasswordAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsInvalidToken);
        Assert.False(result.IsExpiredToken);
        Assert.False(result.IsUserNotFound);
        Assert.Empty(result.Errors);
        mockUserManager.Verify(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ReturnsInvalidToken()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "user@example.com",
            Token = "invalid-token",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." }));

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ResetPasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsInvalidToken);
        Assert.False(result.IsUserNotFound);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonExistentUser_ReturnsUserNotFound()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "nonexistent@example.com",
            Token = "some-token",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ResetPasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsUserNotFound);
        Assert.False(result.IsInvalidToken);
        Assert.False(result.IsExpiredToken);
        mockUserManager.Verify(x => x.ResetPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ResetPasswordAsync(null!));
    }

    [Fact]
    public async Task ResetPasswordAsync_LogsPasswordReset()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        await service.ResetPasswordAsync(command);

        // Assert - verify audit logging was called for successful password reset
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password reset successful")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithPasswordValidationError_ReturnsFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 8 characters." }));

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ResetPasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.IsInvalidToken);
        Assert.False(result.IsUserNotFound);
        Assert.Contains("Passwords must be at least 8 characters.", result.Errors);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        var user = new IdentityUser { Id = "user-id", Email = command.Email, UserName = command.Email };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var mockLogger = new Mock<ILogger<PasswordResetService>>();
        var service = new PasswordResetService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.ResetPasswordAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("unexpected error", result.Errors.First(), StringComparison.OrdinalIgnoreCase);
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
