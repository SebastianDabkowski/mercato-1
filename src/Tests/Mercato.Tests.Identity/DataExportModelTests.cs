using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
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

public class DataExportModelTests
{
    [Fact]
    public async Task OnPostAsync_SuccessfulExport_ReturnsFileResult()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var exportData = "{\"identity\":{\"email\":\"test@example.com\"}}";
        var exportedAt = DateTimeOffset.UtcNow;

        var mockDataExportService = new Mock<IUserDataExportService>(MockBehavior.Strict);
        var mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);

        mockDataExportService.Setup(x => x.ExportUserDataAsync(userId))
            .ReturnsAsync(UserDataExportResult.Success(exportData, exportedAt));
        mockAdminAuditRepository.Setup(x => x.AddAsync(It.IsAny<AdminAuditLog>()))
            .ReturnsAsync((AdminAuditLog log) => log);

        var model = CreateModel(userId, mockDataExportService.Object, mockAdminAuditRepository.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/json", fileResult.ContentType);
        Assert.StartsWith("mercato-data-export-", fileResult.FileDownloadName);
        Assert.EndsWith(".json", fileResult.FileDownloadName);
        Assert.Equal(exportData, System.Text.Encoding.UTF8.GetString(fileResult.FileContents));

        mockDataExportService.VerifyAll();
        mockAdminAuditRepository.VerifyAll();
    }

    [Fact]
    public async Task OnPostAsync_SuccessfulExport_CreatesAuditLogEntry()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var exportData = "{\"identity\":{\"email\":\"test@example.com\"}}";
        var exportedAt = DateTimeOffset.UtcNow;

        var mockDataExportService = new Mock<IUserDataExportService>(MockBehavior.Strict);
        var mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);

        mockDataExportService.Setup(x => x.ExportUserDataAsync(userId))
            .ReturnsAsync(UserDataExportResult.Success(exportData, exportedAt));

        AdminAuditLog? capturedLog = null;
        mockAdminAuditRepository.Setup(x => x.AddAsync(It.IsAny<AdminAuditLog>()))
            .Callback<AdminAuditLog>(log => capturedLog = log)
            .ReturnsAsync((AdminAuditLog log) => log);

        var model = CreateModel(userId, mockDataExportService.Object, mockAdminAuditRepository.Object);

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Equal(userId, capturedLog.AdminUserId);
        Assert.Equal("DataExport", capturedLog.Action);
        Assert.Equal("User", capturedLog.EntityType);
        Assert.Equal(userId, capturedLog.EntityId);
        Assert.Contains("GDPR data export", capturedLog.Details);
    }

    [Fact]
    public async Task OnPostAsync_ExportFails_RedirectsToPageWithError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var mockDataExportService = new Mock<IUserDataExportService>(MockBehavior.Strict);
        var mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);

        mockDataExportService.Setup(x => x.ExportUserDataAsync(userId))
            .ReturnsAsync(UserDataExportResult.Failure("Export failed"));

        var model = CreateModel(userId, mockDataExportService.Object, mockAdminAuditRepository.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.True(model.TempData.ContainsKey("StatusMessage"));
        Assert.True(model.TempData.ContainsKey("IsError"));
        Assert.Equal(true, model.TempData["IsError"]);
    }

    [Fact]
    public async Task OnPostAsync_UserNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var mockDataExportService = new Mock<IUserDataExportService>(MockBehavior.Strict);
        var mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);

        var model = CreateModel(null, mockDataExportService.Object, mockAdminAuditRepository.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirectResult.PageName);
    }

    [Fact]
    public void OnGet_UserAuthenticated_ReturnsPage()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockDataExportService = new Mock<IUserDataExportService>(MockBehavior.Strict);
        var mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);

        var model = CreateModel(userId, mockDataExportService.Object, mockAdminAuditRepository.Object);

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public void OnGet_UserNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var mockDataExportService = new Mock<IUserDataExportService>(MockBehavior.Strict);
        var mockAdminAuditRepository = new Mock<IAdminAuditRepository>(MockBehavior.Strict);

        var model = CreateModel(null, mockDataExportService.Object, mockAdminAuditRepository.Object);

        // Act
        var result = model.OnGet();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirectResult.PageName);
    }

    private static DataExportModel CreateModel(
        string? userId,
        IUserDataExportService dataExportService,
        IAdminAuditRepository adminAuditRepository)
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var mockLogger = new Mock<ILogger<DataExportModel>>();

        var model = new DataExportModel(
            dataExportService,
            mockUserManager.Object,
            adminAuditRepository,
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
}
