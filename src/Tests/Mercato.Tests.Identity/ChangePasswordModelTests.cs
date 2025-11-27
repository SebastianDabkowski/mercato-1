using Mercato.Admin.Application.Services;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Mercato.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Mercato.Tests.Identity;

public class ChangePasswordModelTests
{
    [Fact]
    public async Task OnPostAsync_SuccessfulPasswordChange_SignsOutAndRedirectsToLogin()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockPasswordChangeService = new Mock<IPasswordChangeService>(MockBehavior.Strict);
        var mockSignInManager = CreateMockSignInManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);

        mockPasswordChangeService.Setup(x => x.HasPasswordAsync(userId))
            .ReturnsAsync(true);
        mockPasswordChangeService.Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordCommand>()))
            .ReturnsAsync(ChangePasswordResult.Success());
        mockSignInManager.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);
        mockAuthEventService.Setup(x => x.LogEventAsync(
                It.IsAny<Mercato.Admin.Domain.Entities.AuthenticationEventType>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var model = CreateModel(userId, mockPasswordChangeService.Object, mockSignInManager.Object, mockAuthEventService.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirectResult.PageName);

        // Verify that SignOutAsync was called
        mockSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_SuccessfulPasswordChange_SetsTempDataWithSuccessMessage()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockPasswordChangeService = new Mock<IPasswordChangeService>(MockBehavior.Strict);
        var mockSignInManager = CreateMockSignInManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);

        mockPasswordChangeService.Setup(x => x.HasPasswordAsync(userId))
            .ReturnsAsync(true);
        mockPasswordChangeService.Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordCommand>()))
            .ReturnsAsync(ChangePasswordResult.Success());
        mockSignInManager.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);
        mockAuthEventService.Setup(x => x.LogEventAsync(
                It.IsAny<Mercato.Admin.Domain.Entities.AuthenticationEventType>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var model = CreateModel(userId, mockPasswordChangeService.Object, mockSignInManager.Object, mockAuthEventService.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.True(model.TempData.ContainsKey("StatusMessage"));
        Assert.Contains("Please log in with your new password", model.TempData["StatusMessage"]?.ToString());
    }

    [Fact]
    public async Task OnPostAsync_FailedPasswordChange_DoesNotSignOut()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockPasswordChangeService = new Mock<IPasswordChangeService>(MockBehavior.Strict);
        var mockSignInManager = CreateMockSignInManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);

        mockPasswordChangeService.Setup(x => x.HasPasswordAsync(userId))
            .ReturnsAsync(true);
        mockPasswordChangeService.Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordCommand>()))
            .ReturnsAsync(ChangePasswordResult.IncorrectCurrentPassword());
        mockAuthEventService.Setup(x => x.LogEventAsync(
                It.IsAny<Mercato.Admin.Domain.Entities.AuthenticationEventType>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var model = CreateModel(userId, mockPasswordChangeService.Object, mockSignInManager.Object, mockAuthEventService.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "WrongP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Verify that SignOutAsync was NOT called
        mockSignInManager.Verify(x => x.SignOutAsync(), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_IncorrectCurrentPassword_AddsModelError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockPasswordChangeService = new Mock<IPasswordChangeService>(MockBehavior.Strict);
        var mockSignInManager = CreateMockSignInManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);

        mockPasswordChangeService.Setup(x => x.HasPasswordAsync(userId))
            .ReturnsAsync(true);
        mockPasswordChangeService.Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordCommand>()))
            .ReturnsAsync(ChangePasswordResult.IncorrectCurrentPassword());
        mockAuthEventService.Setup(x => x.LogEventAsync(
                It.IsAny<Mercato.Admin.Domain.Entities.AuthenticationEventType>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var model = CreateModel(userId, mockPasswordChangeService.Object, mockSignInManager.Object, mockAuthEventService.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "WrongP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("CurrentPassword"));
    }

    [Fact]
    public async Task OnPostAsync_UserNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var mockPasswordChangeService = new Mock<IPasswordChangeService>(MockBehavior.Strict);
        var mockSignInManager = CreateMockSignInManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);

        var model = CreateModel(null, mockPasswordChangeService.Object, mockSignInManager.Object, mockAuthEventService.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirectResult.PageName);
    }

    [Fact]
    public async Task OnPostAsync_UserHasNoPassword_ReturnsPageWithError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockPasswordChangeService = new Mock<IPasswordChangeService>(MockBehavior.Strict);
        var mockSignInManager = CreateMockSignInManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);

        mockPasswordChangeService.Setup(x => x.HasPasswordAsync(userId))
            .ReturnsAsync(false);

        var model = CreateModel(userId, mockPasswordChangeService.Object, mockSignInManager.Object, mockAuthEventService.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "OldP@ssw0rd!",
            NewPassword = "NewP@ssw0rd!",
            ConfirmPassword = "NewP@ssw0rd!"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.IsError);
        Assert.Contains("social login", model.StatusMessage, StringComparison.OrdinalIgnoreCase);

        // Verify that SignOutAsync was NOT called
        mockSignInManager.Verify(x => x.SignOutAsync(), Times.Never);
    }

    private static ChangePasswordModel CreateModel(
        string? userId,
        IPasswordChangeService passwordChangeService,
        SignInManager<IdentityUser> signInManager,
        IAuthenticationEventService authEventService)
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var mockLogger = new Mock<ILogger<ChangePasswordModel>>();

        var model = new ChangePasswordModel(
            passwordChangeService,
            mockUserManager.Object,
            signInManager,
            authEventService,
            mockLogger.Object);

        // Set up HttpContext with user claims
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.Add(new Claim(ClaimTypes.Name, "test@example.com"));
        }

        var identity = !string.IsNullOrEmpty(userId)
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        // Create TempData
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        var tempData = tempDataDictionaryFactory.GetTempData(httpContext);

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };
        model.TempData = tempData;

        return model;
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<IdentityUser>> CreateMockSignInManager()
    {
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        var mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var mockContextAccessor = new Mock<IHttpContextAccessor>();
        var mockClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();

        return new Mock<SignInManager<IdentityUser>>(
            mockUserManager.Object,
            mockContextAccessor.Object,
            mockClaimsPrincipalFactory.Object,
            null!, null!, null!, null!);
    }
}
