using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure;
using Mercato.Payments.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Orders;

public class CaseMessagingServiceTests
{
    private static readonly string TestBuyerId = "test-buyer-id";
    private static readonly string TestSellerId = "test-seller-id";
    private static readonly string TestAdminId = "test-admin-id";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestSubOrderId = Guid.NewGuid();
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly Guid TestReturnRequestId = Guid.NewGuid();
    private static readonly Guid TestMessageId = Guid.NewGuid();

    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ISellerSubOrderRepository> _mockSellerSubOrderRepository;
    private readonly Mock<IReturnRequestRepository> _mockReturnRequestRepository;
    private readonly Mock<IShippingStatusHistoryRepository> _mockShippingStatusHistoryRepository;
    private readonly Mock<ICaseMessageRepository> _mockCaseMessageRepository;
    private readonly Mock<IOrderConfirmationEmailService> _mockEmailService;
    private readonly Mock<IShippingNotificationService> _mockShippingNotificationService;
    private readonly Mock<ISellerNotificationEmailService> _mockSellerNotificationEmailService;
    private readonly Mock<IStoreEmailProvider> _mockStoreEmailProvider;
    private readonly Mock<IRefundService> _mockRefundService;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly ReturnSettings _returnSettings;
    private readonly OrderService _service;

    public CaseMessagingServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        _mockSellerSubOrderRepository = new Mock<ISellerSubOrderRepository>(MockBehavior.Strict);
        _mockReturnRequestRepository = new Mock<IReturnRequestRepository>(MockBehavior.Strict);
        _mockShippingStatusHistoryRepository = new Mock<IShippingStatusHistoryRepository>(MockBehavior.Strict);
        _mockCaseMessageRepository = new Mock<ICaseMessageRepository>(MockBehavior.Strict);
        _mockEmailService = new Mock<IOrderConfirmationEmailService>(MockBehavior.Strict);
        _mockShippingNotificationService = new Mock<IShippingNotificationService>(MockBehavior.Strict);
        _mockSellerNotificationEmailService = new Mock<ISellerNotificationEmailService>(MockBehavior.Strict);
        _mockStoreEmailProvider = new Mock<IStoreEmailProvider>(MockBehavior.Strict);
        _mockRefundService = new Mock<IRefundService>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<OrderService>>();
        _returnSettings = new ReturnSettings { ReturnWindowDays = 30 };

