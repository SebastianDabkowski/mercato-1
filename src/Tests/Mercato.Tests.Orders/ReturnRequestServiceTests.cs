using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Orders;

public class ReturnRequestServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestReturnRequestId = Guid.NewGuid();

    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ISellerSubOrderRepository> _mockSellerSubOrderRepository;
    private readonly Mock<IReturnRequestRepository> _mockReturnRequestRepository;
    private readonly Mock<IShippingStatusHistoryRepository> _mockShippingStatusHistoryRepository;
    private readonly Mock<IOrderConfirmationEmailService> _mockEmailService;
    private readonly Mock<IShippingNotificationService> _mockShippingNotificationService;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly ReturnSettings _returnSettings;
    private readonly OrderService _service;

    public ReturnRequestServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        _mockSellerSubOrderRepository = new Mock<ISellerSubOrderRepository>(MockBehavior.Strict);
        _mockReturnRequestRepository = new Mock<IReturnRequestRepository>(MockBehavior.Strict);
        _mockShippingStatusHistoryRepository = new Mock<IShippingStatusHistoryRepository>(MockBehavior.Strict);
        _mockEmailService = new Mock<IOrderConfirmationEmailService>(MockBehavior.Strict);
        _mockShippingNotificationService = new Mock<IShippingNotificationService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<OrderService>>();
        _returnSettings = new ReturnSettings { ReturnWindowDays = 30 };

        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockSellerSubOrderRepository.Object,
            _mockReturnRequestRepository.Object,
            _mockShippingStatusHistoryRepository.Object,
            _mockEmailService.Object,
            _mockShippingNotificationService.Object,
            Options.Create(_returnSettings),
            _mockLogger.Object);
    }

    #region CreateReturnRequestAsync Tests

    [Fact]
    public async Task CreateReturnRequestAsync_ValidCommand_CreatesReturnRequest()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync((ReturnRequest?)null);
        _mockReturnRequestRepository.Setup(r => r.AddAsync(It.IsAny<ReturnRequest>()))
            .ReturnsAsync((ReturnRequest rr) => rr);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReturnRequestId);
        _mockReturnRequestRepository.Verify(r => r.AddAsync(It.Is<ReturnRequest>(rr =>
            rr.SellerSubOrderId == subOrder.Id &&
            rr.BuyerId == TestBuyerId &&
            rr.Status == ReturnStatus.Requested &&
            rr.Reason == "Product was defective")), Times.Once);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = Guid.NewGuid(),
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(command.SellerSubOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.Order.BuyerId = "other-buyer";

        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_NotDelivered_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.Status = SellerSubOrderStatus.Shipped; // Not delivered yet

        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return can only be initiated for delivered orders.", result.Errors);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_OutsideReturnWindow_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.DeliveredAt = DateTimeOffset.UtcNow.AddDays(-31); // Delivered 31 days ago

        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return window has expired.", result.Errors[0]);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_ExistingReturnRequest_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var existingRequest = CreateTestReturnRequest(subOrder.Id);

        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync(existingRequest);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("A return request already exists for this sub-order.", result.Errors);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_EmptyReason_ReturnsFailure()
    {
        // Arrange
        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Reason = string.Empty
        };

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return reason is required.", result.Errors);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Arrange
        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = string.Empty,
            Reason = "Product was defective"
        };

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_ReasonTooLong_ReturnsFailure()
    {
        // Arrange
        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = TestSubOrderId,
            BuyerId = TestBuyerId,
            Reason = new string('x', 2001)
        };

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return reason must not exceed 2000 characters.", result.Errors);
    }

    #endregion

    #region GetReturnRequestAsync Tests

    [Fact]
    public async Task GetReturnRequestAsync_ValidRequest_ReturnsReturnRequest()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest(TestSubOrderId);
        returnRequest.Id = TestReturnRequestId;

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetReturnRequestAsync(TestReturnRequestId, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReturnRequest);
        Assert.Equal(TestReturnRequestId, result.ReturnRequest.Id);
    }

    [Fact]
    public async Task GetReturnRequestAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.GetReturnRequestAsync(TestReturnRequestId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request not found.", result.Errors);
    }

    [Fact]
    public async Task GetReturnRequestAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest(TestSubOrderId);
        returnRequest.Id = TestReturnRequestId;
        returnRequest.BuyerId = "other-buyer";

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetReturnRequestAsync(TestReturnRequestId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetReturnRequestAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReturnRequestAsync(TestReturnRequestId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    #endregion

    #region GetReturnRequestsForBuyerAsync Tests

    [Fact]
    public async Task GetReturnRequestsForBuyerAsync_ValidBuyer_ReturnsReturnRequests()
    {
        // Arrange
        var returnRequests = new List<ReturnRequest>
        {
            CreateTestReturnRequest(Guid.NewGuid()),
            CreateTestReturnRequest(Guid.NewGuid())
        };

        _mockReturnRequestRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(returnRequests);

        // Act
        var result = await _service.GetReturnRequestsForBuyerAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ReturnRequests.Count);
    }

    [Fact]
    public async Task GetReturnRequestsForBuyerAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReturnRequestsForBuyerAsync(string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetReturnRequestsForBuyerAsync_NoReturnRequests_ReturnsEmptyList()
    {
        // Arrange
        _mockReturnRequestRepository.Setup(r => r.GetByBuyerIdAsync(TestBuyerId))
            .ReturnsAsync(new List<ReturnRequest>());

        // Act
        var result = await _service.GetReturnRequestsForBuyerAsync(TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.ReturnRequests);
    }

    #endregion

    #region GetReturnRequestForSellerSubOrderAsync Tests

    [Fact]
    public async Task GetReturnRequestForSellerSubOrderAsync_ValidSubOrder_ReturnsReturnRequest()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder.Id);

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetReturnRequestForSellerSubOrderAsync(subOrder.Id, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReturnRequest);
    }

    [Fact]
    public async Task GetReturnRequestForSellerSubOrderAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.GetReturnRequestForSellerSubOrderAsync(TestSubOrderId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    [Fact]
    public async Task GetReturnRequestForSellerSubOrderAsync_DifferentStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.StoreId = Guid.NewGuid(); // Different store

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.GetReturnRequestForSellerSubOrderAsync(subOrder.Id, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetReturnRequestForSellerSubOrderAsync_NoReturnRequest_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.GetReturnRequestForSellerSubOrderAsync(subOrder.Id, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request not found.", result.Errors);
    }

    [Fact]
    public async Task GetReturnRequestForSellerSubOrderAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReturnRequestForSellerSubOrderAsync(TestSubOrderId, Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    #endregion

    #region UpdateReturnRequestStatusAsync Tests

    [Fact]
    public async Task UpdateReturnRequestStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder.Id);
        returnRequest.SellerSubOrder = subOrder;
        returnRequest.Status = ReturnStatus.Requested;

        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = ReturnStatus.UnderReview,
            SellerNotes = "Looking into this issue"
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockReturnRequestRepository.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateReturnRequestStatusAsync(returnRequest.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
        _mockReturnRequestRepository.Verify(r => r.UpdateAsync(It.Is<ReturnRequest>(rr =>
            rr.Status == ReturnStatus.UnderReview &&
            rr.SellerNotes == "Looking into this issue")), Times.Once);
    }

    [Fact]
    public async Task UpdateReturnRequestStatusAsync_InvalidTransition_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder.Id);
        returnRequest.SellerSubOrder = subOrder;
        returnRequest.Status = ReturnStatus.Rejected; // Can't transition from Rejected

        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = ReturnStatus.Approved
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.UpdateReturnRequestStatusAsync(returnRequest.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot transition from Rejected to Approved.", result.Errors);
    }

    [Fact]
    public async Task UpdateReturnRequestStatusAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = ReturnStatus.Approved
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.UpdateReturnRequestStatusAsync(TestReturnRequestId, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request not found.", result.Errors);
    }

    [Fact]
    public async Task UpdateReturnRequestStatusAsync_DifferentStore_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.StoreId = Guid.NewGuid(); // Different store
        var returnRequest = CreateTestReturnRequest(subOrder.Id);
        returnRequest.SellerSubOrder = subOrder;

        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = ReturnStatus.Approved
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.UpdateReturnRequestStatusAsync(returnRequest.Id, TestStoreId, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task UpdateReturnRequestStatusAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = ReturnStatus.Approved
        };

        // Act
        var result = await _service.UpdateReturnRequestStatusAsync(TestReturnRequestId, Guid.Empty, command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Theory]
    [InlineData(ReturnStatus.Requested, ReturnStatus.UnderReview)]
    [InlineData(ReturnStatus.Requested, ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Requested, ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.UnderReview, ReturnStatus.Approved)]
    [InlineData(ReturnStatus.UnderReview, ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Approved, ReturnStatus.Completed)]
    public async Task UpdateReturnRequestStatusAsync_ValidTransitions_Succeed(
        ReturnStatus currentStatus, ReturnStatus newStatus)
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var returnRequest = CreateTestReturnRequest(subOrder.Id);
        returnRequest.SellerSubOrder = subOrder;
        returnRequest.Status = currentStatus;

        var command = new UpdateReturnRequestStatusCommand
        {
            NewStatus = newStatus
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockReturnRequestRepository.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateReturnRequestStatusAsync(returnRequest.Id, TestStoreId, command);

        // Assert
        Assert.True(result.Succeeded);
    }

    #endregion

    #region CanInitiateReturnAsync Tests

    [Fact]
    public async Task CanInitiateReturnAsync_ValidSubOrder_ReturnsCanInitiate()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.CanInitiateReturnAsync(subOrder.Id, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.CanInitiate);
        Assert.Null(result.BlockedReason);
    }

    [Fact]
    public async Task CanInitiateReturnAsync_NotDelivered_ReturnsCannotInitiate()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.Status = SellerSubOrderStatus.Shipped;

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CanInitiateReturnAsync(subOrder.Id, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanInitiate);
        Assert.Contains("delivered orders", result.BlockedReason);
    }

    [Fact]
    public async Task CanInitiateReturnAsync_OutsideReturnWindow_ReturnsCannotInitiate()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.DeliveredAt = DateTimeOffset.UtcNow.AddDays(-31);

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CanInitiateReturnAsync(subOrder.Id, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanInitiate);
        Assert.Contains("expired", result.BlockedReason);
    }

    [Fact]
    public async Task CanInitiateReturnAsync_ExistingReturnRequest_ReturnsCannotInitiate()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        var existingRequest = CreateTestReturnRequest(subOrder.Id);

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync(existingRequest);

        // Act
        var result = await _service.CanInitiateReturnAsync(subOrder.Id, TestBuyerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.CanInitiate);
        Assert.Contains("already exists", result.BlockedReason);
    }

    [Fact]
    public async Task CanInitiateReturnAsync_SubOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(TestSubOrderId))
            .ReturnsAsync((SellerSubOrder?)null);

        // Act
        var result = await _service.CanInitiateReturnAsync(TestSubOrderId, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sub-order not found.", result.Errors);
    }

    [Fact]
    public async Task CanInitiateReturnAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.Order.BuyerId = "other-buyer";

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CanInitiateReturnAsync(subOrder.Id, TestBuyerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task CanInitiateReturnAsync_EmptyBuyerId_ReturnsFailure()
    {
        // Act
        var result = await _service.CanInitiateReturnAsync(TestSubOrderId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer ID is required.", result.Errors);
    }

    #endregion

    #region Return Window Validation Tests

    [Fact]
    public async Task CreateReturnRequestAsync_WithinReturnWindow_Succeeds()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.DeliveredAt = DateTimeOffset.UtcNow.AddDays(-29); // 29 days ago, safely within 30-day window

        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);
        _mockReturnRequestRepository.Setup(r => r.GetBySellerSubOrderIdAsync(subOrder.Id))
            .ReturnsAsync((ReturnRequest?)null);
        _mockReturnRequestRepository.Setup(r => r.AddAsync(It.IsAny<ReturnRequest>()))
            .ReturnsAsync((ReturnRequest rr) => rr);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_NoDeliveryDate_ReturnsFailure()
    {
        // Arrange
        var subOrder = CreateTestDeliveredSubOrder();
        subOrder.DeliveredAt = null;

        var command = new CreateReturnRequestCommand
        {
            SellerSubOrderId = subOrder.Id,
            BuyerId = TestBuyerId,
            Reason = "Product was defective"
        };

        _mockSellerSubOrderRepository.Setup(r => r.GetByIdAsync(subOrder.Id))
            .ReturnsAsync(subOrder);

        // Act
        var result = await _service.CreateReturnRequestAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return window has expired.", result.Errors[0]);
    }

    #endregion

    #region Helper Methods

    private static SellerSubOrder CreateTestDeliveredSubOrder()
    {
        var order = new Order
        {
            Id = TestOrderId,
            BuyerId = TestBuyerId,
            OrderNumber = "ORD-12345678",
            Status = OrderStatus.Delivered,
            DeliveryFullName = "Test Buyer",
            DeliveryAddressLine1 = "123 Test St",
            DeliveryCity = "Test City",
            DeliveryPostalCode = "12345",
            DeliveryCountry = "US",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Items = [],
            SellerSubOrders = []
        };

        return new SellerSubOrder
        {
            Id = TestSubOrderId,
            OrderId = TestOrderId,
            StoreId = TestStoreId,
            StoreName = "Test Store",
            SubOrderNumber = "ORD-12345678-S1",
            Status = SellerSubOrderStatus.Delivered,
            ItemsSubtotal = 59.98m,
            ShippingCost = 5.99m,
            TotalAmount = 65.97m,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1), // Delivered yesterday
            Order = order,
            Items = []
        };
    }

    private static ReturnRequest CreateTestReturnRequest(Guid sellerSubOrderId)
    {
        return new ReturnRequest
        {
            Id = Guid.NewGuid(),
            SellerSubOrderId = sellerSubOrderId,
            BuyerId = TestBuyerId,
            Status = ReturnStatus.Requested,
            Reason = "Product was defective",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
