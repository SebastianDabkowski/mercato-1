using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Payments;

public class PayoutServiceTests
{
    private readonly Mock<IPayoutRepository> _mockPayoutRepository;
    private readonly Mock<IEscrowRepository> _mockEscrowRepository;
    private readonly Mock<ILogger<PayoutService>> _mockLogger;

    public PayoutServiceTests()
    {
        _mockPayoutRepository = new Mock<IPayoutRepository>(MockBehavior.Strict);
        _mockEscrowRepository = new Mock<IEscrowRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<PayoutService>>();
    }

    private PayoutService CreateService(PayoutSettings? settings = null)
    {
        var payoutSettings = settings ?? new PayoutSettings();
        var options = Options.Create(payoutSettings);
        return new PayoutService(
            _mockPayoutRepository.Object,
            _mockEscrowRepository.Object,
            _mockLogger.Object,
            options);
    }

    #region SchedulePayoutsAsync Tests

    [Fact]
    public async Task SchedulePayoutsAsync_ValidCommand_SchedulesPayoutsForEligibleSellers()
    {
        // Arrange
        var service = CreateService();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = seller1Id,
                Amount = 100.00m,
                Currency = "USD",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = seller1Id,
                Amount = 50.00m,
                Currency = "USD",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = seller2Id,
                Amount = 75.00m,
                Currency = "USD",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            }
        };

        _mockEscrowRepository
            .Setup(r => r.GetByStatusForPayoutAsync(EscrowStatus.Released, true))
            .ReturnsAsync(escrowEntries);

        _mockPayoutRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        _mockEscrowRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new SchedulePayoutsCommand
        {
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(1),
            ScheduleFrequency = PayoutScheduleFrequency.Weekly,
            AuditNote = "Weekly payout scheduling"
        };

        // Act
        var result = await service.SchedulePayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count); // Two sellers
        Assert.Contains(result.Payouts, p => p.SellerId == seller1Id && p.Amount == 150.00m);
        Assert.Contains(result.Payouts, p => p.SellerId == seller2Id && p.Amount == 75.00m);
        Assert.All(result.Payouts, p =>
        {
            Assert.Equal(PayoutStatus.Scheduled, p.Status);
            Assert.Equal("USD", p.Currency);
            Assert.Equal(PayoutScheduleFrequency.Weekly, p.ScheduleFrequency);
        });
    }

    [Fact]
    public async Task SchedulePayoutsAsync_BelowThreshold_RollsOver()
    {
        // Arrange
        var settings = new PayoutSettings { MinimumPayoutThreshold = 50.00m };
        var service = CreateService(settings);
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = seller1Id,
                Amount = 100.00m,
                Currency = "USD",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = seller2Id,
                Amount = 25.00m, // Below threshold
                Currency = "USD",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            }
        };

        _mockEscrowRepository
            .Setup(r => r.GetByStatusForPayoutAsync(EscrowStatus.Released, true))
            .ReturnsAsync(escrowEntries);

        _mockPayoutRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        _mockEscrowRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new SchedulePayoutsCommand
        {
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(1),
            ScheduleFrequency = PayoutScheduleFrequency.Weekly
        };

        // Act
        var result = await service.SchedulePayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Payouts);
        Assert.Equal(seller1Id, result.Payouts[0].SellerId);
        Assert.Equal(1, result.RolledOverCount);
    }

    [Fact]
    public async Task SchedulePayoutsAsync_NoEligibleEntries_ReturnsEmptyResult()
    {
        // Arrange
        var service = CreateService();

        _mockEscrowRepository
            .Setup(r => r.GetByStatusForPayoutAsync(EscrowStatus.Released, true))
            .ReturnsAsync(new List<EscrowEntry>());

        var command = new SchedulePayoutsCommand
        {
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(1),
            ScheduleFrequency = PayoutScheduleFrequency.Weekly
        };

        // Act
        var result = await service.SchedulePayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payouts);
        Assert.Equal(0, result.RolledOverCount);
    }

    [Fact]
    public async Task SchedulePayoutsAsync_MissingScheduledDate_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var command = new SchedulePayoutsCommand
        {
            ScheduledAt = default,
            ScheduleFrequency = PayoutScheduleFrequency.Weekly
        };

        // Act
        var result = await service.SchedulePayoutsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Scheduled date is required"));
    }

    [Fact]
    public async Task SchedulePayoutsAsync_AggregatesByCurrency()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = 100.00m,
                Currency = "USD",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = 50.00m,
                Currency = "EUR",
                Status = EscrowStatus.Released,
                IsEligibleForPayout = false
            }
        };

        _mockEscrowRepository
            .Setup(r => r.GetByStatusForPayoutAsync(EscrowStatus.Released, true))
            .ReturnsAsync(escrowEntries);

        _mockPayoutRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        _mockEscrowRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new SchedulePayoutsCommand
        {
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(1),
            ScheduleFrequency = PayoutScheduleFrequency.Weekly
        };

        // Act
        var result = await service.SchedulePayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count); // Same seller, different currencies
        Assert.Contains(result.Payouts, p => p.Currency == "USD" && p.Amount == 100.00m);
        Assert.Contains(result.Payouts, p => p.Currency == "EUR" && p.Amount == 50.00m);
    }

    #endregion

    #region ProcessPayoutsAsync Tests

    [Fact]
    public async Task ProcessPayoutsAsync_ScheduledPayouts_ProcessesAndMarksPaid()
    {
        // Arrange
        var service = CreateService();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var scheduledPayouts = new List<Payout>
        {
            new Payout
            {
                Id = Guid.NewGuid(),
                SellerId = seller1Id,
                Amount = 100.00m,
                Currency = "USD",
                Status = PayoutStatus.Scheduled,
                ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new Payout
            {
                Id = Guid.NewGuid(),
                SellerId = seller2Id,
                Amount = 75.00m,
                Currency = "USD",
                Status = PayoutStatus.Scheduled,
                ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(scheduledPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessPayoutsCommand
        {
            ProcessBefore = DateTimeOffset.UtcNow,
            CreateBatch = true
        };

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.NotNull(result.BatchId);
        Assert.All(result.Payouts, p => Assert.Equal(PayoutStatus.Paid, p.Status));
    }

    [Fact]
    public async Task ProcessPayoutsAsync_NoScheduledPayouts_ReturnsEmptyResult()
    {
        // Arrange
        var service = CreateService();

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Payout>());

        var command = new ProcessPayoutsCommand
        {
            ProcessBefore = DateTimeOffset.UtcNow
        };

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payouts);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Null(result.BatchId);
    }

    [Fact]
    public async Task ProcessPayoutsAsync_WithBatching_LimitsPayoutsPerBatch()
    {
        // Arrange
        var settings = new PayoutSettings { EnableBatching = true, MaxPayoutsPerBatch = 2 };
        var service = CreateService(settings);

        var scheduledPayouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled },
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled },
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled }
        };

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(scheduledPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessPayoutsCommand
        {
            ProcessBefore = DateTimeOffset.UtcNow,
            CreateBatch = true
        };

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count); // Limited to 2
    }

    [Fact]
    public async Task ProcessPayoutsAsync_WithoutBatching_ProcessesAllPayouts()
    {
        // Arrange
        var settings = new PayoutSettings { EnableBatching = false };
        var service = CreateService(settings);

        var scheduledPayouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled },
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled },
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled }
        };

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(scheduledPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessPayoutsCommand
        {
            ProcessBefore = DateTimeOffset.UtcNow,
            CreateBatch = false
        };

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payouts.Count);
        Assert.Null(result.BatchId);
    }

    #endregion

    #region RetryPayoutsAsync Tests

    [Fact]
    public async Task RetryPayoutsAsync_FailedPayout_RetriesAndMarksAsPaid()
    {
        // Arrange
        var service = CreateService();
        var payoutId = Guid.NewGuid();

        var failedPayout = new Payout
        {
            Id = payoutId,
            SellerId = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD",
            Status = PayoutStatus.Failed,
            RetryCount = 0,
            ErrorReference = "ERR-123",
            ErrorMessage = "Connection timeout"
        };

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync(failedPayout);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new RetryPayoutsCommand
        {
            PayoutId = payoutId,
            AuditNote = "Manual retry"
        };

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Payouts);
        Assert.Equal(PayoutStatus.Paid, result.Payouts[0].Status);
        Assert.Equal(1, result.Payouts[0].RetryCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task RetryPayoutsAsync_ExceededMaxRetries_ReturnsFailure()
    {
        // Arrange
        var settings = new PayoutSettings { MaxRetryAttempts = 3 };
        var service = CreateService(settings);
        var payoutId = Guid.NewGuid();

        var failedPayout = new Payout
        {
            Id = payoutId,
            SellerId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = PayoutStatus.Failed,
            RetryCount = 3 // Already at max
        };

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync(failedPayout);

        var command = new RetryPayoutsCommand { PayoutId = payoutId };

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("exceeded maximum retry attempts"));
    }

    [Fact]
    public async Task RetryPayoutsAsync_NonFailedPayout_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var payoutId = Guid.NewGuid();

        var paidPayout = new Payout
        {
            Id = payoutId,
            SellerId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = PayoutStatus.Paid
        };

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync(paidPayout);

        var command = new RetryPayoutsCommand { PayoutId = payoutId };

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Only failed payouts can be retried"));
    }

    [Fact]
    public async Task RetryPayoutsAsync_PayoutNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var payoutId = Guid.NewGuid();

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync((Payout?)null);

        var command = new RetryPayoutsCommand { PayoutId = payoutId };

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payout not found"));
    }

    [Fact]
    public async Task RetryPayoutsAsync_AllEligiblePayouts_RetriesAllFailedPayouts()
    {
        // Arrange
        var settings = new PayoutSettings { MaxRetryAttempts = 3 };
        var service = CreateService(settings);

        var failedPayouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Failed, RetryCount = 1 },
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 50.00m, Status = PayoutStatus.Failed, RetryCount = 2 }
        };

        _mockPayoutRepository
            .Setup(r => r.GetPayoutsForRetryAsync(3))
            .ReturnsAsync(failedPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new RetryPayoutsCommand(); // No PayoutId - retry all

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count);
        Assert.All(result.Payouts, p => Assert.Equal(PayoutStatus.Paid, p.Status));
    }

    [Fact]
    public async Task RetryPayoutsAsync_NoEligiblePayouts_ReturnsEmptyResult()
    {
        // Arrange
        var settings = new PayoutSettings { MaxRetryAttempts = 3 };
        var service = CreateService(settings);

        _mockPayoutRepository
            .Setup(r => r.GetPayoutsForRetryAsync(3))
            .ReturnsAsync(new List<Payout>());

        var command = new RetryPayoutsCommand();

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payouts);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    #endregion

    #region GetPayoutAsync Tests

    [Fact]
    public async Task GetPayoutAsync_ValidId_ReturnsPayout()
    {
        // Arrange
        var service = CreateService();
        var payoutId = Guid.NewGuid();

        var payout = new Payout
        {
            Id = payoutId,
            SellerId = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD",
            Status = PayoutStatus.Paid
        };

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync(payout);

        // Act
        var result = await service.GetPayoutAsync(payoutId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payout);
        Assert.Equal(payoutId, result.Payout.Id);
    }

    [Fact]
    public async Task GetPayoutAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetPayoutAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payout ID is required"));
    }

    [Fact]
    public async Task GetPayoutAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var payoutId = Guid.NewGuid();

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync((Payout?)null);

        // Act
        var result = await service.GetPayoutAsync(payoutId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payout not found"));
    }

    #endregion

    #region GetPayoutsBySellerIdAsync Tests

    [Fact]
    public async Task GetPayoutsBySellerIdAsync_ValidSellerId_ReturnsPayouts()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var payouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 100.00m, Status = PayoutStatus.Paid },
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 75.00m, Status = PayoutStatus.Scheduled }
        };

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(payouts);

        // Act
        var result = await service.GetPayoutsBySellerIdAsync(sellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count);
    }

    [Fact]
    public async Task GetPayoutsBySellerIdAsync_WithStatusFilter_ReturnsFilteredPayouts()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var payouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 100.00m, Status = PayoutStatus.Paid }
        };

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdAndStatusAsync(sellerId, PayoutStatus.Paid))
            .ReturnsAsync(payouts);

        // Act
        var result = await service.GetPayoutsBySellerIdAsync(sellerId, PayoutStatus.Paid);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Payouts);
        Assert.All(result.Payouts, p => Assert.Equal(PayoutStatus.Paid, p.Status));
    }

    [Fact]
    public async Task GetPayoutsBySellerIdAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetPayoutsBySellerIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    #endregion

    #region Payout Status Tests

    [Fact]
    public async Task ProcessPayoutsAsync_SetsProcessingTimestamp()
    {
        // Arrange
        var service = CreateService();

        var scheduledPayouts = new List<Payout>
        {
            new Payout
            {
                Id = Guid.NewGuid(),
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = PayoutStatus.Scheduled,
                ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(scheduledPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessPayoutsCommand
        {
            ProcessBefore = DateTimeOffset.UtcNow,
            CreateBatch = true
        };

        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payouts[0].ProcessingStartedAt);
        Assert.True(result.Payouts[0].ProcessingStartedAt >= beforeTime);
        Assert.True(result.Payouts[0].ProcessingStartedAt <= afterTime);
    }

    [Fact]
    public async Task ProcessPayoutsAsync_SetsCompletedAtForSuccessfulPayouts()
    {
        // Arrange
        var service = CreateService();

        var scheduledPayouts = new List<Payout>
        {
            new Payout
            {
                Id = Guid.NewGuid(),
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = PayoutStatus.Scheduled
            }
        };

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(scheduledPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessPayoutsCommand();

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payouts[0].CompletedAt);
    }

    #endregion

    #region Error Reference Tests

    [Fact]
    public async Task RetryPayoutsAsync_ClearsErrorReferenceOnRetry()
    {
        // Arrange
        var service = CreateService();
        var payoutId = Guid.NewGuid();

        var failedPayout = new Payout
        {
            Id = payoutId,
            SellerId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = PayoutStatus.Failed,
            RetryCount = 0,
            ErrorReference = "ERR-123",
            ErrorMessage = "Original error"
        };

        _mockPayoutRepository
            .Setup(r => r.GetByIdAsync(payoutId))
            .ReturnsAsync(failedPayout);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new RetryPayoutsCommand { PayoutId = payoutId };

        // Act
        var result = await service.RetryPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.Payouts[0].ErrorReference);
        Assert.Null(result.Payouts[0].ErrorMessage);
    }

    #endregion

    #region Batch ID Tests

    [Fact]
    public async Task ProcessPayoutsAsync_AssignsBatchIdToAllPayouts()
    {
        // Arrange
        var service = CreateService();

        var scheduledPayouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 100.00m, Status = PayoutStatus.Scheduled },
            new Payout { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Amount = 50.00m, Status = PayoutStatus.Scheduled }
        };

        _mockPayoutRepository
            .Setup(r => r.GetScheduledPayoutsAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(scheduledPayouts);

        _mockPayoutRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Payout>>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessPayoutsCommand { CreateBatch = true };

        // Act
        var result = await service.ProcessPayoutsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.BatchId);
        Assert.All(result.Payouts, p => Assert.Equal(result.BatchId, p.BatchId));
    }

    #endregion

    #region GetPayoutsFilteredAsync Tests

    [Fact]
    public async Task GetPayoutsFilteredAsync_ValidQuery_ReturnsPayouts()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var payouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 100.00m, Status = PayoutStatus.Paid, ScheduledAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 75.00m, Status = PayoutStatus.Scheduled, ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1) }
        };

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdWithFiltersAsync(sellerId, null, null, null))
            .ReturnsAsync(payouts);

        var query = new GetPayoutsFilteredQuery { SellerId = sellerId };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payouts.Count);
    }

    [Fact]
    public async Task GetPayoutsFilteredAsync_WithStatusFilter_ReturnsFilteredPayouts()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var payouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 100.00m, Status = PayoutStatus.Paid, ScheduledAt = DateTimeOffset.UtcNow.AddDays(-5) }
        };

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdWithFiltersAsync(sellerId, PayoutStatus.Paid, null, null))
            .ReturnsAsync(payouts);

        var query = new GetPayoutsFilteredQuery
        {
            SellerId = sellerId,
            Status = PayoutStatus.Paid
        };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Payouts);
        Assert.All(result.Payouts, p => Assert.Equal(PayoutStatus.Paid, p.Status));
    }

    [Fact]
    public async Task GetPayoutsFilteredAsync_WithDateRangeFilter_ReturnsFilteredPayouts()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow;

        var payouts = new List<Payout>
        {
            new Payout { Id = Guid.NewGuid(), SellerId = sellerId, Amount = 100.00m, Status = PayoutStatus.Paid, ScheduledAt = DateTimeOffset.UtcNow.AddDays(-15) }
        };

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdWithFiltersAsync(sellerId, null, fromDate, toDate))
            .ReturnsAsync(payouts);

        var query = new GetPayoutsFilteredQuery
        {
            SellerId = sellerId,
            FromDate = fromDate,
            ToDate = toDate
        };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Payouts);
    }

    [Fact]
    public async Task GetPayoutsFilteredAsync_WithAllFilters_ReturnsFilteredPayouts()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow;
        var status = PayoutStatus.Failed;

        var payouts = new List<Payout>
        {
            new Payout 
            { 
                Id = Guid.NewGuid(), 
                SellerId = sellerId, 
                Amount = 100.00m, 
                Status = PayoutStatus.Failed, 
                ScheduledAt = DateTimeOffset.UtcNow.AddDays(-10),
                ErrorMessage = "Connection timeout"
            }
        };

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdWithFiltersAsync(sellerId, status, fromDate, toDate))
            .ReturnsAsync(payouts);

        var query = new GetPayoutsFilteredQuery
        {
            SellerId = sellerId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate
        };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Payouts);
        Assert.Equal(PayoutStatus.Failed, result.Payouts[0].Status);
    }

    [Fact]
    public async Task GetPayoutsFilteredAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        var query = new GetPayoutsFilteredQuery { SellerId = Guid.Empty };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task GetPayoutsFilteredAsync_FromDateAfterToDate_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var query = new GetPayoutsFilteredQuery
        {
            SellerId = sellerId,
            FromDate = DateTimeOffset.UtcNow,
            ToDate = DateTimeOffset.UtcNow.AddDays(-30) // Before FromDate
        };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("From date must be before or equal to To date"));
    }

    [Fact]
    public async Task GetPayoutsFilteredAsync_NoPayoutsFound_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        _mockPayoutRepository
            .Setup(r => r.GetBySellerIdWithFiltersAsync(sellerId, null, null, null))
            .ReturnsAsync(new List<Payout>());

        var query = new GetPayoutsFilteredQuery { SellerId = sellerId };

        // Act
        var result = await service.GetPayoutsFilteredAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payouts);
    }

    #endregion
}
