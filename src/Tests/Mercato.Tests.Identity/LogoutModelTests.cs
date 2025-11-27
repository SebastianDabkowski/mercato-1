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

        var model = CreateModel(
            authenticated: true, 
            email: "user@example.com",
            signInManager: mockSignInManager.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowConfirmation);
        mockSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_WhenUserNotAuthenticated_ReturnsPageWithoutSignOut()
    {
        // Arrange
        var mockSignInManager = CreateMockSignInManager();
        var model = CreateModel(
            authenticated: false,
            signInManager: mockSignInManager.Object);

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
        SignInManager<IdentityUser>? signInManager = null)
    {
        signInManager ??= CreateMockSignInManager().Object;
        var mockLogger = new Mock<ILogger<LogoutModel>>();
        
        var model = new LogoutModel(signInManager, mockLogger.Object);

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
