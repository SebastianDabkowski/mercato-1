using Mercato.Identity.Application.Commands;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Identity;

public class SellerRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

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
        var existingUser = new IdentityUser { Email = "seller@example.com" };
        var command = CreateValidCommand();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

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
        var command = CreateValidCommand();
        command.Password = "weak";
        command.ConfirmPassword = "weak";

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

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

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
        var command = CreateValidCommand();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role not found." }));
        mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("assign buyer role", result.Errors[0]);
        mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenClaimsAssignmentFails_StillReturnsSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Claims failed." }));

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        // Even if claims fail, registration should succeed since the account is valid
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task RegisterAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SellerRegistrationService(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SellerRegistrationService(mockUserManager.Object, null!));
    }

    [Fact]
    public async Task RegisterAsync_SetsPhoneNumberOnUser()
    {
        // Arrange
        var command = CreateValidCommand();
        IdentityUser? capturedUser = null;

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .Callback<IdentityUser, string>((user, _) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

        // Act
        await service.RegisterAsync(command);

        // Assert
        Assert.NotNull(capturedUser);
        Assert.Equal(command.PhoneNumber, capturedUser.PhoneNumber);
    }

    [Fact]
    public async Task RegisterAsync_AddsBusinessClaimsToUser()
    {
        // Arrange
        var command = CreateValidCommand();
        IEnumerable<System.Security.Claims.Claim>? capturedClaims = null;

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((IdentityUser?)null);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Buyer"))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
            .Callback<IdentityUser, IEnumerable<System.Security.Claims.Claim>>((_, claims) => capturedClaims = claims)
            .ReturnsAsync(IdentityResult.Success);

        var mockLogger = new Mock<ILogger<SellerRegistrationService>>();
        var service = new SellerRegistrationService(mockUserManager.Object, mockLogger.Object);

        // Act
        await service.RegisterAsync(command);

        // Assert
        Assert.NotNull(capturedClaims);
        var claimsList = capturedClaims.ToList();
        Assert.Equal(4, claimsList.Count);
        Assert.Contains(claimsList, c => c.Type == "BusinessName" && c.Value == command.BusinessName);
        Assert.Contains(claimsList, c => c.Type == "BusinessAddress" && c.Value == command.BusinessAddress);
        Assert.Contains(claimsList, c => c.Type == "TaxId" && c.Value == command.TaxId);
        Assert.Contains(claimsList, c => c.Type == "ContactName" && c.Value == command.ContactName);
    }

    private static RegisterSellerCommand CreateValidCommand()
    {
        return new RegisterSellerCommand
        {
            Email = "seller@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            BusinessName = "Test Business",
            BusinessAddress = "123 Test Street, City, Country",
            TaxId = "TAX123456789",
            PhoneNumber = "+1234567890",
            ContactName = "John Doe"
        };
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