        // Setup default store email provider behavior
        _mockStoreEmailProvider.Setup(p => p.GetStoreEmailsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _mockStoreEmailProvider.Setup(p => p.GetStoreEmailAsync(It.IsAny<Guid>()))
            .ReturnsAsync((string?)null);

        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockSellerSubOrderRepository.Object,
            _mockReturnRequestRepository.Object,
            _mockShippingStatusHistoryRepository.Object,
            _mockCaseMessageRepository.Object,
            _mockEmailService.Object,
            _mockShippingNotificationService.Object,
            _mockSellerNotificationEmailService.Object,
            _mockStoreEmailProvider.Object,
            _mockRefundService.Object,
            Options.Create(_returnSettings),
            _mockLogger.Object);
    }

    #region GetCaseMessagesAsync Tests

    [Fact]
    public async Task GetCaseMessagesAsync_BuyerAccessOwnCase_ReturnsMessages()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var messages = new List<CaseMessage>
        {
            CreateTestMessage(returnRequest.Id, TestBuyerId, "Buyer", "Hello, I need help"),
            CreateTestMessage(returnRequest.Id, TestSellerId, "Seller", "How can I help you?")
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockCaseMessageRepository.Setup(r => r.GetByReturnRequestIdAsync(returnRequest.Id))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetCaseMessagesAsync(returnRequest.Id, TestBuyerId, "Buyer");

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Messages.Count);
        Assert.NotNull(result.ReturnRequest);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_SellerAccessCase_ReturnsMessages()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var messages = new List<CaseMessage>
        {
            CreateTestMessage(returnRequest.Id, TestBuyerId, "Buyer", "Hello, I need help")
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockCaseMessageRepository.Setup(r => r.GetByReturnRequestIdAsync(returnRequest.Id))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetCaseMessagesAsync(returnRequest.Id, TestSellerId, "Seller", TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Messages);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_AdminAccessCase_ReturnsMessages()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var messages = new List<CaseMessage>
        {
            CreateTestMessage(returnRequest.Id, TestBuyerId, "Buyer", "Hello"),
            CreateTestMessage(returnRequest.Id, TestSellerId, "Seller", "Hi"),
            CreateTestMessage(returnRequest.Id, TestAdminId, "Admin", "I'm stepping in")
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockCaseMessageRepository.Setup(r => r.GetByReturnRequestIdAsync(returnRequest.Id))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetCaseMessagesAsync(returnRequest.Id, TestAdminId, "Admin");

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Messages.Count);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        returnRequest.BuyerId = "other-buyer";

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetCaseMessagesAsync(returnRequest.Id, TestBuyerId, "Buyer");

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_DifferentStore_ReturnsNotAuthorized()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var differentStoreId = Guid.NewGuid();

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetCaseMessagesAsync(returnRequest.Id, TestSellerId, "Seller", differentStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_CaseNotFound_ReturnsFailure()
    {
        // Arrange
        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.GetCaseMessagesAsync(TestReturnRequestId, TestBuyerId, "Buyer");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case not found.", result.Errors);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetCaseMessagesAsync(TestReturnRequestId, string.Empty, "Buyer");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetCaseMessagesAsync_EmptyRole_ReturnsFailure()
    {
        // Act
        var result = await _service.GetCaseMessagesAsync(TestReturnRequestId, TestBuyerId, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User role is required.", result.Errors);
    }

    #endregion

    #region AddCaseMessageAsync Tests

    [Fact]
    public async Task AddCaseMessageAsync_BuyerAddsMessage_Succeeds()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = returnRequest.Id,
            SenderUserId = TestBuyerId,
            SenderRole = "Buyer",
            Content = "This is my message"
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockCaseMessageRepository.Setup(r => r.AddAsync(It.IsAny<CaseMessage>()))
            .ReturnsAsync((CaseMessage m) => m);
        _mockReturnRequestRepository.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.MessageId);
        _mockCaseMessageRepository.Verify(r => r.AddAsync(It.Is<CaseMessage>(m =>
            m.Content == "This is my message" &&
            m.SenderRole == "Buyer" &&
            m.SenderUserId == TestBuyerId)), Times.Once);
        _mockReturnRequestRepository.Verify(r => r.UpdateAsync(It.Is<ReturnRequest>(rr =>
            rr.HasNewActivity == true &&
            rr.LastActivityByUserId == TestBuyerId)), Times.Once);
    }

    [Fact]
    public async Task AddCaseMessageAsync_SellerAddsMessage_Succeeds()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = returnRequest.Id,
            SenderUserId = TestSellerId,
            SenderRole = "Seller",
            Content = "We will process your request"
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockCaseMessageRepository.Setup(r => r.AddAsync(It.IsAny<CaseMessage>()))
            .ReturnsAsync((CaseMessage m) => m);
        _mockReturnRequestRepository.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddCaseMessageAsync(command, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task AddCaseMessageAsync_AdminAddsMessage_Succeeds()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = returnRequest.Id,
            SenderUserId = TestAdminId,
            SenderRole = "Admin",
            Content = "This is an admin intervention"
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockCaseMessageRepository.Setup(r => r.AddAsync(It.IsAny<CaseMessage>()))
            .ReturnsAsync((CaseMessage m) => m);
        _mockReturnRequestRepository.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task AddCaseMessageAsync_DifferentBuyer_ReturnsNotAuthorized()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        returnRequest.BuyerId = "other-buyer";
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = returnRequest.Id,
            SenderUserId = TestBuyerId,
            SenderRole = "Buyer",
            Content = "Test message"
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task AddCaseMessageAsync_CaseNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = TestReturnRequestId,
            SenderUserId = TestBuyerId,
            SenderRole = "Buyer",
            Content = "Test message"
        };

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case not found.", result.Errors);
    }

    [Fact]
    public async Task AddCaseMessageAsync_EmptyContent_ReturnsValidationFailure()
    {
        // Arrange
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = TestReturnRequestId,
            SenderUserId = TestBuyerId,
            SenderRole = "Buyer",
            Content = string.Empty
        };

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message content is required.", result.Errors);
    }

    [Fact]
    public async Task AddCaseMessageAsync_ContentTooLong_ReturnsValidationFailure()
    {
        // Arrange
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = TestReturnRequestId,
            SenderUserId = TestBuyerId,
            SenderRole = "Buyer",
            Content = new string('x', 2001)
        };

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Message content must not exceed 2000 characters.", result.Errors);
    }

    [Fact]
    public async Task AddCaseMessageAsync_InvalidRole_ReturnsValidationFailure()
    {
        // Arrange
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = TestReturnRequestId,
            SenderUserId = TestBuyerId,
            SenderRole = "InvalidRole",
            Content = "Test message"
        };

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Sender role must be Buyer, Seller, or Admin.", result.Errors);
    }

    [Fact]
    public async Task AddCaseMessageAsync_EmptyReturnRequestId_ReturnsValidationFailure()
    {
        // Arrange
        var command = new AddCaseMessageCommand
        {
            ReturnRequestId = Guid.Empty,
            SenderUserId = TestBuyerId,
            SenderRole = "Buyer",
            Content = "Test message"
        };

        // Act
        var result = await _service.AddCaseMessageAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request ID is required.", result.Errors);
    }

    #endregion

    #region MarkCaseActivityViewedAsync Tests

    [Fact]
    public async Task MarkCaseActivityViewedAsync_ClearsNewActivityFlag()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        returnRequest.HasNewActivity = true;
        returnRequest.LastActivityByUserId = TestSellerId; // Activity was from seller

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _mockReturnRequestRepository.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>()))
            .Returns(Task.CompletedTask);

        // Act - Buyer views (different from who created activity)
        var result = await _service.MarkCaseActivityViewedAsync(returnRequest.Id, TestBuyerId, "Buyer");

        // Assert
        Assert.True(result.Succeeded);
        _mockReturnRequestRepository.Verify(r => r.UpdateAsync(It.Is<ReturnRequest>(rr =>
            rr.HasNewActivity == false)), Times.Once);
    }

    [Fact]
    public async Task MarkCaseActivityViewedAsync_SameUserDoesNotClearFlag()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        returnRequest.HasNewActivity = true;
        returnRequest.LastActivityByUserId = TestBuyerId; // Activity was from same buyer

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act - Same buyer views (should not clear flag since they created the activity)
        var result = await _service.MarkCaseActivityViewedAsync(returnRequest.Id, TestBuyerId, "Buyer");

        // Assert
        Assert.True(result.Succeeded);
        _mockReturnRequestRepository.Verify(r => r.UpdateAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task MarkCaseActivityViewedAsync_NoNewActivity_DoesNotUpdate()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        returnRequest.HasNewActivity = false;

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.MarkCaseActivityViewedAsync(returnRequest.Id, TestBuyerId, "Buyer");

        // Assert
        Assert.True(result.Succeeded);
        _mockReturnRequestRepository.Verify(r => r.UpdateAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task MarkCaseActivityViewedAsync_NotAuthorized_ReturnsNotAuthorized()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        returnRequest.BuyerId = "other-buyer";

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.MarkCaseActivityViewedAsync(returnRequest.Id, TestBuyerId, "Buyer");

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task MarkCaseActivityViewedAsync_CaseNotFound_ReturnsFailure()
    {
        // Arrange
        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.MarkCaseActivityViewedAsync(TestReturnRequestId, TestBuyerId, "Buyer");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Case not found.", result.Errors);
    }

    [Fact]
    public async Task MarkCaseActivityViewedAsync_EmptyUserId_ReturnsFailure()
    {
        // Act
        var result = await _service.MarkCaseActivityViewedAsync(TestReturnRequestId, string.Empty, "Buyer");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    #endregion

    #region GetReturnRequestForSellerAsync Tests

    [Fact]
    public async Task GetReturnRequestForSellerAsync_ValidStoreId_ReturnsCase()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetReturnRequestForSellerAsync(returnRequest.Id, TestStoreId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReturnRequest);
        Assert.Equal(returnRequest.Id, result.ReturnRequest.Id);
    }

    [Fact]
    public async Task GetReturnRequestForSellerAsync_DifferentStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();
        var differentStoreId = Guid.NewGuid();

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetReturnRequestForSellerAsync(returnRequest.Id, differentStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
    }

    [Fact]
    public async Task GetReturnRequestForSellerAsync_EmptyStoreId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReturnRequestForSellerAsync(TestReturnRequestId, Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetReturnRequestForSellerAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.GetReturnRequestForSellerAsync(TestReturnRequestId, TestStoreId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request not found.", result.Errors);
    }

    #endregion

    #region GetReturnRequestForAdminAsync Tests

    [Fact]
    public async Task GetReturnRequestForAdminAsync_ValidId_ReturnsCase()
    {
        // Arrange
        var returnRequest = CreateTestReturnRequest();

        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _service.GetReturnRequestForAdminAsync(returnRequest.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ReturnRequest);
    }

    [Fact]
    public async Task GetReturnRequestForAdminAsync_EmptyId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetReturnRequestForAdminAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetReturnRequestForAdminAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        _mockReturnRequestRepository.Setup(r => r.GetByIdAsync(TestReturnRequestId))
            .ReturnsAsync((ReturnRequest?)null);

        // Act
        var result = await _service.GetReturnRequestForAdminAsync(TestReturnRequestId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Return request not found.", result.Errors);
    }

    #endregion

    #region Helper Methods

    private static SellerSubOrder CreateTestSubOrder()
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
            BuyerEmail = "test@example.com",
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
            DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1),
            Order = order,
            Items = new List<SellerSubOrderItem>
            {
                new SellerSubOrderItem
                {
                    Id = Guid.NewGuid(),
                    SellerSubOrderId = TestSubOrderId,
                    ProductId = Guid.NewGuid(),
                    ProductTitle = "Test Product 1",
                    UnitPrice = 29.99m,
                    Quantity = 1,
                    Status = SellerSubOrderItemStatus.Delivered,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                    LastUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };
    }

    /// <summary>
    /// Number of characters to use for short ID display.
    /// </summary>
    private const int ShortIdLength = 8;

    private static ReturnRequest CreateTestReturnRequest()
    {
        var subOrder = CreateTestSubOrder();
        return new ReturnRequest
        {
            Id = TestReturnRequestId,
            CaseNumber = $"CASE-{TestReturnRequestId.ToString("N")[..ShortIdLength].ToUpperInvariant()}",
            CaseType = CaseType.Return,
            SellerSubOrderId = subOrder.Id,
            SellerSubOrder = subOrder,
            BuyerId = TestBuyerId,
            Status = ReturnStatus.Requested,
            Reason = "Product was defective",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            HasNewActivity = false,
            CaseItems = [],
            Messages = []
        };
    }

    private static CaseMessage CreateTestMessage(Guid returnRequestId, string senderUserId, string senderRole, string content)
    {
        return new CaseMessage
        {
            Id = Guid.NewGuid(),
            ReturnRequestId = returnRequestId,
            SenderUserId = senderUserId,
            SenderRole = senderRole,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
