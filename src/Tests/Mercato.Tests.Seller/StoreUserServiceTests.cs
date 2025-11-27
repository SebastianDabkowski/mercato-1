using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class StoreUserServiceTests
{
    private const string TestSellerId = "seller-test-123";
    private const string TestUserId = "user-test-456";
    private const string TestEmail = "test@example.com";
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestStoreUserId = Guid.NewGuid();

    private readonly Mock<IStoreUserRepository> _mockRepository;
    private readonly Mock<IStoreRepository> _mockStoreRepository;
    private readonly Mock<ILogger<StoreUserService>> _mockLogger;
    private readonly StoreUserService _service;

    public StoreUserServiceTests()
    {
        _mockRepository = new Mock<IStoreUserRepository>(MockBehavior.Strict);
        _mockStoreRepository = new Mock<IStoreRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<StoreUserService>>();
        _service = new StoreUserService(
            _mockRepository.Object,
            _mockStoreRepository.Object,
            _mockLogger.Object);
    }

    #region GetStoreUsersAsync Tests

    [Fact]
    public async Task GetStoreUsersAsync_ReturnsUsersList()
    {
        // Arrange
        var expectedUsers = new List<StoreUser>
        {
            CreateTestStoreUser(),
            CreateTestStoreUser()
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _service.GetStoreUsersAsync(TestStoreId);

        // Assert
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetByStoreIdAsync(TestStoreId), Times.Once);
    }

    #endregion

    #region HasStoreAccessAsync Tests

    [Fact]
    public async Task HasStoreAccessAsync_WhenUserIsActive_ReturnsTrue()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active;
        _mockRepository.Setup(r => r.GetByStoreAndUserIdAsync(TestStoreId, TestUserId))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.HasStoreAccessAsync(TestStoreId, TestUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasStoreAccessAsync_WhenUserIsDeactivated_ReturnsFalse()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Deactivated;
        _mockRepository.Setup(r => r.GetByStoreAndUserIdAsync(TestStoreId, TestUserId))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.HasStoreAccessAsync(TestStoreId, TestUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasStoreAccessAsync_WhenUserNotFound_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByStoreAndUserIdAsync(TestStoreId, TestUserId))
            .ReturnsAsync((StoreUser?)null);

        // Act
        var result = await _service.HasStoreAccessAsync(TestStoreId, TestUserId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetUserRoleAsync Tests

    [Fact]
    public async Task GetUserRoleAsync_WhenUserIsActive_ReturnsRole()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active;
        storeUser.Role = StoreRole.CatalogManager;
        _mockRepository.Setup(r => r.GetByStoreAndUserIdAsync(TestStoreId, TestUserId))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.GetUserRoleAsync(TestStoreId, TestUserId);

        // Assert
        Assert.Equal(StoreRole.CatalogManager, result);
    }

    [Fact]
    public async Task GetUserRoleAsync_WhenUserIsDeactivated_ReturnsNull()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Deactivated;
        _mockRepository.Setup(r => r.GetByStoreAndUserIdAsync(TestStoreId, TestUserId))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.GetUserRoleAsync(TestStoreId, TestUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region InviteUserAsync Tests

    [Fact]
    public async Task InviteUserAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidInviteCommand();
        var store = CreateTestStore();

        _mockStoreRepository.Setup(r => r.GetByIdAsync(command.StoreId))
            .ReturnsAsync(store);
        _mockRepository.Setup(r => r.EmailExistsForStoreAsync(command.StoreId, command.Email.ToLowerInvariant()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<StoreUser>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.InviteUserAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.StoreUserId);
        Assert.NotNull(result.InvitationToken);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<StoreUser>(su =>
            su.Email == command.Email.ToLowerInvariant() &&
            su.Role == command.Role &&
            su.Status == StoreUserStatus.Pending &&
            su.InvitedBy == command.InvitedBy &&
            su.InvitationToken != null)), Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_WhenStoreNotFound_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidInviteCommand();
        _mockStoreRepository.Setup(r => r.GetByIdAsync(command.StoreId))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.InviteUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Store not found"));
    }

    [Fact]
    public async Task InviteUserAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidInviteCommand();
        var store = CreateTestStore();

        _mockStoreRepository.Setup(r => r.GetByIdAsync(command.StoreId))
            .ReturnsAsync(store);
        _mockRepository.Setup(r => r.EmailExistsForStoreAsync(command.StoreId, command.Email.ToLowerInvariant()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.InviteUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task InviteUserAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidInviteCommand();
        command.Email = "not-an-email";

        // Act
        var result = await _service.InviteUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("valid email"));
    }

    [Fact]
    public async Task InviteUserAsync_WithEmptyEmail_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidInviteCommand();
        command.Email = "";

        // Act
        var result = await _service.InviteUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("required"));
    }

    #endregion

    #region AcceptInvitationAsync Tests

    [Fact]
    public async Task AcceptInvitationAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Pending;
        storeUser.InvitationToken = "valid-token";
        storeUser.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1);
        storeUser.Email = TestEmail.ToLowerInvariant();

        var command = new AcceptStoreUserInvitationCommand
        {
            Token = "valid-token",
            UserId = TestUserId,
            Email = TestEmail
        };

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync(command.Token))
            .ReturnsAsync(storeUser);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<StoreUser>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AcceptInvitationAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(storeUser.StoreId, result.StoreId);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<StoreUser>(su =>
            su.UserId == command.UserId &&
            su.Status == StoreUserStatus.Active &&
            su.InvitationToken == null)), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Pending;
        storeUser.InvitationToken = "expired-token";
        storeUser.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        storeUser.Email = TestEmail.ToLowerInvariant();

        var command = new AcceptStoreUserInvitationCommand
        {
            Token = "expired-token",
            UserId = TestUserId,
            Email = TestEmail
        };

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync(command.Token))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("expired"));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var command = new AcceptStoreUserInvitationCommand
        {
            Token = "invalid-token",
            UserId = TestUserId,
            Email = TestEmail
        };

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync(command.Token))
            .ReturnsAsync((StoreUser?)null);

        // Act
        var result = await _service.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Invalid invitation token"));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithMismatchedEmail_ReturnsFailure()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Pending;
        storeUser.InvitationToken = "valid-token";
        storeUser.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1);
        storeUser.Email = "different@example.com";

        var command = new AcceptStoreUserInvitationCommand
        {
            Token = "valid-token",
            UserId = TestUserId,
            Email = TestEmail
        };

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync(command.Token))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("does not match"));
    }

    #endregion

    #region UpdateUserRoleAsync Tests

    [Fact]
    public async Task UpdateUserRoleAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active;
        storeUser.Role = StoreRole.CatalogManager;

        var command = new UpdateStoreUserRoleCommand
        {
            StoreUserId = storeUser.Id,
            StoreId = storeUser.StoreId,
            NewRole = StoreRole.OrderManager,
            ChangedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync(storeUser);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<StoreUser>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserRoleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<StoreUser>(su =>
            su.Role == StoreRole.OrderManager &&
            su.RoleChangedBy == TestSellerId)), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateStoreUserRoleCommand
        {
            StoreUserId = Guid.NewGuid(),
            StoreId = TestStoreId,
            NewRole = StoreRole.OrderManager,
            ChangedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync((StoreUser?)null);

        // Act
        var result = await _service.UpdateUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserIsDeactivated_ReturnsFailure()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Deactivated;

        var command = new UpdateStoreUserRoleCommand
        {
            StoreUserId = storeUser.Id,
            StoreId = storeUser.StoreId,
            NewRole = StoreRole.OrderManager,
            ChangedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.UpdateUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("deactivated"));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenChangingOnlyOwner_ReturnsFailure()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active;
        storeUser.Role = StoreRole.StoreOwner;

        var command = new UpdateStoreUserRoleCommand
        {
            StoreUserId = storeUser.Id,
            StoreId = storeUser.StoreId,
            NewRole = StoreRole.CatalogManager,
            ChangedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync(storeUser);
        _mockRepository.Setup(r => r.GetByStoreIdAsync(storeUser.StoreId))
            .ReturnsAsync(new List<StoreUser> { storeUser });

        // Act
        var result = await _service.UpdateUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("only store owner"));
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active;
        storeUser.Role = StoreRole.CatalogManager;

        var command = new DeactivateStoreUserCommand
        {
            StoreUserId = storeUser.Id,
            StoreId = storeUser.StoreId,
            DeactivatedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync(storeUser);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<StoreUser>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeactivateUserAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<StoreUser>(su =>
            su.Status == StoreUserStatus.Deactivated &&
            su.DeactivatedBy == TestSellerId)), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new DeactivateStoreUserCommand
        {
            StoreUserId = Guid.NewGuid(),
            StoreId = TestStoreId,
            DeactivatedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync((StoreUser?)null);

        // Act
        var result = await _service.DeactivateUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenUserAlreadyDeactivated_ReturnsFailure()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Deactivated;

        var command = new DeactivateStoreUserCommand
        {
            StoreUserId = storeUser.Id,
            StoreId = storeUser.StoreId,
            DeactivatedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.DeactivateUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already deactivated"));
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenDeactivatingOnlyOwner_ReturnsFailure()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active;
        storeUser.Role = StoreRole.StoreOwner;

        var command = new DeactivateStoreUserCommand
        {
            StoreUserId = storeUser.Id,
            StoreId = storeUser.StoreId,
            DeactivatedBy = TestSellerId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.StoreUserId))
            .ReturnsAsync(storeUser);
        _mockRepository.Setup(r => r.GetByStoreIdAsync(storeUser.StoreId))
            .ReturnsAsync(new List<StoreUser> { storeUser });

        // Act
        var result = await _service.DeactivateUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("only store owner"));
    }

    #endregion

    #region ValidateInvitationTokenAsync Tests

    [Fact]
    public async Task ValidateInvitationTokenAsync_WithValidToken_ReturnsStoreUser()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Pending;
        storeUser.InvitationToken = "valid-token";
        storeUser.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1);

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync("valid-token"))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.ValidateInvitationTokenAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(storeUser.Id, result.Id);
    }

    [Fact]
    public async Task ValidateInvitationTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Pending;
        storeUser.InvitationToken = "expired-token";
        storeUser.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync("expired-token"))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.ValidateInvitationTokenAsync("expired-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateInvitationTokenAsync_WithUsedToken_ReturnsNull()
    {
        // Arrange
        var storeUser = CreateTestStoreUser();
        storeUser.Status = StoreUserStatus.Active; // Already used
        storeUser.InvitationToken = "used-token";

        _mockRepository.Setup(r => r.GetByInvitationTokenAsync("used-token"))
            .ReturnsAsync(storeUser);

        // Act
        var result = await _service.ValidateInvitationTokenAsync("used-token");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    private static StoreUser CreateTestStoreUser()
    {
        return new StoreUser
        {
            Id = TestStoreUserId,
            StoreId = TestStoreId,
            UserId = TestUserId,
            Email = TestEmail,
            Role = StoreRole.CatalogManager,
            Status = StoreUserStatus.Active,
            InvitedAt = DateTimeOffset.UtcNow.AddDays(-7),
            InvitedBy = TestSellerId
        };
    }

    private static Store CreateTestStore()
    {
        return new Store
        {
            Id = TestStoreId,
            SellerId = TestSellerId,
            Name = "Test Store",
            Slug = "test-store",
            Status = StoreStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            LastUpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    private static InviteStoreUserCommand CreateValidInviteCommand()
    {
        return new InviteStoreUserCommand
        {
            StoreId = TestStoreId,
            Email = TestEmail,
            Role = StoreRole.CatalogManager,
            InvitedBy = TestSellerId
        };
    }

    #endregion
}
