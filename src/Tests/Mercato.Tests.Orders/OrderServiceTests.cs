using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly Mock<ISellerSubOrderRepository> _mockSellerSubOrderRepository;
    private readonly Mock<IReturnRequestRepository> _mockReturnRequestRepository;
    private readonly Mock<IShippingStatusHistoryRepository> _mockShippingStatusHistoryRepository;
    private readonly Mock<IOrderConfirmationEmailService> _mockEmailService;
    private readonly Mock<IShippingNotificationService> _mockShippingNotificationService;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        _mockSellerSubOrderRepository = new Mock<ISellerSubOrderRepository>(MockBehavior.Strict);
        _mockReturnRequestRepository = new Mock<IReturnRequestRepository>(MockBehavior.Strict);
        _mockShippingStatusHistoryRepository = new Mock<IShippingStatusHistoryRepository>(MockBehavior.Strict);
        _mockEmailService = new Mock<IOrderConfirmationEmailService>(MockBehavior.Strict);
        _mockShippingNotificationService = new Mock<IShippingNotificationService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<OrderService>>();
        var returnSettings = Options.Create(new ReturnSettings { ReturnWindowDays = 30 });
        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockSellerSubOrderRepository.Object,
            _mockReturnRequestRepository.Object,
            _mockShippingStatusHistoryRepository.Object,
            _mockEmailService.Object,
            _mockShippingNotificationService.Object,
            returnSettings,
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
            o.Status == OrderStatus.New)), Times.Once);
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
    public async Task UpdateOrderStatusAsync_PaymentSuccessful_SetsOrderToPaid()
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
            o.Status == OrderStatus.Paid &&
            o.ConfirmedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_PaymentFailed_FailsOrder()
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
            o.Status == OrderStatus.Failed &&
            o.FailedAt != null)), Times.Once);
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

    [Fact]
    public async Task UpdateOrderStatusAsync_OrderNotInNewStatus_ReturnsFailure()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Status = OrderStatus.Paid; // Order already paid

        _mockOrderRepository.Setup(r => r.GetByIdAsync(TestOrderId))
            .ReturnsAsync(order);

        // Act
        var result = await _service.UpdateOrderStatusAsync(TestOrderId, isPaymentSuccessful: true);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot process payment for order in status 'Paid'. Order must be in 'New' status.", result.Errors);
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

    #region GetFilteredOrdersForBuyerAsync Tests

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_ValidQuery_ReturnsFilteredOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 1,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, null, null, null, null, 1, 10))
            .ReturnsAsync((orders, 1));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Orders);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_WithStatusFilter_ReturnsMatchingOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var statuses = new List<OrderStatus> { OrderStatus.New, OrderStatus.Paid };
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Statuses = statuses,
            Page = 1,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, statuses, null, null, null, 1, 10))
            .ReturnsAsync((orders, 1));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Orders);
        _mockOrderRepository.Verify(r => r.GetFilteredByBuyerIdAsync(
            TestBuyerId, statuses, null, null, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_WithDateRange_ReturnsMatchingOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 1,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, null, fromDate, toDate, null, 1, 10))
            .ReturnsAsync((orders, 1));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.GetFilteredByBuyerIdAsync(
            TestBuyerId, null, fromDate, toDate, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_WithStoreId_ReturnsMatchingOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, null, null, null, TestStoreId, 1, 10))
            .ReturnsAsync((orders, 1));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.GetFilteredByBuyerIdAsync(
            TestBuyerId, null, null, null, TestStoreId, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = string.Empty,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_InvalidPage_ReturnsFailure()
    {
        // Arrange
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 0,
            PageSize = 10
        };

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page number must be at least 1.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_InvalidPageSize_ReturnsFailure()
    {
        // Arrange
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 1,
            PageSize = 0
        };

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_PageSizeTooLarge_ReturnsFailure()
    {
        // Arrange
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 1,
            PageSize = 101
        };

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 100.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_FromDateAfterToDate_ReturnsFailure()
    {
        // Arrange
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            FromDate = DateTimeOffset.UtcNow,
            ToDate = DateTimeOffset.UtcNow.AddDays(-7),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 1,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, null, null, null, null, 1, 10))
            .ReturnsAsync((new List<Order>(), 0));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Orders);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_Pagination_ReturnsCorrectTotalPages()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 1,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, null, null, null, null, 1, 10))
            .ReturnsAsync((orders, 25));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetFilteredOrdersForBuyerAsync_MiddlePage_HasPreviousAndNextPages()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var query = new BuyerOrderFilterQuery
        {
            BuyerId = TestBuyerId,
            Page = 2,
            PageSize = 10
        };

        _mockOrderRepository.Setup(r => r.GetFilteredByBuyerIdAsync(
                TestBuyerId, null, null, null, null, 2, 10))
            .ReturnsAsync((orders, 25));

        // Act
        var result = await _service.GetFilteredOrdersForBuyerAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Page);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    #endregion

    #region GetDistinctSellersForBuyerAsync Tests

    [Fact]
    public async Task GetDistinctSellersForBuyerAsync_ValidBuyer_ReturnsSellers()
    {
        // Arrange
        var sellers = new List<(Guid StoreId, string StoreName)>
        {
            (TestStoreId, "Test Store"),
            (Guid.NewGuid(), "Another Store")
        };

        _mockOrderRepository.Setup(r => r.GetDistinctSellersByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(sellers);

        // Act
        var result = await _service.GetDistinctSellersForBuyerAsync(TestBuyerId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.StoreName == "Test Store");
    }

    [Fact]
    public async Task GetDistinctSellersForBuyerAsync_EmptyBuyerId_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetDistinctSellersForBuyerAsync(string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDistinctSellersForBuyerAsync_NoSellers_ReturnsEmptyList()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetDistinctSellersByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<(Guid StoreId, string StoreName)>());

        // Act
        var result = await _service.GetDistinctSellersForBuyerAsync(TestBuyerId);

        // Assert
        Assert.Empty(result);
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
            Status = OrderStatus.New,
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
            Items = [],
            SellerSubOrders = []
        };
    }

    private static SellerSubOrder CreateTestSellerSubOrder()
    {
        return new SellerSubOrder
        {
            Id = Guid.NewGuid(),
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            StoreName = "Test Store",
            SubOrderNumber = "ORD-12345678-S1",
            Status = SellerSubOrderStatus.Paid,
            ItemsSubtotal = 59.98m,
            ShippingCost = 5.99m,
            TotalAmount = 65.97m,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = []
        };
    }

    #endregion

    #region CreateOrderAsync with Seller Sub-Orders Tests

    [Fact]
    public async Task CreateOrderAsync_WithPaymentMethodName_SetsPaymentMethodName()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.PaymentMethodName = "Credit Card";

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.PaymentMethodName == "Credit Card")), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithoutPaymentMethodName_PaymentMethodNameIsNull()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.PaymentMethodName = null;

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.PaymentMethodName == null)), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_SingleSeller_CreatesOneSubOrder()
    {
        // Arrange
        var command = CreateTestOrderCommand();

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.SellerSubOrders.Count == 1 &&
            o.SellerSubOrders.First().StoreId == TestStoreId &&
            o.SellerSubOrders.First().Status == SellerSubOrderStatus.New)), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_MultipleSellers_CreatesMultipleSubOrders()
    {
        // Arrange
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            BuyerId = TestBuyerId,
            PaymentTransactionId = TestTransactionId,
            Items = new List<CreateOrderItem>
            {
                new CreateOrderItem
                {
                    ProductId = Guid.NewGuid(),
                    StoreId = storeId1,
                    ProductTitle = "Product from Store 1",
                    UnitPrice = 20.00m,
                    Quantity = 1,
                    StoreName = "Store 1"
                },
                new CreateOrderItem
                {
                    ProductId = Guid.NewGuid(),
                    StoreId = storeId2,
                    ProductTitle = "Product from Store 2",
                    UnitPrice = 30.00m,
                    Quantity = 2,
                    StoreName = "Store 2"
                }
            },
            ShippingTotal = 10.00m,
            DeliveryAddress = CreateTestDeliveryAddress()
        };

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.SellerSubOrders.Count == 2 &&
            o.SellerSubOrders.Any(s => s.StoreId == storeId1 && s.ItemsSubtotal == 20.00m) &&
            o.SellerSubOrders.Any(s => s.StoreId == storeId2 && s.ItemsSubtotal == 60.00m))), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_SubOrderNumbers_AreGeneratedCorrectly()
    {
        // Arrange
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            BuyerId = TestBuyerId,
            PaymentTransactionId = TestTransactionId,
            Items = new List<CreateOrderItem>
            {
                new CreateOrderItem
                {
                    ProductId = Guid.NewGuid(),
                    StoreId = storeId1,
                    ProductTitle = "Product 1",
                    UnitPrice = 20.00m,
                    Quantity = 1,
                    StoreName = "Store 1"
                },
                new CreateOrderItem
                {
                    ProductId = Guid.NewGuid(),
                    StoreId = storeId2,
                    ProductTitle = "Product 2",
                    UnitPrice = 30.00m,
                    Quantity = 1,
                    StoreName = "Store 2"
                }
            },
            ShippingTotal = 10.00m,
            DeliveryAddress = CreateTestDeliveryAddress()
        };

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.SellerSubOrders.All(s => s.SubOrderNumber.StartsWith(o.OrderNumber) && s.SubOrderNumber.Contains("-S")))), Times.Once);
    }

    #endregion

    #region GetSellerSubOrdersAsync Tests

    [Fact]
    public async Task GetSellerSubOrdersAsync_ValidStoreId_ReturnsSubOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder() };
        _mockSellerSubOrderRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(subOrders);

        // Act
        var result = await _service.GetSellerSubOrdersAsync(TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.SellerSubOrders);
    }

    [Fact]
    public async Task GetSellerSubOrdersAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetSellerSubOrdersAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region GetSellerSubOrderAsync Tests

    [Fact]
    public async Task GetSellerSubOrderAsync_ValidSubOrder_ReturnsSubOrder()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.GetSellerSubOrderAsync(subOrder.Id, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.SellerSubOrder);
    }

    [Fact]
    public async Task GetSellerSubOrderAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.GetSellerSubOrderAsync(subOrderId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    [Fact]
    public async Task GetSellerSubOrderAsync_DifferentStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.StoreId = Guid.NewGuid(); // Different store

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.GetSellerSubOrderAsync(subOrder.Id, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    #endregion

    #region UpdateSellerSubOrderStatusAsync Tests

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Paid;

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Preparing
        };

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.UpdateAsync(It.Is<SellerSubOrder>(s =>
            s.Status == SellerSubOrderStatus.Preparing)), Times.Once);
    }

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_InvalidTransition_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Delivered; // Cannot transition to Preparing from Delivered

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Preparing // Invalid transition
        };

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot transition from Delivered to Preparing.", result.Errors);
    }

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_ShippedWithTracking_SetsTrackingInfo()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Preparing;

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Shipped,
            TrackingNumber = "1Z999AA10123456784",
            ShippingCarrier = "UPS"
        };

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.UpdateAsync(It.Is<SellerSubOrder>(s =>
            s.Status == SellerSubOrderStatus.Shipped &&
            s.TrackingNumber == "1Z999AA10123456784" &&
            s.ShippingCarrier == "UPS" &&
            s.ShippedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_DifferentStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.StoreId = Guid.NewGuid(); // Different store

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Preparing
        };

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_Refunded_SetsRefundedAt()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Delivered;

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        // Mock order repository for parent order update
        _mockOrderRepository.Setup(r => r.GetByIdAsync(subOrder.OrderId))
            .ReturnsAsync((Order?)null); // No parent order, just testing sub-order refund

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Refunded
        };

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.UpdateAsync(It.Is<SellerSubOrder>(s =>
            s.Status == SellerSubOrderStatus.Refunded &&
            s.RefundedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_Refunded_UpdatesParentOrderWhenAllSubOrdersRefunded()
    {
        // Arrange
        var parentOrder = CreateTestOrder();
        parentOrder.Status = OrderStatus.Delivered;

        var subOrder1 = CreateTestSellerSubOrder();
        subOrder1.OrderId = parentOrder.Id;
        subOrder1.Status = SellerSubOrderStatus.Refunded;

        var subOrder2 = CreateTestSellerSubOrder();
        subOrder2.OrderId = parentOrder.Id;
        subOrder2.Status = SellerSubOrderStatus.Delivered;

        parentOrder.SellerSubOrders = [subOrder1, subOrder2];

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder2.Id))
            .ReturnsAsync(subOrder2);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.GetByIdAsync(parentOrder.Id))
            .ReturnsAsync(parentOrder);
        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Refunded
        };

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder2.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Refunded &&
            o.RefundedAt != null)), Times.Once);
    }

    #endregion

    #region GetFilteredSellerSubOrdersAsync Tests

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_ValidQuery_ReturnsFilteredSubOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder() };
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.SubOrders);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_WithStatusFilter_ReturnsMatchingSubOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder() };
        var statuses = new List<SellerSubOrderStatus> { SellerSubOrderStatus.Paid, SellerSubOrderStatus.Preparing };
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            Statuses = statuses,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, statuses, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.SubOrders);
        _mockSellerSubOrderRepository.Verify(r => r.GetFilteredByStoreIdAsync(
            TestStoreId, statuses, null, null, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_WithDateRange_ReturnsMatchingSubOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder() };
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, fromDate, toDate, null, 1, 10))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.GetFilteredByStoreIdAsync(
            TestStoreId, null, fromDate, toDate, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_WithBuyerSearchTerm_ReturnsMatchingSubOrders()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder() };
        var searchTerm = "buyer@test.com";
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            BuyerSearchTerm = searchTerm,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, searchTerm, 1, 10))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.GetFilteredByStoreIdAsync(
            TestStoreId, null, null, null, searchTerm, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = Guid.Empty,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_InvalidPage_ReturnsFailure()
    {
        // Arrange
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            Page = 0,
            PageSize = 10
        };

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page number must be at least 1.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_InvalidPageSize_ReturnsFailure()
    {
        // Arrange
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 0
        };

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Page size must be between 1 and 10000.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_FromDateAfterToDate_ReturnsFailure()
    {
        // Arrange
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            FromDate = DateTimeOffset.UtcNow,
            ToDate = DateTimeOffset.UtcNow.AddDays(-7),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("From date cannot be after to date.", result.Errors);
    }

    [Fact]
    public async Task GetFilteredSellerSubOrdersAsync_Pagination_ReturnsCorrectTotalPages()
    {
        // Arrange
        var subOrders = new List<SellerSubOrder> { CreateTestSellerSubOrder() };
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            Page = 1,
            PageSize = 10
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10))
            .ReturnsAsync((subOrders, 25));

        // Act
        var result = await _service.GetFilteredSellerSubOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    #endregion

    #region GetDistinctBuyersForStoreAsync Tests

    [Fact]
    public async Task GetDistinctBuyersForStoreAsync_ValidStoreId_ReturnsBuyers()
    {
        // Arrange
        var buyers = new List<(string BuyerId, string BuyerEmail)>
        {
            ("buyer-1", "buyer1@test.com"),
            ("buyer-2", "buyer2@test.com")
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetDistinctBuyersByStoreIdAsync(TestStoreId))
            .ReturnsAsync(buyers);

        // Act
        var result = await _service.GetDistinctBuyersForStoreAsync(TestStoreId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.BuyerId == "buyer-1");
    }

    [Fact]
    public async Task GetDistinctBuyersForStoreAsync_EmptyStoreId_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetDistinctBuyersForStoreAsync(Guid.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDistinctBuyersForStoreAsync_NoBuyers_ReturnsEmptyList()
    {
        // Arrange
        _mockSellerSubOrderRepository.Setup(r => r.GetDistinctBuyersByStoreIdAsync(TestStoreId))
            .ReturnsAsync(new List<(string BuyerId, string BuyerEmail)>());

        // Act
        var result = await _service.GetDistinctBuyersForStoreAsync(TestStoreId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ExportSellerSubOrdersToCsvAsync Tests

    [Fact]
    public async Task ExportSellerSubOrdersToCsvAsync_ValidQuery_ReturnsCsvBytes()
    {
        // Arrange
        var order = CreateTestOrder();
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Order = order;
        subOrder.Items = new List<SellerSubOrderItem>
        {
            new SellerSubOrderItem { Id = Guid.NewGuid(), ProductTitle = "Test Product", Quantity = 2, UnitPrice = 10m }
        };
        var subOrders = new List<SellerSubOrder> { subOrder };
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.ExportSellerSubOrdersToCsvAsync(TestStoreId, query);

        // Assert
        Assert.NotEmpty(result);
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Sub-Order Number", csvContent);
        Assert.Contains("Buyer Name", csvContent);
        Assert.Contains("Delivery Address Line 1", csvContent);
        Assert.Contains("Phone", csvContent);
        Assert.Contains("Shipping Method", csvContent);
        Assert.Contains("Tracking Number", csvContent);
        Assert.Contains("Items", csvContent);
        Assert.Contains(subOrder.SubOrderNumber, csvContent);
    }

    [Fact]
    public async Task ExportSellerSubOrdersToCsvAsync_EmptyStoreId_ReturnsEmptyArray()
    {
        // Arrange
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = Guid.Empty
        };

        // Act
        var result = await _service.ExportSellerSubOrdersToCsvAsync(Guid.Empty, query);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportSellerSubOrdersToCsvAsync_NoOrders_ReturnsEmptyArray()
    {
        // Arrange
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((new List<SellerSubOrder>(), 0));

        // Act
        var result = await _service.ExportSellerSubOrdersToCsvAsync(TestStoreId, query);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportSellerSubOrdersToCsvAsync_WithFilters_AppliesFilters()
    {
        // Arrange
        var order = CreateTestOrder();
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Order = order;
        subOrder.Items = new List<SellerSubOrderItem>
        {
            new SellerSubOrderItem { Id = Guid.NewGuid(), ProductTitle = "Product 1", Quantity = 1, UnitPrice = 10m }
        };
        var subOrders = new List<SellerSubOrder> { subOrder };
        var statuses = new List<SellerSubOrderStatus> { SellerSubOrderStatus.Paid };
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var query = new SellerSubOrderFilterQuery
        {
            StoreId = TestStoreId,
            Statuses = statuses,
            FromDate = fromDate,
            ToDate = toDate,
            BuyerSearchTerm = "test"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, statuses, fromDate, toDate, "test", 1, 10000))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.ExportSellerSubOrdersToCsvAsync(TestStoreId, query);

        // Assert
        Assert.NotEmpty(result);
        _mockSellerSubOrderRepository.Verify(r => r.GetFilteredByStoreIdAsync(
            TestStoreId, statuses, fromDate, toDate, "test", 1, 10000), Times.Once);
    }

    [Fact]
    public async Task ExportSellerSubOrdersToCsvAsync_IncludesShippingFields()
    {
        // Arrange
        var order = CreateTestOrder();
        order.DeliveryPhoneNumber = "+1-555-123-4567";
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Order = order;
        subOrder.ShippingMethodName = "Express Delivery";
        subOrder.TrackingNumber = "TRACK123";
        subOrder.ShippingCarrier = "FedEx";
        subOrder.Items = new List<SellerSubOrderItem>
        {
            new SellerSubOrderItem { Id = Guid.NewGuid(), ProductTitle = "Widget", Quantity = 3, UnitPrice = 25m },
            new SellerSubOrderItem { Id = Guid.NewGuid(), ProductTitle = "Gadget", Quantity = 1, UnitPrice = 50m }
        };
        var subOrders = new List<SellerSubOrder> { subOrder };
        var query = new SellerSubOrderFilterQuery { StoreId = TestStoreId };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.ExportSellerSubOrdersToCsvAsync(TestStoreId, query);

        // Assert
        Assert.NotEmpty(result);
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Test Buyer", csvContent); // DeliveryFullName
        Assert.Contains("123 Test St", csvContent); // DeliveryAddressLine1
        Assert.Contains("Test City", csvContent); // DeliveryCity
        Assert.Contains("+1-555-123-4567", csvContent); // Phone
        Assert.Contains("Express Delivery", csvContent); // ShippingMethodName
        Assert.Contains("TRACK123", csvContent); // TrackingNumber
        Assert.Contains("FedEx", csvContent); // ShippingCarrier
        Assert.Contains("Widget x3", csvContent); // Items summary
        Assert.Contains("Gadget x1", csvContent); // Items summary
    }

    [Fact]
    public async Task ExportSellerSubOrdersToCsvAsync_HandlesSpecialCharactersInFields()
    {
        // Arrange
        var order = CreateTestOrder();
        order.DeliveryFullName = "John \"Johnny\" Doe";
        order.DeliveryAddressLine1 = "123, Main St, Suite #5";
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Order = order;
        subOrder.Items = new List<SellerSubOrderItem>
        {
            new SellerSubOrderItem { Id = Guid.NewGuid(), ProductTitle = "Product, with comma", Quantity = 1, UnitPrice = 10m }
        };
        var subOrders = new List<SellerSubOrder> { subOrder };
        var query = new SellerSubOrderFilterQuery { StoreId = TestStoreId };

        _mockSellerSubOrderRepository.Setup(r => r.GetFilteredByStoreIdAsync(
                TestStoreId, null, null, null, null, 1, 10000))
            .ReturnsAsync((subOrders, 1));

        // Act
        var result = await _service.ExportSellerSubOrdersToCsvAsync(TestStoreId, query);

        // Assert
        Assert.NotEmpty(result);
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        // Verify CSV escaping of special characters (quotes and commas)
        Assert.Contains("\"John \"\"Johnny\"\" Doe\"", csvContent); // Escaped quotes
        Assert.Contains("\"123, Main St, Suite #5\"", csvContent); // Escaped comma
        Assert.Contains("\"Product, with comma x1\"", csvContent); // Escaped comma in items
    }

    #endregion

    #region CreateOrderAsync with Buyer Email and Delivery Instructions Tests

    [Fact]
    public async Task CreateOrderAsync_WithBuyerEmail_SetsBuyerEmail()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.BuyerEmail = "buyer@example.com";

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.BuyerEmail == "buyer@example.com")), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithDeliveryInstructions_SetsDeliveryInstructions()
    {
        // Arrange
        var command = CreateTestOrderCommand();
        command.DeliveryAddress.DeliveryInstructions = "Leave at the front door";

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.DeliveryInstructions == "Leave at the front door")), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithShippingMethodName_SetsShippingMethodOnSubOrder()
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
                    ProductTitle = "Test Product",
                    UnitPrice = 29.99m,
                    Quantity = 2,
                    StoreName = "Test Store",
                    ShippingMethodName = "Express Shipping"
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
            o.SellerSubOrders.Count == 1 &&
            o.SellerSubOrders.First().ShippingMethodName == "Express Shipping")), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithAllNewFields_SetsAllFieldsCorrectly()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            BuyerId = TestBuyerId,
            BuyerEmail = "buyer@example.com",
            PaymentTransactionId = TestTransactionId,
            PaymentMethodName = "Credit Card",
            Items = new List<CreateOrderItem>
            {
                new CreateOrderItem
                {
                    ProductId = TestProductId,
                    StoreId = TestStoreId,
                    ProductTitle = "Test Product",
                    UnitPrice = 29.99m,
                    Quantity = 2,
                    StoreName = "Test Store",
                    ShippingMethodName = "Standard Shipping"
                }
            },
            ShippingTotal = 5.99m,
            DeliveryAddress = new DeliveryAddressInfo
            {
                FullName = "Test Buyer",
                AddressLine1 = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "US",
                PhoneNumber = "+1234567890",
                DeliveryInstructions = "Ring the doorbell"
            }
        };

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _service.CreateOrderAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.BuyerEmail == "buyer@example.com" &&
            o.DeliveryInstructions == "Ring the doorbell" &&
            o.SellerSubOrders.First().ShippingMethodName == "Standard Shipping")), Times.Once);
    }

    #endregion

    #region UpdateTrackingInfoAsync Tests

    [Fact]
    public async Task UpdateTrackingInfoAsync_ValidShippedOrder_UpdatesTrackingInfo()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Shipped;
        subOrder.TrackingNumber = "OLD123";
        subOrder.ShippingCarrier = "OldCarrier";

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.UpdateAsync(It.Is<SellerSubOrder>(s =>
            s.TrackingNumber == "NEW456" &&
            s.ShippingCarrier == "NewCarrier" &&
            s.Status == SellerSubOrderStatus.Shipped)), Times.Once);
    }

    [Fact]
    public async Task UpdateTrackingInfoAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    [Fact]
    public async Task UpdateTrackingInfoAsync_DifferentStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Shipped;
        subOrder.StoreId = Guid.NewGuid(); // Different store

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task UpdateTrackingInfoAsync_NotShippedOrder_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Preparing; // Not shipped

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Tracking information can only be updated for shipped orders.", result.Errors);
    }

    [Fact]
    public async Task UpdateTrackingInfoAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(Guid.NewGuid(), Guid.Empty, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task UpdateTrackingInfoAsync_ClearsTrackingInfo_UpdatesSuccessfully()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Shipped;
        subOrder.TrackingNumber = "OLD123";
        subOrder.ShippingCarrier = "OldCarrier";

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = null,
            ShippingCarrier = null
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.UpdateAsync(It.Is<SellerSubOrder>(s =>
            s.TrackingNumber == null &&
            s.ShippingCarrier == null)), Times.Once);
    }

    [Theory]
    [InlineData(SellerSubOrderStatus.New)]
    [InlineData(SellerSubOrderStatus.Paid)]
    [InlineData(SellerSubOrderStatus.Preparing)]
    [InlineData(SellerSubOrderStatus.Delivered)]
    [InlineData(SellerSubOrderStatus.Cancelled)]
    [InlineData(SellerSubOrderStatus.Refunded)]
    public async Task UpdateTrackingInfoAsync_NonShippedStatus_ReturnsFailure(SellerSubOrderStatus status)
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = status;

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Tracking information can only be updated for shipped orders.", result.Errors);
    }

    [Fact]
    public async Task UpdateTrackingInfoAsync_UpdatesLastUpdatedAt()
    {
        // Arrange
        var subOrder = CreateTestSellerSubOrder();
        subOrder.Status = SellerSubOrderStatus.Shipped;
        var originalLastUpdatedAt = subOrder.LastUpdatedAt;

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateTrackingInfoCommand
        {
            TrackingNumber = "NEW456",
            ShippingCarrier = "NewCarrier"
        };

        // Act
        var result = await _service.UpdateTrackingInfoAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockSellerSubOrderRepository.Verify(r => r.UpdateAsync(It.Is<SellerSubOrder>(s =>
            s.LastUpdatedAt >= originalLastUpdatedAt)), Times.Once);
    }

    #endregion

    #region Partial Fulfillment Tests

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_ValidCommand_UpdatesItemStatuses()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Paid,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = itemId1, ProductTitle = "Product 1", UnitPrice = 10, Quantity = 1, Status = SellerSubOrderItemStatus.New },
                new() { Id = itemId2, ProductTitle = "Product 2", UnitPrice = 20, Quantity = 2, Status = SellerSubOrderItemStatus.New }
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = itemId1, NewStatus = SellerSubOrderItemStatus.Shipped },
                new ItemStatusUpdate { ItemId = itemId2, NewStatus = SellerSubOrderItemStatus.Cancelled }
            ],
            TrackingNumber = "TRACK123",
            ShippingCarrier = "FedEx"
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.SubOrder);
        var updatedSubOrder = result.SubOrder;
        
        var item1 = updatedSubOrder.Items.First(i => i.Id == itemId1);
        Assert.Equal(SellerSubOrderItemStatus.Shipped, item1.Status);
        Assert.NotNull(item1.ShippedAt);
        
        var item2 = updatedSubOrder.Items.First(i => i.Id == itemId2);
        Assert.Equal(SellerSubOrderItemStatus.Cancelled, item2.Status);
        Assert.NotNull(item2.CancelledAt);
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var differentStoreId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId, // Different from request
            Status = SellerSubOrderStatus.Paid,
            Items = new List<SellerSubOrderItem>()
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = Guid.NewGuid(), NewStatus = SellerSubOrderItemStatus.Shipped }
            ]
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, differentStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_InvalidSubOrderStatus_ReturnsFailure()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Delivered, // Cannot update items when delivered
            Items = new List<SellerSubOrderItem>()
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = Guid.NewGuid(), NewStatus = SellerSubOrderItemStatus.Shipped }
            ]
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Paid or Preparing status"));
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var unknownItemId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Paid,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductTitle = "Product 1", Status = SellerSubOrderItemStatus.New }
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = unknownItemId, NewStatus = SellerSubOrderItemStatus.Shipped }
            ]
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found in sub-order"));
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_InvalidStatusTransition_ReturnsFailure()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Paid,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = itemId, ProductTitle = "Product 1", Status = SellerSubOrderItemStatus.Cancelled }
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = itemId, NewStatus = SellerSubOrderItemStatus.Shipped } // Invalid - can't ship cancelled item
            ]
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Cannot transition"));
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_AllItemsShipped_UpdatesSubOrderToShipped()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Paid,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = itemId, ProductTitle = "Product 1", Status = SellerSubOrderItemStatus.New, UnitPrice = 10, Quantity = 1 }
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = itemId, NewStatus = SellerSubOrderItemStatus.Shipped }
            ],
            TrackingNumber = "TRACK123",
            ShippingCarrier = "UPS"
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(SellerSubOrderStatus.Shipped, result.SubOrder!.Status);
        Assert.Equal("TRACK123", result.SubOrder.TrackingNumber);
        Assert.Equal("UPS", result.SubOrder.ShippingCarrier);
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_AllItemsCancelled_UpdatesSubOrderToCancelled()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Paid,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = itemId, ProductTitle = "Product 1", Status = SellerSubOrderItemStatus.New, UnitPrice = 10, Quantity = 1 }
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates =
            [
                new ItemStatusUpdate { ItemId = itemId, NewStatus = SellerSubOrderItemStatus.Cancelled }
            ]
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(subOrderId, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(SellerSubOrderStatus.Cancelled, result.SubOrder!.Status);
        Assert.NotNull(result.SubOrder.CancelledAt);
    }

    [Fact]
    public async Task UpdateSubOrderItemStatusAsync_EmptyItemUpdates_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateSubOrderItemStatusCommand
        {
            ItemUpdates = []
        };

        // Act
        var result = await _service.UpdateSubOrderItemStatusAsync(Guid.NewGuid(), TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("At least one item update is required"));
    }

    [Fact]
    public async Task CalculateCancelledItemsRefundAsync_WithCancelledItems_ReturnsCorrectRefund()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var cancelledAt = DateTimeOffset.UtcNow;
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Preparing,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductTitle = "Product 1", UnitPrice = 10, Quantity = 2, Status = SellerSubOrderItemStatus.Cancelled, CancelledAt = cancelledAt },
                new() { Id = Guid.NewGuid(), ProductTitle = "Product 2", UnitPrice = 25, Quantity = 1, Status = SellerSubOrderItemStatus.Cancelled, CancelledAt = cancelledAt },
                new() { Id = Guid.NewGuid(), ProductTitle = "Product 3", UnitPrice = 15, Quantity = 3, Status = SellerSubOrderItemStatus.Shipped } // Not cancelled
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CalculateCancelledItemsRefundAsync(subOrderId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(45m, result.RefundAmount); // (10 * 2) + (25 * 1) = 45
        Assert.Equal(2, result.CancelledItems.Count);
    }

    [Fact]
    public async Task CalculateCancelledItemsRefundAsync_NoCancelledItems_ReturnsZeroRefund()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            Status = SellerSubOrderStatus.Shipped,
            Items = new List<SellerSubOrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductTitle = "Product 1", UnitPrice = 10, Quantity = 2, Status = SellerSubOrderItemStatus.Shipped },
                new() { Id = Guid.NewGuid(), ProductTitle = "Product 2", UnitPrice = 25, Quantity = 1, Status = SellerSubOrderItemStatus.Delivered }
            }
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CalculateCancelledItemsRefundAsync(subOrderId, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0m, result.RefundAmount);
        Assert.Empty(result.CancelledItems);
    }

    [Fact]
    public async Task CalculateCancelledItemsRefundAsync_UnauthorizedStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var differentStoreId = Guid.NewGuid();
        var subOrder = new SellerSubOrder
        {
            Id = subOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId, // Different from request
            Items = new List<SellerSubOrderItem>()
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CalculateCancelledItemsRefundAsync(subOrderId, differentStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CalculateCancelledItemsRefundAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.CalculateCancelledItemsRefundAsync(subOrderId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Sub-order not found"));
    }

    #endregion

    #region GetAdminOrdersAsync Tests

    [Fact]
    public async Task GetAdminOrdersAsync_ValidQuery_ReturnsOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var query = new AdminOrderFilterQuery
        {
            Page = 1,
            PageSize = 20
        };

        _mockOrderRepository.Setup(r => r.GetFilteredForAdminAsync(
                null, null, null, null, 1, 20))
            .ReturnsAsync((orders, 1));

        // Act
        var result = await _service.GetAdminOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Orders);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_WithSearchTerm_ReturnsFilteredOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var query = new AdminOrderFilterQuery
        {
            SearchTerm = "ORD-",
            Page = 1,
            PageSize = 20
        };

        _mockOrderRepository.Setup(r => r.GetFilteredForAdminAsync(
                null, null, null, "ORD-", 1, 20))
            .ReturnsAsync((orders, 1));

        // Act
        var result = await _service.GetAdminOrdersAsync(query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Orders);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_InvalidPage_ReturnsFailure()
    {
        // Arrange
        var query = new AdminOrderFilterQuery
        {
            Page = 0,
            PageSize = 20
        };

        // Act
        var result = await _service.GetAdminOrdersAsync(query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Page number must be at least 1"));
    }

    [Fact]
    public async Task GetOrderForAdminAsync_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderForAdminAsync(order.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Order);
        Assert.Equal(order.Id, result.Order.Id);
    }

    [Fact]
    public async Task GetOrderForAdminAsync_EmptyOrderId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetOrderForAdminAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order ID is required"));
    }

    [Fact]
    public async Task GetOrderForAdminAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderForAdminAsync(orderId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Order not found"));
    }

    #endregion

    #region GetShippingStatusHistoryAsync Tests

    [Fact]
    public async Task GetShippingStatusHistoryAsync_ValidSubOrderId_ReturnsHistory()
    {
        // Arrange
        var subOrderId = Guid.NewGuid();
        var history = new List<ShippingStatusHistory>
        {
            new ShippingStatusHistory
            {
                Id = Guid.NewGuid(),
                SellerSubOrderId = subOrderId,
                PreviousStatus = SellerSubOrderStatus.Paid,
                NewStatus = SellerSubOrderStatus.Preparing,
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new ShippingStatusHistory
            {
                Id = Guid.NewGuid(),
                SellerSubOrderId = subOrderId,
                PreviousStatus = SellerSubOrderStatus.Preparing,
                NewStatus = SellerSubOrderStatus.Shipped,
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-1),
                TrackingNumber = "1234567890",
                ShippingCarrier = "FedEx"
            }
        };

        _mockShippingStatusHistoryRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrderId))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetShippingStatusHistoryAsync(subOrderId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.History.Count);
    }

    [Fact]
    public async Task GetShippingStatusHistoryAsync_EmptySubOrderId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetShippingStatusHistoryAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Seller sub-order ID is required"));
    }

    #endregion

    #region UpdateSellerSubOrderStatusAsync with Shipping Notification Tests

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_ToShipped_RecordsHistoryAndSendsNotification()
    {
        // Arrange
        var parentOrder = CreateTestOrder();
        var subOrder = new SellerSubOrder
        {
            Id = Guid.NewGuid(),
            OrderId = parentOrder.Id,
            StoreId = TestStoreId,
            StoreName = "Test Store",
            SubOrderNumber = "ORD-123-S1",
            Status = SellerSubOrderStatus.Preparing,
            Items = new List<SellerSubOrderItem>
            {
                new SellerSubOrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = TestProductId,
                    ProductTitle = "Test Product",
                    UnitPrice = 10.00m,
                    Quantity = 1,
                    Status = SellerSubOrderItemStatus.Preparing
                }
            },
            Order = parentOrder
        };

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Shipped,
            TrackingNumber = "1234567890",
            ShippingCarrier = "UPS"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);
        _mockShippingStatusHistoryRepository.Setup(r => r.AddAsync(It.IsAny<ShippingStatusHistory>()))
            .ReturnsAsync((ShippingStatusHistory h) => h);
        _mockShippingNotificationService.Setup(s => s.SendShippingNotificationAsync(It.IsAny<SellerSubOrder>(), It.IsAny<Order>()))
            .ReturnsAsync(SendEmailResult.Success());

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockShippingStatusHistoryRepository.Verify(r => r.AddAsync(It.Is<ShippingStatusHistory>(h =>
            h.PreviousStatus == SellerSubOrderStatus.Preparing &&
            h.NewStatus == SellerSubOrderStatus.Shipped &&
            h.TrackingNumber == "1234567890" &&
            h.ShippingCarrier == "UPS")), Times.Once);
        _mockShippingNotificationService.Verify(s => s.SendShippingNotificationAsync(
            It.IsAny<SellerSubOrder>(), It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSellerSubOrderStatusAsync_ToDelivered_RecordsHistoryButNoNotification()
    {
        // Arrange
        var parentOrder = CreateTestOrder();
        var subOrder = new SellerSubOrder
        {
            Id = Guid.NewGuid(),
            OrderId = parentOrder.Id,
            StoreId = TestStoreId,
            StoreName = "Test Store",
            SubOrderNumber = "ORD-123-S1",
            Status = SellerSubOrderStatus.Shipped,
            ShippedAt = DateTimeOffset.UtcNow.AddDays(-2),
            Items = new List<SellerSubOrderItem>
            {
                new SellerSubOrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = TestProductId,
                    ProductTitle = "Test Product",
                    UnitPrice = 10.00m,
                    Quantity = 1,
                    Status = SellerSubOrderItemStatus.Shipped
                }
            },
            Order = parentOrder
        };

        var command = new UpdateSellerSubOrderStatusCommand
        {
            NewStatus = SellerSubOrderStatus.Delivered
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockSellerSubOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerSubOrder>()))
            .Returns(Task.CompletedTask);
        _mockShippingStatusHistoryRepository.Setup(r => r.AddAsync(It.IsAny<ShippingStatusHistory>()))
            .ReturnsAsync((ShippingStatusHistory h) => h);

        // Act
        var result = await _service.UpdateSellerSubOrderStatusAsync(subOrder.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockShippingStatusHistoryRepository.Verify(r => r.AddAsync(It.Is<ShippingStatusHistory>(h =>
            h.PreviousStatus == SellerSubOrderStatus.Shipped &&
            h.NewStatus == SellerSubOrderStatus.Delivered)), Times.Once);
        // No shipping notification for delivered status
        _mockShippingNotificationService.Verify(s => s.SendShippingNotificationAsync(
            It.IsAny<SellerSubOrder>(), It.IsAny<Order>()), Times.Never);
    }

    #endregion
}
