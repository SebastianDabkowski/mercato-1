using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Payments;

public class RefundServiceTests
{
    private readonly Mock<IRefundRepository> _mockRefundRepository;
    private readonly Mock<IEscrowService> _mockEscrowService;
    private readonly Mock<ICommissionService> _mockCommissionService;
    private readonly Mock<ILogger<RefundService>> _mockLogger;

    public RefundServiceTests()
    {
        _mockRefundRepository = new Mock<IRefundRepository>(MockBehavior.Strict);
        _mockEscrowService = new Mock<IEscrowService>(MockBehavior.Strict);
        _mockCommissionService = new Mock<ICommissionService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<RefundService>>();
    }

    private RefundService CreateService(RefundSettings? settings = null)
    {
        var refundSettings = settings ?? new RefundSettings();
        var options = Options.Create(refundSettings);
        return new RefundService(
            _mockRefundRepository.Object,
            _mockEscrowService.Object,
            _mockCommissionService.Object,
            _mockLogger.Object,
            options);
    }

    #region ProcessFullRefundAsync Tests

    [Fact]
    public async Task ProcessFullRefundAsync_ValidCommand_CreatesRefundAndUpdatesEscrowAndCommission()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var command = new ProcessFullRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = paymentTransactionId,
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.AddAsync(It.IsAny<Refund>()))
            .ReturnsAsync((Refund r) => r);

        _mockEscrowService
            .Setup(e => e.RefundEscrowAsync(It.IsAny<RefundEscrowCommand>()))
            .ReturnsAsync(RefundEscrowResult.Success(escrowEntries));

        _mockCommissionService
            .Setup(c => c.RecalculatePartialRefundAsync(It.IsAny<RecalculatePartialRefundCommand>()))
            .ReturnsAsync(RecalculatePartialRefundResult.Success(new CommissionRecord
            {
                RefundedCommissionAmount = 10.00m
            }));

        _mockRefundRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Refund>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Refund);
        Assert.Equal(RefundType.Full, result.Refund.Type);
        Assert.Equal(RefundStatus.Completed, result.Refund.Status);
        Assert.Equal(100.00m, result.Refund.Amount);
        Assert.Equal(orderId, result.Refund.OrderId);

        _mockRefundRepository.Verify(r => r.AddAsync(It.IsAny<Refund>()), Times.Once);
        _mockRefundRepository.Verify(r => r.UpdateAsync(It.IsAny<Refund>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFullRefundAsync_NoEscrowEntries_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessFullRefundCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(command.OrderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(new List<EscrowEntry>()));

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entries found"));
    }

    [Fact]
    public async Task ProcessFullRefundAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessFullRefundCommand
        {
            OrderId = Guid.Empty,
            PaymentTransactionId = Guid.NewGuid(),
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task ProcessFullRefundAsync_MissingPaymentTransactionId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessFullRefundCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentTransactionId = Guid.Empty,
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payment transaction ID is required"));
    }

    [Fact]
    public async Task ProcessFullRefundAsync_MissingReason_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessFullRefundCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            Reason = "",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund reason is required"));
    }

    [Fact]
    public async Task ProcessFullRefundAsync_MissingInitiator_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessFullRefundCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            Reason = "Customer request",
            InitiatedByUserId = "",
            InitiatedByRole = "Admin"
        };

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Initiating user ID is required"));
    }

    [Fact]
    public async Task ProcessFullRefundAsync_NoAvailableFunds_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                RefundedAmount = 100.00m, // Fully refunded
                Currency = "USD",
                Status = EscrowStatus.Refunded
            }
        };

        var command = new ProcessFullRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = Guid.NewGuid(),
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No funds available for refund"));
    }

    #endregion

    #region ProcessPartialRefundAsync Tests

    [Fact]
    public async Task ProcessPartialRefundAsync_ValidCommand_CreatesRefundAndUpdatesEscrowAndCommission()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var command = new ProcessPartialRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = paymentTransactionId,
            SellerId = sellerId,
            Amount = 25.00m,
            Reason = "Partial return",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.AddAsync(It.IsAny<Refund>()))
            .ReturnsAsync((Refund r) => r);

        _mockEscrowService
            .Setup(e => e.PartialRefundEscrowAsync(It.IsAny<PartialRefundEscrowCommand>()))
            .ReturnsAsync(PartialRefundEscrowResult.Success(escrowEntries[0], 25.00m, 75.00m));

        _mockCommissionService
            .Setup(c => c.RecalculatePartialRefundAsync(It.IsAny<RecalculatePartialRefundCommand>()))
            .ReturnsAsync(RecalculatePartialRefundResult.Success(new CommissionRecord
            {
                RefundedCommissionAmount = 2.50m
            }));

        _mockRefundRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Refund>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Refund);
        Assert.Equal(RefundType.Partial, result.Refund.Type);
        Assert.Equal(RefundStatus.Completed, result.Refund.Status);
        Assert.Equal(25.00m, result.Refund.Amount);
        Assert.Equal(sellerId, result.Refund.SellerId);
    }

    [Fact]
    public async Task ProcessPartialRefundAsync_AmountExceedsBalance_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 80.00m, // Only 20 remaining
                Currency = "USD",
                Status = EscrowStatus.PartiallyRefunded
            }
        };

        var command = new ProcessPartialRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = Guid.NewGuid(),
            SellerId = sellerId,
            Amount = 50.00m, // Exceeds 20.00 remaining
            Reason = "Partial return",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("exceeds available balance"));
    }

    [Fact]
    public async Task ProcessPartialRefundAsync_ZeroAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessPartialRefundCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = 0m,
            Reason = "Partial return",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund amount must be greater than zero"));
    }

    [Fact]
    public async Task ProcessPartialRefundAsync_NegativeAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ProcessPartialRefundCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = -25.00m,
            Reason = "Partial return",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund amount must be greater than zero"));
    }

    [Fact]
    public async Task ProcessPartialRefundAsync_SellerNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var existingSellerId = Guid.NewGuid();
        var requestedSellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = existingSellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held
            }
        };

        var command = new ProcessPartialRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = Guid.NewGuid(),
            SellerId = requestedSellerId,
            Amount = 25.00m,
            Reason = "Partial return",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entry found for the specified seller"));
    }

    [Fact]
    public async Task ProcessPartialRefundAsync_ProviderError_ReturnsProviderError()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held
            }
        };

        var command = new ProcessPartialRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = Guid.NewGuid(),
            SellerId = sellerId,
            Amount = 25.00m,
            Reason = "Partial return",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.AddAsync(It.IsAny<Refund>()))
            .ReturnsAsync((Refund r) => r);

        _mockEscrowService
            .Setup(e => e.PartialRefundEscrowAsync(It.IsAny<PartialRefundEscrowCommand>()))
            .ReturnsAsync(PartialRefundEscrowResult.Failure("Provider connection error"));

        _mockRefundRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Refund>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.HasProviderErrors);
        Assert.NotNull(result.ProviderErrorMessage);
        Assert.Contains("Provider connection error", result.ProviderErrorMessage);
    }

    #endregion

    #region GetRefundAsync Tests

    [Fact]
    public async Task GetRefundAsync_ValidRefundId_ReturnsRefund()
    {
        // Arrange
        var service = CreateService();
        var refundId = Guid.NewGuid();
        var refund = new Refund
        {
            Id = refundId,
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = RefundStatus.Completed
        };

        _mockRefundRepository
            .Setup(r => r.GetByIdAsync(refundId))
            .ReturnsAsync(refund);

        // Act
        var result = await service.GetRefundAsync(refundId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Refund);
        Assert.Equal(refundId, result.Refund.Id);
    }

    [Fact]
    public async Task GetRefundAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var refundId = Guid.NewGuid();

        _mockRefundRepository
            .Setup(r => r.GetByIdAsync(refundId))
            .ReturnsAsync((Refund?)null);

        // Act
        var result = await service.GetRefundAsync(refundId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund not found"));
    }

    [Fact]
    public async Task GetRefundAsync_EmptyRefundId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetRefundAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund ID is required"));
    }

    #endregion

    #region GetRefundsByOrderIdAsync Tests

    [Fact]
    public async Task GetRefundsByOrderIdAsync_ValidOrderId_ReturnsRefunds()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var refunds = new List<Refund>
        {
            new Refund { Id = Guid.NewGuid(), OrderId = orderId, Amount = 50.00m, Status = RefundStatus.Completed },
            new Refund { Id = Guid.NewGuid(), OrderId = orderId, Amount = 25.00m, Status = RefundStatus.Completed }
        };

        _mockRefundRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(refunds);

        // Act
        var result = await service.GetRefundsByOrderIdAsync(orderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Refunds.Count);
        Assert.Equal(75.00m, result.TotalRefunded);
    }

    [Fact]
    public async Task GetRefundsByOrderIdAsync_EmptyOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetRefundsByOrderIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    #endregion

    #region CheckSellerRefundEligibilityAsync Tests

    [Fact]
    public async Task CheckSellerRefundEligibilityAsync_EligibleSeller_ReturnsEligible()
    {
        // Arrange
        var settings = new RefundSettings
        {
            SellerRefundWindowDays = 14,
            AllowSellerPartialRefunds = true,
            MaxSellerRefundPercentage = 100m
        };
        var service = CreateService(settings);
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) // Within window
            }
        };

        var command = new CheckRefundEligibilityCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.GetTotalRefundedByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync(0m);

        // Act
        var result = await service.CheckSellerRefundEligibilityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsEligible);
        Assert.Equal(100.00m, result.MaxRefundableAmount);
    }

    [Fact]
    public async Task CheckSellerRefundEligibilityAsync_ExpiredWindow_ReturnsNotEligible()
    {
        // Arrange
        var settings = new RefundSettings
        {
            SellerRefundWindowDays = 14,
            AllowSellerPartialRefunds = true
        };
        var service = CreateService(settings);
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-20) // Past window
            }
        };

        var command = new CheckRefundEligibilityCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        // Act
        var result = await service.CheckSellerRefundEligibilityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsEligible);
        Assert.Contains("Refund window has expired", result.IneligibilityReason);
    }

    [Fact]
    public async Task CheckSellerRefundEligibilityAsync_SellerRefundsDisabled_ReturnsNotEligible()
    {
        // Arrange
        var settings = new RefundSettings
        {
            SellerRefundWindowDays = 14,
            AllowSellerPartialRefunds = false
        };
        var service = CreateService(settings);
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            }
        };

        var command = new CheckRefundEligibilityCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        // Act
        var result = await service.CheckSellerRefundEligibilityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsEligible);
        Assert.Contains("Seller-initiated refunds are not allowed", result.IneligibilityReason);
    }

    [Fact]
    public async Task CheckSellerRefundEligibilityAsync_EscrowReleased_ReturnsNotEligible()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Released, // Already released
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            }
        };

        var command = new CheckRefundEligibilityCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        // Act
        var result = await service.CheckSellerRefundEligibilityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsEligible);
        Assert.Contains("already been released", result.IneligibilityReason);
    }

    [Fact]
    public async Task CheckSellerRefundEligibilityAsync_MaxPercentageExceeded_ReturnsNotEligible()
    {
        // Arrange
        var settings = new RefundSettings
        {
            SellerRefundWindowDays = 14,
            AllowSellerPartialRefunds = true,
            MaxSellerRefundPercentage = 50m // 50% max
        };
        var service = CreateService(settings);
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            }
        };

        var command = new CheckRefundEligibilityCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 60.00m // Exceeds 50%
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.GetTotalRefundedByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync(0m);

        // Act
        var result = await service.CheckSellerRefundEligibilityAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.IsEligible);
        Assert.Contains("exceeds maximum refundable amount", result.IneligibilityReason);
    }

    #endregion

    #region Audit and Logging Tests

    [Fact]
    public async Task ProcessFullRefundAsync_SetsAuditInformation()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var command = new ProcessFullRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = Guid.NewGuid(),
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedByRole = "Admin",
            AuditNote = "Approved by supervisor"
        };

        Refund? capturedRefund = null;

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.AddAsync(It.IsAny<Refund>()))
            .Callback<Refund>(r => capturedRefund = r)
            .ReturnsAsync((Refund r) => r);

        _mockEscrowService
            .Setup(e => e.RefundEscrowAsync(It.IsAny<RefundEscrowCommand>()))
            .ReturnsAsync(RefundEscrowResult.Success(escrowEntries));

        _mockCommissionService
            .Setup(c => c.RecalculatePartialRefundAsync(It.IsAny<RecalculatePartialRefundCommand>()))
            .ReturnsAsync(RecalculatePartialRefundResult.Success(new CommissionRecord()));

        _mockRefundRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Refund>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ProcessFullRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedRefund);
        Assert.Equal("admin-1", capturedRefund.InitiatedByUserId);
        Assert.Equal("Admin", capturedRefund.InitiatedByRole);
        Assert.Equal("Customer request", capturedRefund.Reason);
        Assert.Equal("Approved by supervisor", capturedRefund.AuditNote);
    }

    [Fact]
    public async Task ProcessPartialRefundAsync_TracksCommissionAndEscrowRefunded()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var escrowEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                RefundedAmount = 0m,
                Currency = "USD",
                Status = EscrowStatus.Held
            }
        };

        var command = new ProcessPartialRefundCommand
        {
            OrderId = orderId,
            PaymentTransactionId = Guid.NewGuid(),
            SellerId = sellerId,
            Amount = 50.00m,
            Reason = "Partial return",
            InitiatedByUserId = "seller-1",
            InitiatedByRole = "Seller"
        };

        _mockEscrowService
            .Setup(e => e.GetEscrowEntriesByOrderIdAsync(orderId))
            .ReturnsAsync(GetEscrowEntriesResult.Success(escrowEntries));

        _mockRefundRepository
            .Setup(r => r.AddAsync(It.IsAny<Refund>()))
            .ReturnsAsync((Refund r) => r);

        _mockEscrowService
            .Setup(e => e.PartialRefundEscrowAsync(It.IsAny<PartialRefundEscrowCommand>()))
            .ReturnsAsync(PartialRefundEscrowResult.Success(escrowEntries[0], 50.00m, 50.00m));

        _mockCommissionService
            .Setup(c => c.RecalculatePartialRefundAsync(It.IsAny<RecalculatePartialRefundCommand>()))
            .ReturnsAsync(RecalculatePartialRefundResult.Success(new CommissionRecord
            {
                RefundedCommissionAmount = 5.00m
            }));

        _mockRefundRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Refund>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.ProcessPartialRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Refund);
        Assert.Equal(50.00m, result.Refund.EscrowRefunded);
        Assert.Equal(5.00m, result.Refund.CommissionRefunded);
    }

    #endregion
}
