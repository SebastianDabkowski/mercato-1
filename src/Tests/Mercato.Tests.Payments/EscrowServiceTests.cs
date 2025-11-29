using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Payments;

public class EscrowServiceTests
{
    private readonly Mock<IEscrowRepository> _mockRepository;
    private readonly Mock<ILogger<EscrowService>> _mockLogger;

    public EscrowServiceTests()
    {
        _mockRepository = new Mock<IEscrowRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<EscrowService>>();
    }

    private EscrowService CreateService(EscrowSettings? settings = null)
    {
        var escrowSettings = settings ?? new EscrowSettings();
        var options = Options.Create(escrowSettings);
        return new EscrowService(_mockRepository.Object, _mockLogger.Object, options);
    }

    #region HoldEscrowAsync Tests

    [Fact]
    public async Task HoldEscrowAsync_ValidCommand_CreatesEscrowEntriesPerSeller()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = paymentTransactionId,
            OrderId = orderId,
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = seller1Id, Amount = 100.00m },
                new SellerAllocation { SellerId = seller2Id, Amount = 50.00m }
            ],
            AuditNote = "Test escrow"
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Entries.Count);
        Assert.Contains(result.Entries, e => e.SellerId == seller1Id && e.Amount == 100.00m);
        Assert.Contains(result.Entries, e => e.SellerId == seller2Id && e.Amount == 50.00m);
        Assert.All(result.Entries, e =>
        {
            Assert.Equal(EscrowStatus.Held, e.Status);
            Assert.Equal(orderId, e.OrderId);
            Assert.Equal(paymentTransactionId, e.PaymentTransactionId);
            Assert.Equal("USD", e.Currency);
            Assert.False(e.IsEligibleForPayout);
        });

        _mockRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<EscrowEntry>>(
            entries => entries.Count() == 2)), Times.Once);
    }

    [Fact]
    public async Task HoldEscrowAsync_SingleSeller_CreatesOneEntry()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = sellerId, Amount = 75.50m }
            ]
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Entries);
        Assert.Equal(sellerId, result.Entries[0].SellerId);
        Assert.Equal(75.50m, result.Entries[0].Amount);
    }

    [Fact]
    public async Task HoldEscrowAsync_MissingPaymentTransactionId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.Empty,
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payment transaction ID is required"));
    }

    [Fact]
    public async Task HoldEscrowAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.Empty,
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task HoldEscrowAsync_EmptySellerAllocations_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations = []
        };

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("At least one seller allocation is required"));
    }

    [Fact]
    public async Task HoldEscrowAsync_ZeroAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 0 }
            ]
        };

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Amount must be greater than zero"));
    }

    [Fact]
    public async Task HoldEscrowAsync_MissingCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency is required"));
    }

    [Fact]
    public async Task HoldEscrowAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.Empty, Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    #endregion

    #region ReleaseEscrowAsync Tests

    [Fact]
    public async Task ReleaseEscrowAsync_ValidCommand_ReleasesEscrowAndUpdatesStatus()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new ReleaseEscrowCommand
        {
            OrderId = orderId,
            AuditNote = "Order fulfilled"
        };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Entries);
        Assert.Equal(EscrowStatus.Released, result.Entries[0].Status);
        Assert.NotNull(result.Entries[0].ReleasedAt);

        _mockRepository.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<EscrowEntry>>(
            entries => entries.All(e => e.Status == EscrowStatus.Released))), Times.Once);
    }

    [Fact]
    public async Task ReleaseEscrowAsync_SpecificSeller_ReleasesOnlyThatSeller()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = seller1Id,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = seller2Id,
                Amount = 50.00m,
                Status = EscrowStatus.Held
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new ReleaseEscrowCommand
        {
            OrderId = orderId,
            SellerId = seller1Id
        };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Entries);
        Assert.Equal(seller1Id, result.Entries[0].SellerId);
        Assert.Equal(EscrowStatus.Released, result.Entries[0].Status);

        _mockRepository.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<EscrowEntry>>(
            entries => entries.Count() == 1 && entries.First().SellerId == seller1Id)), Times.Once);
    }

    [Fact]
    public async Task ReleaseEscrowAsync_AlreadyReleased_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Released,
                ReleasedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        var command = new ReleaseEscrowCommand { OrderId = orderId };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already been released"));
    }

    [Fact]
    public async Task ReleaseEscrowAsync_AlreadyRefunded_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Refunded,
                RefundedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        var command = new ReleaseEscrowCommand { OrderId = orderId };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already been refunded"));
    }

    [Fact]
    public async Task ReleaseEscrowAsync_NoEntriesFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry>());

        var command = new ReleaseEscrowCommand { OrderId = orderId };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entries found for the specified order"));
    }

    [Fact]
    public async Task ReleaseEscrowAsync_SellerNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var existingSellerId = Guid.NewGuid();
        var requestedSellerId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = existingSellerId,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        var command = new ReleaseEscrowCommand
        {
            OrderId = orderId,
            SellerId = requestedSellerId
        };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entries found for the specified seller"));
    }

    [Fact]
    public async Task ReleaseEscrowAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new ReleaseEscrowCommand { OrderId = Guid.Empty };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    #endregion

    #region RefundEscrowAsync Tests

    [Fact]
    public async Task RefundEscrowAsync_ValidCommand_RefundsEscrowAndUpdatesStatus()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = sellerId,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new RefundEscrowCommand
        {
            OrderId = orderId,
            AuditNote = "Order cancelled"
        };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Entries);
        Assert.Equal(EscrowStatus.Refunded, result.Entries[0].Status);
        Assert.NotNull(result.Entries[0].RefundedAt);

        _mockRepository.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<EscrowEntry>>(
            entries => entries.All(e => e.Status == EscrowStatus.Refunded))), Times.Once);
    }

    [Fact]
    public async Task RefundEscrowAsync_SpecificSeller_RefundsOnlyThatSeller()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = seller1Id,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = seller2Id,
                Amount = 50.00m,
                Status = EscrowStatus.Held
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new RefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = seller1Id
        };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Entries);
        Assert.Equal(seller1Id, result.Entries[0].SellerId);
        Assert.Equal(EscrowStatus.Refunded, result.Entries[0].Status);

        _mockRepository.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<EscrowEntry>>(
            entries => entries.Count() == 1 && entries.First().SellerId == seller1Id)), Times.Once);
    }

    [Fact]
    public async Task RefundEscrowAsync_AlreadyRefunded_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Refunded,
                RefundedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        var command = new RefundEscrowCommand { OrderId = orderId };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already been refunded"));
    }

    [Fact]
    public async Task RefundEscrowAsync_AlreadyReleased_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Released,
                ReleasedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        var command = new RefundEscrowCommand { OrderId = orderId };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already been released"));
    }

    [Fact]
    public async Task RefundEscrowAsync_NoEntriesFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry>());

        var command = new RefundEscrowCommand { OrderId = orderId };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entries found for the specified order"));
    }

    [Fact]
    public async Task RefundEscrowAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RefundEscrowCommand { OrderId = Guid.Empty };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    #endregion

    #region GetEscrowEntriesByOrderIdAsync Tests

    [Fact]
    public async Task GetEscrowEntriesByOrderIdAsync_ValidOrderId_ReturnsEntries()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Held
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 50.00m,
                Status = EscrowStatus.Released
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        // Act
        var result = await service.GetEscrowEntriesByOrderIdAsync(orderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Entries.Count);
    }

    [Fact]
    public async Task GetEscrowEntriesByOrderIdAsync_EmptyOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetEscrowEntriesByOrderIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task GetEscrowEntriesByOrderIdAsync_NoEntriesFound_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry>());

        // Act
        var result = await service.GetEscrowEntriesByOrderIdAsync(orderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Entries);
    }

    #endregion

    #region GetEscrowEntriesBySellerIdAsync Tests

    [Fact]
    public async Task GetEscrowEntriesBySellerIdAsync_ValidSellerId_ReturnsEntries()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            },
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = 50.00m,
                Status = EscrowStatus.Released
            }
        };

        _mockRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(existingEntries);

        // Act
        var result = await service.GetEscrowEntriesBySellerIdAsync(sellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Entries.Count);
    }

    [Fact]
    public async Task GetEscrowEntriesBySellerIdAsync_WithStatusFilter_ReturnsFilteredEntries()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var filteredEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = 100.00m,
                Status = EscrowStatus.Held
            }
        };

        _mockRepository
            .Setup(r => r.GetBySellerIdAndStatusAsync(sellerId, EscrowStatus.Held))
            .ReturnsAsync(filteredEntries);

        // Act
        var result = await service.GetEscrowEntriesBySellerIdAsync(sellerId, EscrowStatus.Held);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Entries);
        Assert.All(result.Entries, e => Assert.Equal(EscrowStatus.Held, e.Status));
    }

    [Fact]
    public async Task GetEscrowEntriesBySellerIdAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetEscrowEntriesBySellerIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    #endregion

    #region Multi-Seller Order Tests

    [Fact]
    public async Task HoldEscrowAsync_MultiSellerOrder_CreatesMultipleEscrowEntries()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();
        var seller3Id = Guid.NewGuid();

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = paymentTransactionId,
            OrderId = orderId,
            Currency = "EUR",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = seller1Id, Amount = 100.00m },
                new SellerAllocation { SellerId = seller2Id, Amount = 75.50m },
                new SellerAllocation { SellerId = seller3Id, Amount = 25.00m }
            ],
            AuditNote = "Multi-seller order escrow"
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(200.50m, result.Entries.Sum(e => e.Amount));
        Assert.All(result.Entries, e =>
        {
            Assert.Equal(orderId, e.OrderId);
            Assert.Equal(paymentTransactionId, e.PaymentTransactionId);
            Assert.Equal(EscrowStatus.Held, e.Status);
            Assert.Equal("EUR", e.Currency);
        });
    }

    [Fact]
    public async Task RefundEscrowAsync_MultiSellerOrder_RefundsAllSellers()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry { Id = Guid.NewGuid(), OrderId = orderId, SellerId = Guid.NewGuid(), Amount = 100.00m, Status = EscrowStatus.Held },
            new EscrowEntry { Id = Guid.NewGuid(), OrderId = orderId, SellerId = Guid.NewGuid(), Amount = 75.50m, Status = EscrowStatus.Held },
            new EscrowEntry { Id = Guid.NewGuid(), OrderId = orderId, SellerId = Guid.NewGuid(), Amount = 25.00m, Status = EscrowStatus.Held }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new RefundEscrowCommand
        {
            OrderId = orderId,
            AuditNote = "Order cancelled - refunding all sellers"
        };

        // Act
        var result = await service.RefundEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Entries.Count);
        Assert.All(result.Entries, e =>
        {
            Assert.Equal(EscrowStatus.Refunded, e.Status);
            Assert.NotNull(e.RefundedAt);
        });
    }

    #endregion

    #region Audit Tests

    [Fact]
    public async Task HoldEscrowAsync_WithAuditNote_StoresAuditNote()
    {
        // Arrange
        var service = CreateService();
        var auditNote = "Payment confirmed via PayPal";

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ],
            AuditNote = auditNote
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(auditNote, result.Entries[0].AuditNote);
    }

    [Fact]
    public async Task HoldEscrowAsync_WithoutAuditNote_UsesDefaultNote()
    {
        // Arrange
        var settings = new EscrowSettings { PayoutEligibilityDays = 14 };
        var service = CreateService(settings);

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("Escrow created on payment confirmation", result.Entries[0].AuditNote);
        Assert.Contains("14 days", result.Entries[0].AuditNote);
    }

    [Fact]
    public async Task HoldEscrowAsync_WithCustomPayoutEligibilityDays_ReflectsInAuditNote()
    {
        // Arrange
        var settings = new EscrowSettings { PayoutEligibilityDays = 30 };
        var service = CreateService(settings);

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.HoldEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Contains("30 days", result.Entries[0].AuditNote);
    }

    [Fact]
    public async Task ReleaseEscrowAsync_UpdatesAuditNote()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var auditNote = "Released after order delivery confirmation";

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Held,
                AuditNote = "Original note"
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var command = new ReleaseEscrowCommand
        {
            OrderId = orderId,
            AuditNote = auditNote
        };

        // Act
        var result = await service.ReleaseEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(auditNote, result.Entries[0].AuditNote);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public async Task HoldEscrowAsync_SetsCreatedAtAndLastUpdatedAt()
    {
        // Arrange
        var service = CreateService();

        var command = new HoldEscrowCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Currency = "USD",
            SellerAllocations =
            [
                new SellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = await service.HoldEscrowAsync(command);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        var entry = result.Entries[0];
        Assert.True(entry.CreatedAt >= beforeTime && entry.CreatedAt <= afterTime);
        Assert.True(entry.LastUpdatedAt >= beforeTime && entry.LastUpdatedAt <= afterTime);
        Assert.Equal(entry.CreatedAt, entry.LastUpdatedAt);
    }

    [Fact]
    public async Task ReleaseEscrowAsync_SetsReleasedAtAndUpdatesLastUpdatedAt()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Held,
                CreatedAt = originalCreatedAt,
                LastUpdatedAt = originalCreatedAt
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = await service.ReleaseEscrowAsync(new ReleaseEscrowCommand { OrderId = orderId });

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        var entry = result.Entries[0];
        Assert.NotNull(entry.ReleasedAt);
        Assert.True(entry.ReleasedAt >= beforeTime && entry.ReleasedAt <= afterTime);
        Assert.True(entry.LastUpdatedAt >= beforeTime && entry.LastUpdatedAt <= afterTime);
        Assert.Equal(originalCreatedAt, entry.CreatedAt);
    }

    [Fact]
    public async Task RefundEscrowAsync_SetsRefundedAtAndUpdatesLastUpdatedAt()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var existingEntries = new List<EscrowEntry>
        {
            new EscrowEntry
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = EscrowStatus.Held,
                CreatedAt = originalCreatedAt,
                LastUpdatedAt = originalCreatedAt
            }
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingEntries);

        _mockRepository
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EscrowEntry>>()))
            .Returns(Task.CompletedTask);

        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = await service.RefundEscrowAsync(new RefundEscrowCommand { OrderId = orderId });

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        var entry = result.Entries[0];
        Assert.NotNull(entry.RefundedAt);
        Assert.True(entry.RefundedAt >= beforeTime && entry.RefundedAt <= afterTime);
        Assert.True(entry.LastUpdatedAt >= beforeTime && entry.LastUpdatedAt <= afterTime);
        Assert.Equal(originalCreatedAt, entry.CreatedAt);
    }

    #endregion

    #region PartialRefundEscrowAsync Tests

    [Fact]
    public async Task PartialRefundEscrowAsync_ValidCommand_RefundsPartialAmountAndUpdatesStatus()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 0m,
            Status = EscrowStatus.Held,
            Currency = "USD",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<EscrowEntry>()))
            .Returns(Task.CompletedTask);

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m,
            AuditNote = "Partial refund for item return"
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Entry);
        Assert.Equal(25.00m, result.RefundedAmount);
        Assert.Equal(75.00m, result.RemainingAmount);
        Assert.Equal(EscrowStatus.PartiallyRefunded, result.Entry.Status);
        Assert.Equal(25.00m, result.Entry.RefundedAmount);

        _mockRepository.Verify(r => r.UpdateAsync(It.Is<EscrowEntry>(
            e => e.Status == EscrowStatus.PartiallyRefunded && e.RefundedAmount == 25.00m)), Times.Once);
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_RefundEntireAmount_ChangesStatusToRefunded()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 0m,
            Status = EscrowStatus.Held,
            Currency = "USD"
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<EscrowEntry>()))
            .Returns(Task.CompletedTask);

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m // Full amount
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Entry);
        Assert.Equal(100.00m, result.RefundedAmount);
        Assert.Equal(0m, result.RemainingAmount);
        Assert.Equal(EscrowStatus.Refunded, result.Entry.Status);
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_MultiplePartialRefunds_AccumulatesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 40.00m, // Already partially refunded
            Status = EscrowStatus.PartiallyRefunded,
            Currency = "USD"
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<EscrowEntry>()))
            .Returns(Task.CompletedTask);

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 30.00m // Additional refund
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Entry);
        Assert.Equal(30.00m, result.RefundedAmount);
        Assert.Equal(30.00m, result.RemainingAmount); // 100 - 40 - 30
        Assert.Equal(70.00m, result.Entry.RefundedAmount); // 40 + 30
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_AmountExceedsRemaining_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 80.00m,
            Status = EscrowStatus.PartiallyRefunded,
            Currency = "USD"
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 30.00m // Exceeds remaining 20
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("exceeds remaining escrow amount"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_EscrowAlreadyReleased_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 0m,
            Status = EscrowStatus.Released
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already been released"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_EscrowFullyRefunded_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 100.00m,
            Status = EscrowStatus.Refunded
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already been fully refunded"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_ZeroAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new PartialRefundEscrowCommand
        {
            OrderId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = 0m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund amount must be greater than zero"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_NegativeAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new PartialRefundEscrowCommand
        {
            OrderId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Amount = -25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund amount must be greater than zero"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_SellerNotFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var existingSellerId = Guid.NewGuid();
        var requestedSellerId = Guid.NewGuid();

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = existingSellerId,
            Amount = 100.00m,
            RefundedAmount = 0m,
            Status = EscrowStatus.Held
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = requestedSellerId,
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entry found for the specified seller"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_NoEntriesForOrder_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry>());

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = Guid.NewGuid(),
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No escrow entries found for the specified order"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new PartialRefundEscrowCommand
        {
            OrderId = Guid.Empty,
            SellerId = Guid.NewGuid(),
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_MissingSellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new PartialRefundEscrowCommand
        {
            OrderId = Guid.NewGuid(),
            SellerId = Guid.Empty,
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task PartialRefundEscrowAsync_SetsTimestampsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var existingEntry = new EscrowEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 100.00m,
            RefundedAmount = 0m,
            Status = EscrowStatus.Held,
            Currency = "USD",
            CreatedAt = originalCreatedAt,
            LastUpdatedAt = originalCreatedAt
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<EscrowEntry> { existingEntry });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<EscrowEntry>()))
            .Returns(Task.CompletedTask);

        var beforeTime = DateTimeOffset.UtcNow;

        var command = new PartialRefundEscrowCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            Amount = 25.00m
        };

        // Act
        var result = await service.PartialRefundEscrowAsync(command);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Entry);
        Assert.NotNull(result.Entry.RefundedAt);
        Assert.True(result.Entry.RefundedAt >= beforeTime && result.Entry.RefundedAt <= afterTime);
        Assert.True(result.Entry.LastUpdatedAt >= beforeTime && result.Entry.LastUpdatedAt <= afterTime);
        Assert.Equal(originalCreatedAt, result.Entry.CreatedAt);
    }

    #endregion
}
