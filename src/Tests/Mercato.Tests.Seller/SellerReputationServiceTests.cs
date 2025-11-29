using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class SellerReputationServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();

    private readonly Mock<ISellerReputationRepository> _mockRepository;
    private readonly Mock<ILogger<SellerReputationService>> _mockLogger;
    private readonly SellerReputationService _service;

    public SellerReputationServiceTests()
    {
        _mockRepository = new Mock<ISellerReputationRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<SellerReputationService>>();
        _service = new SellerReputationService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetReputationByStoreIdAsync Tests

    [Fact]
    public async Task GetReputationByStoreIdAsync_WhenReputationExists_ReturnsReputation()
    {
        // Arrange
        var expectedReputation = CreateTestReputation();
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(expectedReputation);

        // Act
        var result = await _service.GetReputationByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedReputation.Id, result.Id);
        Assert.Equal(expectedReputation.StoreId, result.StoreId);
        Assert.Equal(expectedReputation.ReputationScore, result.ReputationScore);
        _mockRepository.Verify(r => r.GetByStoreIdAsync(TestStoreId), Times.Once);
    }

    [Fact]
    public async Task GetReputationByStoreIdAsync_WhenReputationNotExists_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);

        // Act
        var result = await _service.GetReputationByStoreIdAsync(TestStoreId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByStoreIdAsync(TestStoreId), Times.Once);
    }

    #endregion

    #region CalculateAndUpdateReputationAsync Tests - All Metrics Present

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithAllMetrics_CalculatesCorrectScore()
    {
        // Arrange
        var command = CreateCommandWithAllMetrics();
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.ReputationScore);
        Assert.True(result.ReputationScore > 0);
        Assert.True(result.ReputationScore <= 100);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<SellerReputation>(rep =>
            rep.StoreId == TestStoreId &&
            rep.AverageRating == command.AverageRating &&
            rep.TotalRatingsCount == command.TotalRatingsCount &&
            rep.TotalOrdersCount == command.TotalOrdersCount &&
            rep.ReputationScore.HasValue)), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithPerfectMetrics_ReturnsPlatinumLevel()
    {
        // Arrange - Perfect metrics: 5.0 rating, 100% on-time, 0% cancellation, 0% dispute
        var command = new CalculateReputationCommand
        {
            AverageRating = 5.0m,
            TotalRatingsCount = 100,
            TotalOrdersCount = 100,
            DeliveredOrdersCount = 100,
            OnTimeDeliveriesCount = 100,
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(100m, result.ReputationScore);
        Assert.Equal(ReputationLevel.Platinum, result.ReputationLevel);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithGoodMetrics_ReturnsGoldLevel()
    {
        // Arrange - Good metrics that result in score 75-89
        // Rating: 4.0 normalized = 75, On-time: 70%, Cancellation: 10%, Dispute: 10%
        // Score = 75*0.4 + 70*0.25 + 90*0.2 + 90*0.15 = 30 + 17.5 + 18 + 13.5 = 79
        var command = new CalculateReputationCommand
        {
            AverageRating = 4.0m,
            TotalRatingsCount = 50,
            TotalOrdersCount = 50,
            DeliveredOrdersCount = 40,
            OnTimeDeliveriesCount = 28,
            CancelledOrdersCount = 5,
            DisputedOrdersCount = 5
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        Assert.True(result.ReputationScore >= 75 && result.ReputationScore < 90);
        Assert.Equal(ReputationLevel.Gold, result.ReputationLevel);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithAverageMetrics_ReturnsSilverLevel()
    {
        // Arrange - Average metrics that result in score 60-74
        var command = new CalculateReputationCommand
        {
            AverageRating = 3.5m,
            TotalRatingsCount = 30,
            TotalOrdersCount = 30,
            DeliveredOrdersCount = 25,
            OnTimeDeliveriesCount = 18,
            CancelledOrdersCount = 5,
            DisputedOrdersCount = 5
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        Assert.True(result.ReputationScore >= 60 && result.ReputationScore < 75);
        Assert.Equal(ReputationLevel.Silver, result.ReputationLevel);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithPoorMetrics_ReturnsBronzeLevel()
    {
        // Arrange - Poor metrics that result in score < 60
        var command = new CalculateReputationCommand
        {
            AverageRating = 2.0m,
            TotalRatingsCount = 20,
            TotalOrdersCount = 20,
            DeliveredOrdersCount = 15,
            OnTimeDeliveriesCount = 5,
            CancelledOrdersCount = 5,
            DisputedOrdersCount = 8
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        Assert.True(result.ReputationScore < 60);
        Assert.Equal(ReputationLevel.Bronze, result.ReputationLevel);
    }

    #endregion

    #region CalculateAndUpdateReputationAsync Tests - Missing Optional Metrics

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithNoRating_CalculatesScoreFromOtherMetrics()
    {
        // Arrange
        var command = new CalculateReputationCommand
        {
            AverageRating = null,
            TotalRatingsCount = 0,
            TotalOrdersCount = 50,
            DeliveredOrdersCount = 45,
            OnTimeDeliveriesCount = 40,
            CancelledOrdersCount = 2,
            DisputedOrdersCount = 3
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        Assert.True(result.ReputationScore > 0);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<SellerReputation>(rep =>
            rep.AverageRating == null &&
            rep.ReputationScore.HasValue)), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithNoDeliveredOrders_CalculatesScoreWithoutShippingRate()
    {
        // Arrange
        var command = new CalculateReputationCommand
        {
            AverageRating = 4.5m,
            TotalRatingsCount = 10,
            TotalOrdersCount = 10,
            DeliveredOrdersCount = 0,
            OnTimeDeliveriesCount = 0,
            CancelledOrdersCount = 1,
            DisputedOrdersCount = 1
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<SellerReputation>(rep =>
            rep.OnTimeShippingRate == null)), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithNoOrders_ReturnsNullScore()
    {
        // Arrange
        var command = new CalculateReputationCommand
        {
            AverageRating = null,
            TotalRatingsCount = 0,
            TotalOrdersCount = 0,
            DeliveredOrdersCount = 0,
            OnTimeDeliveriesCount = 0,
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.ReputationScore);
        Assert.Equal(ReputationLevel.Unrated, result.ReputationLevel);
    }

    #endregion

    #region CalculateAndUpdateReputationAsync Tests - New Seller Level

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithFewerThan10Orders_ReturnsNewLevel()
    {
        // Arrange - Good metrics but fewer than 10 orders
        var command = new CalculateReputationCommand
        {
            AverageRating = 5.0m,
            TotalRatingsCount = 5,
            TotalOrdersCount = 5,
            DeliveredOrdersCount = 5,
            OnTimeDeliveriesCount = 5,
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        Assert.Equal(100m, result.ReputationScore);
        Assert.Equal(ReputationLevel.New, result.ReputationLevel);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithExactly10Orders_DoesNotReturnNewLevel()
    {
        // Arrange - Exactly 10 orders with perfect metrics
        var command = new CalculateReputationCommand
        {
            AverageRating = 5.0m,
            TotalRatingsCount = 10,
            TotalOrdersCount = 10,
            DeliveredOrdersCount = 10,
            OnTimeDeliveriesCount = 10,
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(ReputationLevel.New, result.ReputationLevel);
        Assert.Equal(ReputationLevel.Platinum, result.ReputationLevel);
    }

    #endregion

    #region CalculateAndUpdateReputationAsync Tests - Update Existing Reputation

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WhenReputationExists_UpdatesReputation()
    {
        // Arrange
        var existingReputation = CreateTestReputation();
        var command = CreateCommandWithAllMetrics();
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(existingReputation);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<SellerReputation>(rep =>
            rep.Id == existingReputation.Id &&
            rep.StoreId == TestStoreId)), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<SellerReputation>()), Times.Never);
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WhenReputationNotExists_CreatesReputation()
    {
        // Arrange
        var command = CreateCommandWithAllMetrics();
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<SellerReputation>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SellerReputation>()), Times.Never);
    }

    #endregion

    #region CalculateAndUpdateReputationAsync Tests - Validation

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithInvalidRating_ReturnsFailure()
    {
        // Arrange
        var command = new CalculateReputationCommand
        {
            AverageRating = 6.0m, // Invalid: must be 1-5
            TotalRatingsCount = 10,
            TotalOrdersCount = 10,
            DeliveredOrdersCount = 10,
            OnTimeDeliveriesCount = 10,
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("rating"));
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithNegativeCounts_ReturnsFailure()
    {
        // Arrange
        var command = new CalculateReputationCommand
        {
            AverageRating = 4.0m,
            TotalRatingsCount = -1, // Invalid: negative
            TotalOrdersCount = 10,
            DeliveredOrdersCount = 10,
            OnTimeDeliveriesCount = 10,
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("negative"));
    }

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_WithOnTimeExceedingDelivered_ReturnsFailure()
    {
        // Arrange
        var command = new CalculateReputationCommand
        {
            AverageRating = 4.0m,
            TotalRatingsCount = 10,
            TotalOrdersCount = 10,
            DeliveredOrdersCount = 5,
            OnTimeDeliveriesCount = 10, // Invalid: exceeds delivered
            CancelledOrdersCount = 0,
            DisputedOrdersCount = 0
        };

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("exceed"));
    }

    #endregion

    #region GetReputationsByStoreIdsAsync Tests

    [Fact]
    public async Task GetReputationsByStoreIdsAsync_WithValidIds_ReturnsReputations()
    {
        // Arrange
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();
        var storeIds = new[] { storeId1, storeId2 };
        var expectedReputations = new List<SellerReputation>
        {
            new() { Id = Guid.NewGuid(), StoreId = storeId1, ReputationLevel = ReputationLevel.Gold },
            new() { Id = Guid.NewGuid(), StoreId = storeId2, ReputationLevel = ReputationLevel.Silver }
        };
        _mockRepository.Setup(r => r.GetByStoreIdsAsync(storeIds))
            .ReturnsAsync(expectedReputations);

        // Act
        var result = await _service.GetReputationsByStoreIdsAsync(storeIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetByStoreIdsAsync(storeIds), Times.Once);
    }

    [Fact]
    public async Task GetReputationsByStoreIdsAsync_WithEmptyIds_ReturnsEmptyList()
    {
        // Arrange
        var storeIds = Array.Empty<Guid>();
        _mockRepository.Setup(r => r.GetByStoreIdsAsync(storeIds))
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetReputationsByStoreIdsAsync(storeIds);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetByStoreIdsAsync(storeIds), Times.Once);
    }

    #endregion

    #region Weighted Formula Tests

    [Fact]
    public async Task CalculateAndUpdateReputationAsync_VerifiesWeightedFormula()
    {
        // Arrange - Test specific values to verify formula
        // Rating: 4.0 (normalized: 75), On-Time: 80%, Dispute: 10%, Cancellation: 5%
        // Expected: 75*0.4 + 80*0.25 + 90*0.2 + 95*0.15 = 30 + 20 + 18 + 14.25 = 82.25
        var command = new CalculateReputationCommand
        {
            AverageRating = 4.0m,
            TotalRatingsCount = 20,
            TotalOrdersCount = 20,
            DeliveredOrdersCount = 20,
            OnTimeDeliveriesCount = 16, // 80%
            CancelledOrdersCount = 1,   // 5%
            DisputedOrdersCount = 2     // 10%
        };
        _mockRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync((SellerReputation?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerReputation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CalculateAndUpdateReputationAsync(TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReputationScore);
        Assert.Equal(82.25m, result.ReputationScore);
        Assert.Equal(ReputationLevel.Gold, result.ReputationLevel);
    }

    #endregion

    #region Helper Methods

    private static SellerReputation CreateTestReputation()
    {
        return new SellerReputation
        {
            Id = Guid.NewGuid(),
            StoreId = TestStoreId,
            AverageRating = 4.5m,
            TotalRatingsCount = 100,
            DisputeRate = 5.0m,
            OnTimeShippingRate = 95.0m,
            CancellationRate = 2.0m,
            TotalOrdersCount = 200,
            ReputationScore = 85.0m,
            ReputationLevel = ReputationLevel.Gold,
            LastCalculatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            LastUpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    private static CalculateReputationCommand CreateCommandWithAllMetrics()
    {
        return new CalculateReputationCommand
        {
            AverageRating = 4.5m,
            TotalRatingsCount = 50,
            TotalOrdersCount = 100,
            DeliveredOrdersCount = 90,
            OnTimeDeliveriesCount = 85,
            CancelledOrdersCount = 5,
            DisputedOrdersCount = 3
        };
    }

    #endregion
}
