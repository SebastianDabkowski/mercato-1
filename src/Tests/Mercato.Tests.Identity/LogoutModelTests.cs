using Mercato.Admin.Application.Services;
using Mercato.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Mercato.Tests.Identity;

public class LogoutModelTests
{
    [Fact]
    public void OnGet_WhenUserNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var model = CreateModel(authenticated: false);

        // Act
        var result = model.OnGet();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirectResult.PageName);
    }

    [Fact]
    public void OnGet_WhenUserAuthenticated_ShowsConfirmation()
    {
        // Arrange
        var model = CreateModel(authenticated: true, email: "user@example.com");

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.ShowConfirmation);
    }

    [Fact]
    public async Task OnPostAsync_WhenUserAuthenticated_SignsOutAndShowsLoggedOutMessage()
    {
        // Arrange
        var mockSignInManager = CreateMockSignInManager();
        mockSignInManager.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal p) => p.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);
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

        var model = CreateModel(
            authenticated: true, 
            email: "user@example.com",
            signInManager: mockSignInManager.Object,
            userManager: mockUserManager.Object,
            authEventService: mockAuthEventService.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowConfirmation);
        mockSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
        mockAuthEventService.Verify(x => x.LogEventAsync(
            Mercato.Admin.Domain.Entities.AuthenticationEventType.Logout,
            "user@example.com",
            true,
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_WhenUserNotAuthenticated_ReturnsPageWithoutSignOut()
    {
        // Arrange
        var mockSignInManager = CreateMockSignInManager();
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventService = new Mock<IAuthenticationEventService>(MockBehavior.Strict);
        var model = CreateModel(
            authenticated: false,
            signInManager: mockSignInManager.Object,
            userManager: mockUserManager.Object,
            authEventService: mockAuthEventService.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowConfirmation);
        mockSignInManager.Verify(x => x.SignOutAsync(), Times.Never);
    }

    private static LogoutModel CreateModel(
        bool authenticated,
        string? email = null,
        SignInManager<IdentityUser>? signInManager = null,
        UserManager<IdentityUser>? userManager = null,
        IAuthenticationEventService? authEventService = null)
    {
        signInManager ??= CreateMockSignInManager().Object;
        userManager ??= CreateMockUserManager().Object;
        authEventService ??= new Mock<IAuthenticationEventService>().Object;
        var mockLogger = new Mock<ILogger<LogoutModel>>();
        
        var model = new LogoutModel(signInManager, userManager, authEventService, mockLogger.Object);

        // Set up HttpContext with user claims
        var claims = new List<Claim>();
        if (authenticated && !string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
            claims.Add(new Claim(ClaimTypes.Name, email));
        }

        var identity = authenticated
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
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
