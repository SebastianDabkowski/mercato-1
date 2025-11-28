using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Orders;

public class OrderServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestTransactionId = Guid.NewGuid();
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();

    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IOrderConfirmationEmailService> _mockEmailService;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        _mockEmailService = new Mock<IOrderConfirmationEmailService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<OrderService>>();
        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    #region CreateOrderAsync Tests

    [Fact]
    public async Task CreateOrderAsync_ValidCommand_CreatesOrder()
    {
        // Arrange
        var command = CreateTestOrderCommand();

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.OrderId);
        Assert.NotNull(result.OrderNumber);
        Assert.StartsWith("ORD-", result.OrderNumber);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.BuyerId == TestBuyerId &&
            o.PaymentTransactionId == TestTransactionId &&
            o.Items.Count == 1 &&
            o.Status == OrderStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_CalculatesTotalsCorrectly()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            BuyerId = TestBuyerId,
            PaymentTransactionId = TestTransactionId,
            Items = new List<CreateOrderItem>
            {
                new CreateOrderItem
                {
                    ProductId = TestProductId,
                    StoreId = TestStoreId,
                    ProductTitle = "Product 1",
                    UnitPrice = 10.00m,
                    Quantity = 2,
                    StoreName = "Test Store"
                },
                new CreateOrderItem
                {
                    ProductId = Guid.NewGuid(),
                    StoreId = TestStoreId,
                    ProductTitle = "Product 2",
                    UnitPrice = 15.00m,
                    Quantity = 3,
                    StoreName = "Test Store"
                }
            },
            ShippingTotal = 5.99m,
            DeliveryAddress = CreateTestDeliveryAddress()
        };

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.ItemsSubtotal == 65.00m &&
            o.ShippingTotal == 5.99m &&
            o.TotalAmount == 70.99m)), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.BuyerId = string.Empty;

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateOrderAsync_EmptyTransactionId_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.PaymentTransactionId = Guid.Empty;

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Payment transaction ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateOrderAsync_NoItems_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.Items = new List<CreateOrderItem>();

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order must contain at least one item.", result.Errors);
    }

    [Fact]
    public async Task CreateOrderAsync_IncompleteDeliveryAddress_ReturnsFailure()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.DeliveryAddress = new DeliveryAddressInfo
        {
            FullName = "",
            AddressLine1 = "",
            City = "",
            PostalCode = "",
            Country = ""
        };

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Delivery full name is required.", result.Errors);
        Assert.Contains("Delivery address line 1 is required.", result.Errors);
        Assert.Contains("Delivery city is required.", result.Errors);
        Assert.Contains("Delivery postal code is required.", result.Errors);
        Assert.Contains("Delivery country is required.", result.Errors);
    }

    #endregion

    #region GetOrderAsync Tests

    [Fact]
    public async Task GetOrderAsync_ValidBuyerOwnsOrder_ReturnsOrder()
    {
        // Arrange
        var order = CreateTestOrder();

        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderAsync(TestOrderId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Order);
        Assert.Equal(TestOrderId, result.Order.Id);
    }

    [Fact]
    public async Task GetOrderAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderAsync(TestOrderId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order not found.", result.Errors);
    }

    [Fact]
    public async Task GetOrderAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var order = CreateTestOrder();
        order.BuyerId = "other-buyer";

        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderAsync(TestOrderId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetOrderAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetOrderAsync(TestOrderId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    #endregion

    #region GetOrderByTransactionAsync Tests

    [Fact]
    public async Task GetOrderByTransactionAsync_ValidTransaction_ReturnsOrder()
    {
        // Arrange
        var order = CreateTestOrder();

        _mockOrderRepository.Setup(r => r.GetByPaymentTransactionIdAsync(TestTransactionId))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderByTransactionAsync(TestTransactionId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Order);
        Assert.Equal(TestOrderId, result.Order.Id);
    }

    [Fact]
    public async Task GetOrderByTransactionAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByPaymentTransactionIdAsync(TestTransactionId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderByTransactionAsync(TestTransactionId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order not found.", result.Errors);
    }

    [Fact]
    public async Task GetOrderByTransactionAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var order = CreateTestOrder();
        order.BuyerId = "other-buyer";

        _mockOrderRepository.Setup(r => r.GetByPaymentTransactionIdAsync(TestTransactionId))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderByTransactionAsync(TestTransactionId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    #endregion

    #region UpdateOrderStatusAsync Tests

    [Fact]
    public async Task UpdateOrderStatusAsync_PaymentSuccessful_ConfirmsOrder()
    {
        // Arrange
        var order = CreateTestOrder();

        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync(order);

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateOrderStatusAsync(TestOrderId, isPaymentSuccessful: true);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Confirmed &&
            o.ConfirmedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_PaymentFailed_CancelsOrder()
    {
        // Arrange
        var order = CreateTestOrder();

        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync(order);

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateOrderStatusAsync(TestOrderId, isPaymentSuccessful: false);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Cancelled &&
            o.CancelledAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.UpdateOrderStatusAsync(TestOrderId, isPaymentSuccessful: true);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order not found.", result.Errors);
    }

    #endregion

    #region GetOrdersForBuyerAsync Tests

    [Fact]
    public async Task GetOrdersForBuyerAsync_ValidBuyer_ReturnsOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        _mockOrderRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.GetOrdersForBuyerAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Orders);
        Assert.Equal(TestOrderId, result.Orders[0].Id);
    }

    [Fact]
    public async Task GetOrdersForBuyerAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetOrdersForBuyerAsync(string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetOrdersForBuyerAsync_NoOrders_ReturnsEmptyList()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _service.GetOrdersForBuyerAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Orders);
    }

    #endregion

    #region SendOrderConfirmationEmailAsync Tests

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_ValidOrder_SendsEmail()
    {
        // Arrange
        var order = CreateTestOrder();
        var buyerEmail = "test@example.com";

        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync(order);

        _mockEmailService.Setup(e => e.SendOrderConfirmationAsync(order, buyerEmail))
            .ReturnsAsync(SendEmailResult.Success());

        // Act
        var result = await _service.SendOrderConfirmationEmailAsync(TestOrderId, buyerEmail);

        // Assert
        Assert.True(result.Succeeded);
        _mockEmailService.Verify(e => e.SendOrderConfirmationAsync(order, buyerEmail), Times.Once);
    }

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_EmptyEmail_ReturnsFailure()
    {
        // Act
        var result = await _service.SendOrderConfirmationEmailAsync(TestOrderId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer email is required.", result.Errors);
    }

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.SendOrderConfirmationEmailAsync(TestOrderId, "test@example.com");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order not found.", result.Errors);
    }

    #endregion

    #region Helper Methods

    private static CreateOrderCommand CreateTestOrderCommand()
    {
        return new CreateOrderCommand
        {
            BuyerId = TestBuyerId,
            PaymentTransactionId = TestTransactionId,
            Items = new List<CreateOrderItem>
            {
                new CreateOrderItem
                {
                    ProductId = TestProductId,
                    StoreId = TestStoreId,
                    ProductTitle = "Test Product",
                    UnitPrice = 29.99m,
                    Quantity = 2,
                    StoreName = "Test Store"
                }
            },
            ShippingTotal = 5.99m,
            DeliveryAddress = CreateTestDeliveryAddress()
        };
    }

    private static DeliveryAddressInfo CreateTestDeliveryAddress()
    {
        return new DeliveryAddressInfo
        {
            FullName = "Test Buyer",
            AddressLine1 = "123 Test St",
            City = "Test City",
            State = "TS",
            PostalCode = "12345",
            Country = "US",
            PhoneNumber = "+1234567890"
        };
    }

    private static Order CreateTestOrder()
    {
        return new Order
        {
            Id = TestOrderId,
            BuyerId = TestBuyerId,
            OrderNumber = "ORD-12345678",
            Status = OrderStatus.Pending,
            PaymentTransactionId = TestTransactionId,
            ItemsSubtotal = 59.98m,
            ShippingTotal = 5.99m,
            TotalAmount = 65.97m,
            DeliveryFullName = "Test Buyer",
            DeliveryAddressLine1 = "123 Test St",
            DeliveryCity = "Test City",
            DeliveryState = "TS",
            DeliveryPostalCode = "12345",
            DeliveryCountry = "US",
            DeliveryPhoneNumber = "+1234567890",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = new List<OrderItem>()
        };
    }

    #endregion
}
