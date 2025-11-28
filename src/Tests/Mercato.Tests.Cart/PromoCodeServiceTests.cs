using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Entities;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Cart.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using CartEntity = Mercato.Cart.Domain.Entities.Cart;
using CartItemEntity = Mercato.Cart.Domain.Entities.CartItem;

namespace Mercato.Tests.Cart;

public class PromoCodeServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly string TestGuestCartId = "guest-cart-123";
    private static readonly Guid TestCartId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestPromoCodeId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();

    private readonly Mock<ICartRepository> _mockCartRepository;
    private readonly Mock<IPromoCodeRepository> _mockPromoCodeRepository;
    private readonly Mock<ILogger<PromoCodeService>> _mockLogger;
    private readonly PromoCodeService _service;

    public PromoCodeServiceTests()
    {
        _mockCartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        _mockPromoCodeRepository = new Mock<IPromoCodeRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<PromoCodeService>>();
        _service = new PromoCodeService(
            _mockCartRepository.Object,
            _mockPromoCodeRepository.Object,
            _mockLogger.Object);
    }

    #region ApplyPromoCodeAsync Tests

    [Fact]
    public async Task ApplyPromoCodeAsync_ValidCode_ReturnsSuccess()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "SAVE10"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("SAVE10"))
            .ReturnsAsync(promoCode);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(TestPromoCodeId, result.PromoCodeId);
        Assert.Equal("SAVE10", result.AppliedPromoCode);
        Assert.Equal(10m, result.DiscountAmount);
        _mockCartRepository.Verify(r => r.UpdateAsync(It.Is<CartEntity>(c =>
            c.AppliedPromoCodeId == TestPromoCodeId)), Times.Once);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_FixedAmountDiscount_ReturnsCorrectDiscount()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "FLAT20"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));

        var promoCode = CreateTestPromoCode(DiscountType.FixedAmount, 20m);
        promoCode.Code = "FLAT20";

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("FLAT20"))
            .ReturnsAsync(promoCode);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(20m, result.DiscountAmount);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_InvalidCode_ReturnsInvalidCode()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "INVALID"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("INVALID"))
            .ReturnsAsync((PromoCode?)null);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("invalid or does not exist", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_ExpiredCode_ReturnsExpired()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "EXPIRED"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.Code = "EXPIRED";
        promoCode.EndDate = DateTimeOffset.UtcNow.AddDays(-1); // Expired yesterday

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("EXPIRED"))
            .ReturnsAsync(promoCode);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("expired", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_AlreadyApplied_ReturnsAlreadyApplied()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "NEWCODE"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));
        cart.AppliedPromoCodeId = Guid.NewGuid(); // Already has a promo code

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("already applied", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_MinimumNotMet_ReturnsMinimumNotMet()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "MIN50"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(25m)); // Subtotal $25, below minimum

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.Code = "MIN50";
        promoCode.MinimumOrderAmount = 50m;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("MIN50"))
            .ReturnsAsync(promoCode);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("minimum order", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_UsageLimitReached_ReturnsUsageLimitReached()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "LIMITED"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.Code = "LIMITED";
        promoCode.UsageLimit = 100;
        promoCode.UsageCount = 100; // Already reached limit

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("LIMITED"))
            .ReturnsAsync(promoCode);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("usage limit", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_SellerScopedCode_AppliesToCorrectStore()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "SELLER10"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m, TestStoreId)); // From correct store
        cart.Items.Add(CreateTestCartItem(50m, Guid.NewGuid())); // From different store

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.Code = "SELLER10";
        promoCode.Scope = PromoCodeScope.Seller;
        promoCode.StoreId = TestStoreId;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("SELLER10"))
            .ReturnsAsync(promoCode);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(10m, result.DiscountAmount); // 10% of $100 (only items from matching store)
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_SellerScopedCode_NoMatchingItems_ReturnsNotApplicable()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "SELLER10"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m, Guid.NewGuid())); // From different store

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.Code = "SELLER10";
        promoCode.Scope = PromoCodeScope.Seller;
        promoCode.StoreId = TestStoreId;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("SELLER10"))
            .ReturnsAsync(promoCode);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("not applicable", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_PlatformScopedCode_AppliesToAllItems()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "PLATFORM10"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m, TestStoreId));
        cart.Items.Add(CreateTestCartItem(50m, Guid.NewGuid()));

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.Code = "PLATFORM10";
        promoCode.Scope = PromoCodeScope.Platform;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("PLATFORM10"))
            .ReturnsAsync(promoCode);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(15m, result.DiscountAmount); // 10% of $150
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_PercentageWithMaxDiscount_CapsDiscount()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "MAXED"
        };

        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(500m)); // 20% would be $100, but max is $50

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 20m);
        promoCode.Code = "MAXED";
        promoCode.MaxDiscountAmount = 50m;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("MAXED"))
            .ReturnsAsync(promoCode);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(50m, result.DiscountAmount); // Capped at max
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_EmptyCart_ReturnsFailure()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = "SAVE10"
        };

        var cart = CreateTestCart();
        // No items in cart

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("empty cart", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = string.Empty,
            PromoCode = "SAVE10"
        };

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required", result.Errors[0]);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_EmptyPromoCode_ReturnsFailure()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            BuyerId = TestBuyerId,
            PromoCode = string.Empty
        };

        // Act
        var result = await _service.ApplyPromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Promo code is required", result.Errors[0]);
    }

    #endregion

    #region ApplyPromoCodeToGuestCartAsync Tests

    [Fact]
    public async Task ApplyPromoCodeToGuestCartAsync_ValidCode_ReturnsSuccess()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            GuestCartId = TestGuestCartId,
            PromoCode = "SAVE10"
        };

        var cart = CreateTestGuestCart();
        cart.Items.Add(CreateTestCartItem(100m));

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);

        _mockCartRepository.Setup(r => r.GetByGuestCartIdAsync(TestGuestCartId))
            .ReturnsAsync(cart);

        _mockPromoCodeRepository.Setup(r => r.GetByCodeAsync("SAVE10"))
            .ReturnsAsync(promoCode);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyPromoCodeToGuestCartAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(10m, result.DiscountAmount);
    }

    [Fact]
    public async Task ApplyPromoCodeToGuestCartAsync_EmptyGuestCartId_ReturnsFailure()
    {
        // Arrange
        var command = new ApplyPromoCodeCommand
        {
            GuestCartId = string.Empty,
            PromoCode = "SAVE10"
        };

        // Act
        var result = await _service.ApplyPromoCodeToGuestCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Guest cart ID is required", result.Errors[0]);
    }

    #endregion

    #region RemovePromoCodeAsync Tests

    [Fact]
    public async Task RemovePromoCodeAsync_ExistingPromoCode_ReturnsSuccess()
    {
        // Arrange
        var command = new RemovePromoCodeCommand
        {
            BuyerId = TestBuyerId
        };

        var cart = CreateTestCart();
        cart.AppliedPromoCodeId = TestPromoCodeId;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RemovePromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockCartRepository.Verify(r => r.UpdateAsync(It.Is<CartEntity>(c =>
            c.AppliedPromoCodeId == null)), Times.Once);
    }

    [Fact]
    public async Task RemovePromoCodeAsync_NoPromoCodeApplied_ReturnsSuccess()
    {
        // Arrange
        var command = new RemovePromoCodeCommand
        {
            BuyerId = TestBuyerId
        };

        var cart = CreateTestCart();
        cart.AppliedPromoCodeId = null;

        _mockCartRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.RemovePromoCodeAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RemovePromoCodeAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new RemovePromoCodeCommand
        {
            BuyerId = string.Empty
        };

        // Act
        var result = await _service.RemovePromoCodeAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required", result.Errors[0]);
    }

    #endregion

    #region RemovePromoCodeFromGuestCartAsync Tests

    [Fact]
    public async Task RemovePromoCodeFromGuestCartAsync_ExistingPromoCode_ReturnsSuccess()
    {
        // Arrange
        var command = new RemovePromoCodeCommand
        {
            GuestCartId = TestGuestCartId
        };

        var cart = CreateTestGuestCart();
        cart.AppliedPromoCodeId = TestPromoCodeId;

        _mockCartRepository.Setup(r => r.GetByGuestCartIdAsync(TestGuestCartId))
            .ReturnsAsync(cart);

        _mockCartRepository.Setup(r => r.UpdateAsync(It.IsAny<CartEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RemovePromoCodeFromGuestCartAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RemovePromoCodeFromGuestCartAsync_EmptyGuestCartId_ReturnsFailure()
    {
        // Arrange
        var command = new RemovePromoCodeCommand
        {
            GuestCartId = string.Empty
        };

        // Act
        var result = await _service.RemovePromoCodeFromGuestCartAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Guest cart ID is required", result.Errors[0]);
    }

    #endregion

    #region CalculateDiscountAsync Tests

    [Fact]
    public async Task CalculateDiscountAsync_WithAppliedPromoCode_ReturnsDiscountInfo()
    {
        // Arrange
        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));
        cart.AppliedPromoCodeId = TestPromoCodeId;

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 15m);

        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(TestPromoCodeId))
            .ReturnsAsync(promoCode);

        // Act
        var result = await _service.CalculateDiscountAsync(cart);

        // Assert
        Assert.Equal(15m, result.DiscountAmount);
        Assert.Equal("SAVE10", result.AppliedPromoCode);
        Assert.Equal(TestPromoCodeId, result.AppliedPromoCodeId);
    }

    [Fact]
    public async Task CalculateDiscountAsync_NoPromoCodeApplied_ReturnsNone()
    {
        // Arrange
        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));
        cart.AppliedPromoCodeId = null;

        // Act
        var result = await _service.CalculateDiscountAsync(cart);

        // Assert
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Null(result.AppliedPromoCode);
    }

    [Fact]
    public async Task CalculateDiscountAsync_InvalidPromoCode_ReturnsNone()
    {
        // Arrange
        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));
        cart.AppliedPromoCodeId = Guid.NewGuid(); // Non-existent promo code

        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PromoCode?)null);

        // Act
        var result = await _service.CalculateDiscountAsync(cart);

        // Assert
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Null(result.AppliedPromoCode);
    }

    [Fact]
    public async Task CalculateDiscountAsync_ExpiredPromoCode_ReturnsNone()
    {
        // Arrange
        var cart = CreateTestCart();
        cart.Items.Add(CreateTestCartItem(100m));
        cart.AppliedPromoCodeId = TestPromoCodeId;

        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.EndDate = DateTimeOffset.UtcNow.AddDays(-1); // Expired

        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(TestPromoCodeId))
            .ReturnsAsync(promoCode);

        // Act
        var result = await _service.CalculateDiscountAsync(cart);

        // Assert
        Assert.Equal(0m, result.DiscountAmount);
    }

    #endregion

    #region PromoCode Entity Tests

    [Fact]
    public void PromoCode_IsValid_ActiveAndWithinDates_ReturnsTrue()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);

        // Act
        var result = promoCode.IsValid(DateTimeOffset.UtcNow);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PromoCode_IsValid_NotActive_ReturnsFalse()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.IsActive = false;

        // Act
        var result = promoCode.IsValid(DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PromoCode_IsValid_BeforeStartDate_ReturnsFalse()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.StartDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var result = promoCode.IsValid(DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PromoCode_IsValid_AfterEndDate_ReturnsFalse()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.EndDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var result = promoCode.IsValid(DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PromoCode_IsValid_UsageLimitReached_ReturnsFalse()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.UsageLimit = 10;
        promoCode.UsageCount = 10;

        // Act
        var result = promoCode.IsValid(DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PromoCode_CalculateDiscount_Percentage_ReturnsCorrectAmount()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 15m);

        // Act
        var result = promoCode.CalculateDiscount(100m);

        // Assert
        Assert.Equal(15m, result);
    }

    [Fact]
    public void PromoCode_CalculateDiscount_FixedAmount_ReturnsCorrectAmount()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.FixedAmount, 25m);

        // Act
        var result = promoCode.CalculateDiscount(100m);

        // Assert
        Assert.Equal(25m, result);
    }

    [Fact]
    public void PromoCode_CalculateDiscount_FixedAmount_DoesNotExceedSubtotal()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.FixedAmount, 50m);

        // Act
        var result = promoCode.CalculateDiscount(30m);

        // Assert
        Assert.Equal(30m, result); // Capped at subtotal
    }

    [Fact]
    public void PromoCode_MeetsMinimumOrderAmount_NoMinimum_ReturnsTrue()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.MinimumOrderAmount = null;

        // Act
        var result = promoCode.MeetsMinimumOrderAmount(10m);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PromoCode_MeetsMinimumOrderAmount_BelowMinimum_ReturnsFalse()
    {
        // Arrange
        var promoCode = CreateTestPromoCode(DiscountType.Percentage, 10m);
        promoCode.MinimumOrderAmount = 50m;

        // Act
        var result = promoCode.MeetsMinimumOrderAmount(25m);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private static CartEntity CreateTestCart()
    {
        return new CartEntity
        {
            Id = TestCartId,
            BuyerId = TestBuyerId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<CartItemEntity>()
        };
    }

    private static CartEntity CreateTestGuestCart()
    {
        return new CartEntity
        {
            Id = TestCartId,
            GuestCartId = TestGuestCartId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<CartItemEntity>()
        };
    }

    private static CartItemEntity CreateTestCartItem(decimal price, Guid? storeId = null)
    {
        return new CartItemEntity
        {
            Id = Guid.NewGuid(),
            CartId = TestCartId,
            ProductId = TestProductId,
            StoreId = storeId ?? TestStoreId,
            Quantity = 1,
            ProductTitle = "Test Product",
            ProductPrice = price,
            StoreName = "Test Store",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static PromoCode CreateTestPromoCode(DiscountType discountType, decimal discountValue)
    {
        return new PromoCode
        {
            Id = TestPromoCodeId,
            Code = "SAVE10",
            Description = "Save 10% on your order",
            DiscountType = discountType,
            DiscountValue = discountValue,
            Scope = PromoCodeScope.Platform,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            EndDate = DateTimeOffset.UtcNow.AddDays(30),
            IsActive = true,
            UsageCount = 0,
            UsageLimit = null,
            MinimumOrderAmount = null,
            MaxDiscountAmount = null,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
