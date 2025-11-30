using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Payments;

public class CommissionServiceTests
{
    private readonly Mock<ICommissionRuleRepository> _mockRuleRepository;
    private readonly Mock<ICommissionRecordRepository> _mockRecordRepository;
    private readonly Mock<ILogger<CommissionService>> _mockLogger;

    public CommissionServiceTests()
    {
        _mockRuleRepository = new Mock<ICommissionRuleRepository>(MockBehavior.Strict);
        _mockRecordRepository = new Mock<ICommissionRecordRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CommissionService>>();
    }

    private CommissionService CreateService(CommissionSettings? settings = null)
    {
        var commissionSettings = settings ?? new CommissionSettings();
        var options = Options.Create(commissionSettings);
        return new CommissionService(
            _mockRuleRepository.Object, 
            _mockRecordRepository.Object, 
            _mockLogger.Object, 
            options);
    }

    #region CalculateCommissionAsync Tests

    [Fact]
    public async Task CalculateCommissionAsync_WithMatchingSellerRule_UsesSellerRate()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var sellerRule = new CommissionRule
        {
            Id = ruleId,
            SellerId = sellerId,
            CategoryId = null,
            CommissionRate = 8.5m,
            IsActive = true,
            Priority = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = paymentTransactionId,
            OrderId = orderId,
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 100.00m, CategoryId = null }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(sellerRule);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.CommissionRecords);
        var record = result.CommissionRecords[0];
        Assert.Equal(8.5m, record.CommissionRate);
        Assert.Equal(8.50m, record.CommissionAmount); // 100 * 8.5%
        Assert.Equal(ruleId, record.AppliedRuleId);
        Assert.Equal(sellerId, record.SellerId);
        Assert.Equal(orderId, record.OrderId);
        Assert.Equal(paymentTransactionId, record.PaymentTransactionId);
    }

    [Fact]
    public async Task CalculateCommissionAsync_WithMatchingCategoryRule_UsesCategoryRate()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var categoryId = "electronics";
        var ruleId = Guid.NewGuid();

        var categoryRule = new CommissionRule
        {
            Id = ruleId,
            SellerId = null,
            CategoryId = categoryId,
            CommissionRate = 12.0m,
            IsActive = true,
            Priority = 5,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 200.00m, CategoryId = categoryId }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, categoryId, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(categoryRule);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.CommissionRecords);
        Assert.Equal(12.0m, result.CommissionRecords[0].CommissionRate);
        Assert.Equal(24.00m, result.CommissionRecords[0].CommissionAmount); // 200 * 12%
    }

    [Fact]
    public async Task CalculateCommissionAsync_WithGlobalDefaultRule_UsesGlobalRate()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var globalRule = new CommissionRule
        {
            Id = ruleId,
            SellerId = null,
            CategoryId = null,
            CommissionRate = 10.0m,
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 50.00m, CategoryId = null }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(globalRule);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.CommissionRecords);
        Assert.Equal(10.0m, result.CommissionRecords[0].CommissionRate);
        Assert.Equal(5.00m, result.CommissionRecords[0].CommissionAmount); // 50 * 10%
    }

    [Fact]
    public async Task CalculateCommissionAsync_NoMatchingRule_UsesDefaultFromSettings()
    {
        // Arrange
        var settings = new CommissionSettings { DefaultCommissionRate = 15.0m };
        var service = CreateService(settings);
        var sellerId = Guid.NewGuid();

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 100.00m, CategoryId = null }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((CommissionRule?)null);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.CommissionRecords);
        Assert.Equal(15.0m, result.CommissionRecords[0].CommissionRate);
        Assert.Equal(15.00m, result.CommissionRecords[0].CommissionAmount); // 100 * 15%
        Assert.Null(result.CommissionRecords[0].AppliedRuleId);
        Assert.Contains("Default rate", result.CommissionRecords[0].AppliedRuleDescription);
    }

    [Fact]
    public async Task CalculateCommissionAsync_WithMinConstraint_AppliesMinimum()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var ruleWithMin = new CommissionRule
        {
            Id = ruleId,
            SellerId = null,
            CategoryId = null,
            CommissionRate = 5.0m,
            MinCommission = 10.00m,
            MaxCommission = null,
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 50.00m, CategoryId = null }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(ruleWithMin);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        // Calculated: 50 * 5% = 2.50, but min is 10.00
        Assert.Equal(10.00m, result.CommissionRecords[0].CommissionAmount);
    }

    [Fact]
    public async Task CalculateCommissionAsync_WithMaxConstraint_AppliesMaximum()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var ruleWithMax = new CommissionRule
        {
            Id = ruleId,
            SellerId = null,
            CategoryId = null,
            CommissionRate = 20.0m,
            MinCommission = null,
            MaxCommission = 50.00m,
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 500.00m, CategoryId = null }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(ruleWithMax);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        // Calculated: 500 * 20% = 100.00, but max is 50.00
        Assert.Equal(50.00m, result.CommissionRecords[0].CommissionAmount);
    }

    [Fact]
    public async Task CalculateCommissionAsync_MultipleSellers_CreatesMultipleRecords()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();
        var seller3Id = Guid.NewGuid();

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = paymentTransactionId,
            OrderId = orderId,
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = seller1Id, Amount = 100.00m, CategoryId = null },
                new CommissionSellerAllocation { SellerId = seller2Id, Amount = 200.00m, CategoryId = "electronics" },
                new CommissionSellerAllocation { SellerId = seller3Id, Amount = 50.00m, CategoryId = "books" }
            ]
        };

        // Different rules for different sellers/categories
        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(seller1Id, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((CommissionRule?)null);

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(seller2Id, "electronics", It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(new CommissionRule
            {
                Id = Guid.NewGuid(),
                CategoryId = "electronics",
                CommissionRate = 15.0m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(seller3Id, "books", It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(new CommissionRule
            {
                Id = Guid.NewGuid(),
                CategoryId = "books",
                CommissionRate = 5.0m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.CommissionRecords.Count);

        var record1 = result.CommissionRecords.First(r => r.SellerId == seller1Id);
        Assert.Equal(10.0m, record1.CommissionRate); // Default from settings
        Assert.Equal(10.00m, record1.CommissionAmount); // 100 * 10%

        var record2 = result.CommissionRecords.First(r => r.SellerId == seller2Id);
        Assert.Equal(15.0m, record2.CommissionRate);
        Assert.Equal(30.00m, record2.CommissionAmount); // 200 * 15%

        var record3 = result.CommissionRecords.First(r => r.SellerId == seller3Id);
        Assert.Equal(5.0m, record3.CommissionRate);
        Assert.Equal(2.50m, record3.CommissionAmount); // 50 * 5%

        Assert.All(result.CommissionRecords, r =>
        {
            Assert.Equal(orderId, r.OrderId);
            Assert.Equal(paymentTransactionId, r.PaymentTransactionId);
            Assert.Equal(0m, r.RefundedAmount);
            Assert.Equal(0m, r.RefundedCommissionAmount);
            Assert.Equal(r.CommissionAmount, r.NetCommissionAmount);
        });

        _mockRecordRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<CommissionRecord>>(
            records => records.Count() == 3)), Times.Once);
    }

    [Fact]
    public async Task CalculateCommissionAsync_MissingPaymentTransactionId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.Empty,
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payment transaction ID is required"));
    }

    [Fact]
    public async Task CalculateCommissionAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.Empty,
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = Guid.NewGuid(), Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task CalculateCommissionAsync_EmptySellerAllocations_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations = []
        };

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("At least one seller allocation is required"));
    }

    [Fact]
    public async Task CalculateCommissionAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = Guid.Empty, Amount = 100.00m }
            ]
        };

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task CalculateCommissionAsync_ZeroAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = Guid.NewGuid(), Amount = 0 }
            ]
        };

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Amount must be greater than zero"));
    }

    [Fact]
    public async Task CalculateCommissionAsync_SetsTimestampsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 100.00m }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((CommissionRule?)null);

        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Returns(Task.CompletedTask);

        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = await service.CalculateCommissionAsync(command);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        var record = result.CommissionRecords[0];
        Assert.True(record.CreatedAt >= beforeTime && record.CreatedAt <= afterTime);
        Assert.True(record.LastUpdatedAt >= beforeTime && record.LastUpdatedAt <= afterTime);
        Assert.True(record.CalculatedAt >= beforeTime && record.CalculatedAt <= afterTime);
        Assert.Null(record.LastRefundRecalculatedAt);
        Assert.Equal(record.CreatedAt, record.LastUpdatedAt);
        Assert.Equal(record.CreatedAt, record.CalculatedAt);
    }

    #endregion

    #region RecalculatePartialRefundAsync Tests

    [Fact]
    public async Task RecalculatePartialRefundAsync_ValidRefund_RecalculatesProportionally()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingRecord = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            OrderAmount = 100.00m,
            CommissionRate = 10.0m,
            CommissionAmount = 10.00m,
            RefundedAmount = 0m,
            RefundedCommissionAmount = 0m,
            NetCommissionAmount = 10.00m,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            LastUpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CalculatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var command = new RecalculatePartialRefundCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            RefundAmount = 25.00m
        };

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync(existingRecord);

        _mockRecordRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionRecord>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.UpdatedRecord);
        Assert.Equal(25.00m, result.UpdatedRecord.RefundedAmount);
        Assert.Equal(2.50m, result.UpdatedRecord.RefundedCommissionAmount); // 25% of 10.00
        Assert.Equal(7.50m, result.UpdatedRecord.NetCommissionAmount); // 10.00 - 2.50
        Assert.NotNull(result.UpdatedRecord.LastRefundRecalculatedAt);

        _mockRecordRepository.Verify(r => r.UpdateAsync(It.Is<CommissionRecord>(
            record => record.RefundedAmount == 25.00m && 
                      record.RefundedCommissionAmount == 2.50m && 
                      record.NetCommissionAmount == 7.50m)), Times.Once);
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_MultiplePartialRefunds_AccumulatesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        // Record already has a previous partial refund
        var existingRecord = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            OrderAmount = 100.00m,
            CommissionRate = 10.0m,
            CommissionAmount = 10.00m,
            RefundedAmount = 20.00m,
            RefundedCommissionAmount = 2.00m,
            NetCommissionAmount = 8.00m,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            LastUpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CalculatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            LastRefundRecalculatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var command = new RecalculatePartialRefundCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            RefundAmount = 30.00m // Additional refund
        };

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync(existingRecord);

        _mockRecordRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionRecord>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.UpdatedRecord);
        Assert.Equal(50.00m, result.UpdatedRecord.RefundedAmount); // 20 + 30
        Assert.Equal(5.00m, result.UpdatedRecord.RefundedCommissionAmount); // 2 + 3 (30% of 10.00)
        Assert.Equal(5.00m, result.UpdatedRecord.NetCommissionAmount); // 10.00 - 5.00
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_RefundExceedsRemainingAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingRecord = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            OrderAmount = 100.00m,
            CommissionAmount = 10.00m,
            RefundedAmount = 80.00m,
            RefundedCommissionAmount = 8.00m,
            NetCommissionAmount = 2.00m
        };

        var command = new RecalculatePartialRefundCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            RefundAmount = 30.00m // Only 20 remaining
        };

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync(existingRecord);

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("exceeds remaining order amount"));
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_NoRecordFound_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var command = new RecalculatePartialRefundCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            RefundAmount = 10.00m
        };

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync((CommissionRecord?)null);

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No commission record found"));
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_MissingOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RecalculatePartialRefundCommand
        {
            OrderId = Guid.Empty,
            SellerId = Guid.NewGuid(),
            RefundAmount = 10.00m
        };

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_MissingSellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RecalculatePartialRefundCommand
        {
            OrderId = Guid.NewGuid(),
            SellerId = Guid.Empty,
            RefundAmount = 10.00m
        };

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_ZeroRefundAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RecalculatePartialRefundCommand
        {
            OrderId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            RefundAmount = 0m
        };

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund amount must be greater than zero"));
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_NegativeRefundAmount_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new RecalculatePartialRefundCommand
        {
            OrderId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            RefundAmount = -10.00m
        };

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Refund amount must be greater than zero"));
    }

    [Fact]
    public async Task RecalculatePartialRefundAsync_PreservesOriginalCommissionValues()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var originalCalculatedAt = DateTimeOffset.UtcNow.AddDays(-5);

        var existingRecord = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = orderId,
            SellerId = sellerId,
            OrderAmount = 100.00m,
            CommissionRate = 10.0m,
            CommissionAmount = 10.00m,
            RefundedAmount = 0m,
            RefundedCommissionAmount = 0m,
            NetCommissionAmount = 10.00m,
            AppliedRuleId = Guid.NewGuid(),
            AppliedRuleDescription = "Original rule description",
            CreatedAt = originalCalculatedAt,
            LastUpdatedAt = originalCalculatedAt,
            CalculatedAt = originalCalculatedAt
        };

        var command = new RecalculatePartialRefundCommand
        {
            OrderId = orderId,
            SellerId = sellerId,
            RefundAmount = 50.00m
        };

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAndSellerIdAsync(orderId, sellerId))
            .ReturnsAsync(existingRecord);

        _mockRecordRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionRecord>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.RecalculatePartialRefundAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.UpdatedRecord);
        // Original values should be preserved
        Assert.Equal(100.00m, result.UpdatedRecord.OrderAmount);
        Assert.Equal(10.0m, result.UpdatedRecord.CommissionRate);
        Assert.Equal(10.00m, result.UpdatedRecord.CommissionAmount);
        Assert.Equal(originalCalculatedAt, result.UpdatedRecord.CalculatedAt);
        Assert.Equal(originalCalculatedAt, result.UpdatedRecord.CreatedAt);
        Assert.Equal("Original rule description", result.UpdatedRecord.AppliedRuleDescription);
        // Updated values
        Assert.Equal(50.00m, result.UpdatedRecord.RefundedAmount);
        Assert.Equal(5.00m, result.UpdatedRecord.RefundedCommissionAmount);
        Assert.Equal(5.00m, result.UpdatedRecord.NetCommissionAmount);
        Assert.True(result.UpdatedRecord.LastUpdatedAt > originalCalculatedAt);
        Assert.NotNull(result.UpdatedRecord.LastRefundRecalculatedAt);
    }

    #endregion

    #region GetCommissionRecordsByOrderIdAsync Tests

    [Fact]
    public async Task GetCommissionRecordsByOrderIdAsync_ValidOrderId_ReturnsRecords()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        var existingRecords = new List<CommissionRecord>
        {
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                OrderAmount = 100.00m,
                CommissionRate = 10.0m,
                CommissionAmount = 10.00m,
                NetCommissionAmount = 10.00m
            },
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                SellerId = Guid.NewGuid(),
                OrderAmount = 50.00m,
                CommissionRate = 15.0m,
                CommissionAmount = 7.50m,
                NetCommissionAmount = 7.50m
            }
        };

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingRecords);

        // Act
        var result = await service.GetCommissionRecordsByOrderIdAsync(orderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Records.Count);
    }

    [Fact]
    public async Task GetCommissionRecordsByOrderIdAsync_EmptyOrderId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetCommissionRecordsByOrderIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task GetCommissionRecordsByOrderIdAsync_NoRecordsFound_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var orderId = Guid.NewGuid();

        _mockRecordRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<CommissionRecord>());

        // Act
        var result = await service.GetCommissionRecordsByOrderIdAsync(orderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Records);
    }

    #endregion

    #region GetCommissionRecordsBySellerIdAsync Tests

    [Fact]
    public async Task GetCommissionRecordsBySellerIdAsync_ValidSellerId_ReturnsRecords()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var existingRecords = new List<CommissionRecord>
        {
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                SellerId = sellerId,
                OrderAmount = 100.00m,
                CommissionRate = 10.0m,
                CommissionAmount = 10.00m,
                NetCommissionAmount = 10.00m
            },
            new CommissionRecord
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                SellerId = sellerId,
                OrderAmount = 200.00m,
                CommissionRate = 10.0m,
                CommissionAmount = 20.00m,
                NetCommissionAmount = 15.00m,
                RefundedAmount = 50.00m,
                RefundedCommissionAmount = 5.00m
            }
        };

        _mockRecordRepository
            .Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(existingRecords);

        // Act
        var result = await service.GetCommissionRecordsBySellerIdAsync(sellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Records.Count);
        Assert.All(result.Records, r => Assert.Equal(sellerId, r.SellerId));
    }

    [Fact]
    public async Task GetCommissionRecordsBySellerIdAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetCommissionRecordsBySellerIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller ID is required"));
    }

    #endregion

    #region Historical Records Preservation Tests

    [Fact]
    public async Task CalculateCommissionAsync_DoesNotModifyExistingRecords()
    {
        // This test verifies that calling CalculateCommissionAsync creates new records
        // without modifying any existing historical records
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();

        var command = new CalculateCommissionCommand
        {
            PaymentTransactionId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            SellerAllocations =
            [
                new CommissionSellerAllocation { SellerId = sellerId, Amount = 100.00m }
            ]
        };

        _mockRuleRepository
            .Setup(r => r.GetBestMatchingRuleAsync(sellerId, null, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync((CommissionRule?)null);

        // Capture the records being added
        IEnumerable<CommissionRecord>? addedRecords = null;
        _mockRecordRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()))
            .Callback<IEnumerable<CommissionRecord>>(records => addedRecords = records)
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CalculateCommissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(addedRecords);
        Assert.Single(addedRecords);

        // Verify AddRangeAsync was called (not Update methods)
        _mockRecordRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()), Times.Once);
        _mockRecordRepository.Verify(r => r.UpdateAsync(It.IsAny<CommissionRecord>()), Times.Never);
        _mockRecordRepository.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<CommissionRecord>>()), Times.Never);
    }

    #endregion
}
