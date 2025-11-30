using Mercato.Identity.Application.Services;
using Mercato.Identity.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Identity;

public class AccountDeletionCheckServiceTests
{
    [Fact]
    public async Task CheckBlockingConditionsAsync_WithNoBlockingConditions_ReturnsCanProceed()
    {
        // Arrange
        var userId = "user-123";
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        
        mockDataProvider.Setup(x => x.GetOpenDisputeCountAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.GetPendingRefundCountAsync(userId))
            .ReturnsAsync(0);

        var service = new AccountDeletionCheckService(mockLogger.Object, mockDataProvider.Object);

        // Act
        var result = await service.CheckBlockingConditionsAsync(userId);

        // Assert
        Assert.True(result.CanDelete);
        Assert.Empty(result.BlockingConditions);
        Assert.False(result.HasOpenDisputes);
        Assert.False(result.HasPendingRefunds);
        mockDataProvider.VerifyAll();
    }

    [Fact]
    public async Task CheckBlockingConditionsAsync_WithOpenDisputes_ReturnsCannotProceed()
    {
        // Arrange
        var userId = "user-123";
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        
        mockDataProvider.Setup(x => x.GetOpenDisputeCountAsync(userId))
            .ReturnsAsync(2);
        mockDataProvider.Setup(x => x.GetPendingRefundCountAsync(userId))
            .ReturnsAsync(0);

        var service = new AccountDeletionCheckService(mockLogger.Object, mockDataProvider.Object);

        // Act
        var result = await service.CheckBlockingConditionsAsync(userId);

        // Assert
        Assert.False(result.CanDelete);
        Assert.Single(result.BlockingConditions);
        Assert.Contains("2 open dispute(s)", result.BlockingConditions[0]);
        Assert.True(result.HasOpenDisputes);
        Assert.Equal(2, result.OpenDisputeCount);
        Assert.False(result.HasPendingRefunds);
        mockDataProvider.VerifyAll();
    }

    [Fact]
    public async Task CheckBlockingConditionsAsync_WithPendingRefunds_ReturnsCannotProceed()
    {
        // Arrange
        var userId = "user-123";
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        
        mockDataProvider.Setup(x => x.GetOpenDisputeCountAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.GetPendingRefundCountAsync(userId))
            .ReturnsAsync(1);

        var service = new AccountDeletionCheckService(mockLogger.Object, mockDataProvider.Object);

        // Act
        var result = await service.CheckBlockingConditionsAsync(userId);

        // Assert
        Assert.False(result.CanDelete);
        Assert.Single(result.BlockingConditions);
        Assert.Contains("1 pending refund(s)", result.BlockingConditions[0]);
        Assert.False(result.HasOpenDisputes);
        Assert.True(result.HasPendingRefunds);
        Assert.Equal(1, result.PendingRefundCount);
        mockDataProvider.VerifyAll();
    }

    [Fact]
    public async Task CheckBlockingConditionsAsync_WithMultipleBlockingConditions_ReturnsAllConditions()
    {
        // Arrange
        var userId = "user-123";
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        
        mockDataProvider.Setup(x => x.GetOpenDisputeCountAsync(userId))
            .ReturnsAsync(3);
        mockDataProvider.Setup(x => x.GetPendingRefundCountAsync(userId))
            .ReturnsAsync(2);

        var service = new AccountDeletionCheckService(mockLogger.Object, mockDataProvider.Object);

        // Act
        var result = await service.CheckBlockingConditionsAsync(userId);

        // Assert
        Assert.False(result.CanDelete);
        Assert.Equal(2, result.BlockingConditions.Count);
        Assert.Contains("3 open dispute(s)", result.BlockingConditions[0]);
        Assert.Contains("2 pending refund(s)", result.BlockingConditions[1]);
        Assert.True(result.HasOpenDisputes);
        Assert.Equal(3, result.OpenDisputeCount);
        Assert.True(result.HasPendingRefunds);
        Assert.Equal(2, result.PendingRefundCount);
        mockDataProvider.VerifyAll();
    }

    [Fact]
    public async Task CheckBlockingConditionsAsync_WithNoDataProvider_ReturnsCanProceed()
    {
        // Arrange
        var userId = "user-123";
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();

        // No data provider - service should still work
        var service = new AccountDeletionCheckService(mockLogger.Object, null);

        // Act
        var result = await service.CheckBlockingConditionsAsync(userId);

        // Assert
        Assert.True(result.CanDelete);
        Assert.Empty(result.BlockingConditions);
    }

    [Fact]
    public async Task CheckBlockingConditionsAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();
        var service = new AccountDeletionCheckService(mockLogger.Object, null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CheckBlockingConditionsAsync(null!));
    }

    [Fact]
    public async Task CheckBlockingConditionsAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AccountDeletionCheckService>>();
        var service = new AccountDeletionCheckService(mockLogger.Object, null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckBlockingConditionsAsync(string.Empty));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AccountDeletionCheckService(null!, null));
    }
}
