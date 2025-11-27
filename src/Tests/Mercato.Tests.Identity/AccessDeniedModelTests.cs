using Mercato.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Mercato.Tests.Identity;

public class AccessDeniedModelTests
{
    [Fact]
    public void OnGet_WhenUserAuthenticated_LogsWarningWithUserIdentity()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AccessDeniedModel>>();
        var model = CreateModel(
            authenticated: true, 
            email: "user@example.com",
            requestPath: "/Admin/SecurePage",
            mockLogger: mockLogger);

        // Act
        model.OnGet();

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user@example.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnGet_WhenUserNotAuthenticated_LogsWarningAsAnonymous()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AccessDeniedModel>>();
        var model = CreateModel(
            authenticated: false,
            requestPath: "/Admin/SecurePage",
            mockLogger: mockLogger);

        // Act
        model.OnGet();

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anonymous")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnGet_ReturnsPage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AccessDeniedModel>>();
        var model = CreateModel(
            authenticated: true,
            email: "user@example.com",
            requestPath: "/Admin/SecurePage",
            mockLogger: mockLogger);

        // Act - OnGet doesn't return anything, just verify it doesn't throw
        var exception = Record.Exception(() => model.OnGet());

        // Assert
        Assert.Null(exception);
    }

    private static AccessDeniedModel CreateModel(
        bool authenticated,
        string? email = null,
        string requestPath = "/",
        Mock<ILogger<AccessDeniedModel>>? mockLogger = null)
    {
        mockLogger ??= new Mock<ILogger<AccessDeniedModel>>();
        
        var model = new AccessDeniedModel(mockLogger.Object);

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
        httpContext.Request.Path = requestPath;

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }
}
