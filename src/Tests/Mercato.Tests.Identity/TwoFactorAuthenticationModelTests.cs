using Mercato.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Tests.Identity;

public class TwoFactorAuthenticationModelTests
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        // Arrange
        var model = CreateModel("test-user-id");

        // Act
        model.OnGet();

        // Assert - OnGet returns void, no explicit result to check
        // Verify model properties are set correctly
        Assert.False(model.IsTwoFactorAvailable);
        Assert.False(model.IsTwoFactorEnabled);
    }

    [Fact]
    public void IsTwoFactorAvailable_ReturnsFalse()
    {
        // Arrange
        var model = CreateModel("test-user-id");

        // Act
        var result = model.IsTwoFactorAvailable;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTwoFactorEnabled_ReturnsFalse()
    {
        // Arrange
        var model = CreateModel("test-user-id");

        // Act
        var result = model.IsTwoFactorEnabled;

        // Assert
        Assert.False(result);
    }

    private static TwoFactorAuthenticationModel CreateModel(string? userId)
    {
        var model = new TwoFactorAuthenticationModel();

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

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }
}
