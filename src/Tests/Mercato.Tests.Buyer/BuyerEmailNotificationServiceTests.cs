using Mercato.Buyer.Application.Services;
using Mercato.Buyer.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercato.Tests.Buyer;

public class BuyerEmailNotificationServiceTests
{
    private readonly Mock<ILogger<BuyerEmailNotificationService>> _mockLogger;
    private readonly BuyerEmailSettings _emailSettings;
    private readonly BuyerEmailNotificationService _service;

    public BuyerEmailNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<BuyerEmailNotificationService>>();
        _emailSettings = new BuyerEmailSettings();
        var mockOptions = new Mock<IOptions<BuyerEmailSettings>>(MockBehavior.Strict);
        mockOptions.Setup(o => o.Value).Returns(_emailSettings);
        _service = new BuyerEmailNotificationService(_mockLogger.Object, mockOptions.Object);
    }

    #region SendRegistrationWelcomeEmailAsync Tests

    [Fact]
    public async Task SendRegistrationWelcomeEmailAsync_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var email = "buyer@example.com";

        // Act
        var result = await _service.SendRegistrationWelcomeEmailAsync(email);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendRegistrationWelcomeEmailAsync_ValidEmailWithName_ReturnsSuccess()
    {
        // Arrange
        var email = "buyer@example.com";
        var name = "John Doe";

        // Act
        var result = await _service.SendRegistrationWelcomeEmailAsync(email, name);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SendRegistrationWelcomeEmailAsync_EmptyOrNullEmail_ReturnsFailure(string? email)
    {
        // Act
        var result = await _service.SendRegistrationWelcomeEmailAsync(email!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer email is required.", result.Errors);
    }

    #endregion

    #region SendPayoutConfirmationEmailAsync Tests

    [Fact]
    public async Task SendPayoutConfirmationEmailAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidPayoutCommand();

        // Act
        var result = await _service.SendPayoutConfirmationEmailAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendPayoutConfirmationEmailAsync_EmptyEmail_ReturnsFailure(string email)
    {
        // Arrange
        var command = CreateValidPayoutCommand();
        command.BuyerEmail = email;

        // Act
        var result = await _service.SendPayoutConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer email is required.", result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task SendPayoutConfirmationEmailAsync_ZeroOrNegativeAmount_ReturnsFailure(decimal amount)
    {
        // Arrange
        var command = CreateValidPayoutCommand();
        command.Amount = amount;

        // Act
        var result = await _service.SendPayoutConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Payout amount must be greater than zero.", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendPayoutConfirmationEmailAsync_EmptyPayoutReference_ReturnsFailure(string reference)
    {
        // Arrange
        var command = CreateValidPayoutCommand();
        command.PayoutReference = reference;

        // Act
        var result = await _service.SendPayoutConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Payout reference is required.", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendPayoutConfirmationEmailAsync_EmptyCurrency_ReturnsFailure(string currency)
    {
        // Arrange
        var command = CreateValidPayoutCommand();
        command.Currency = currency;

        // Act
        var result = await _service.SendPayoutConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Currency is required.", result.Errors);
    }

    [Fact]
    public async Task SendPayoutConfirmationEmailAsync_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new SendPayoutEmailCommand
        {
            BuyerEmail = "",
            Amount = 0,
            PayoutReference = "",
            Currency = ""
        };

        // Act
        var result = await _service.SendPayoutConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count >= 4);
    }

    #endregion

    #region SendRefundConfirmationEmailAsync Tests

    [Fact]
    public async Task SendRefundConfirmationEmailAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidRefundCommand();

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendRefundConfirmationEmailAsync_FullRefund_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.IsFullRefund = true;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SendRefundConfirmationEmailAsync_PartialRefund_ReturnsSuccess()
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.IsFullRefund = false;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendRefundConfirmationEmailAsync_EmptyEmail_ReturnsFailure(string email)
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.BuyerEmail = email;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Buyer email is required.", result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task SendRefundConfirmationEmailAsync_ZeroOrNegativeAmount_ReturnsFailure(decimal amount)
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.Amount = amount;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Refund amount must be greater than zero.", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendRefundConfirmationEmailAsync_EmptyRefundReference_ReturnsFailure(string reference)
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.RefundReference = reference;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Refund reference is required.", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendRefundConfirmationEmailAsync_EmptyOrderNumber_ReturnsFailure(string orderNumber)
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.OrderNumber = orderNumber;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Order number is required.", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendRefundConfirmationEmailAsync_EmptyCurrency_ReturnsFailure(string currency)
    {
        // Arrange
        var command = CreateValidRefundCommand();
        command.Currency = currency;

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Currency is required.", result.Errors);
    }

    [Fact]
    public async Task SendRefundConfirmationEmailAsync_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new SendRefundEmailCommand
        {
            BuyerEmail = "",
            Amount = 0,
            RefundReference = "",
            OrderNumber = "",
            Currency = ""
        };

        // Act
        var result = await _service.SendRefundConfirmationEmailAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count >= 5);
    }

    #endregion

    #region Helper Methods

    private static SendPayoutEmailCommand CreateValidPayoutCommand()
    {
        return new SendPayoutEmailCommand
        {
            BuyerEmail = "buyer@example.com",
            BuyerName = "John Doe",
            Amount = 150.00m,
            Currency = "USD",
            PayoutReference = "PAY-12345",
            ProcessedAt = DateTimeOffset.UtcNow,
            PaymentMethod = "Bank Account ending in 1234"
        };
    }

    private static SendRefundEmailCommand CreateValidRefundCommand()
    {
        return new SendRefundEmailCommand
        {
            BuyerEmail = "buyer@example.com",
            BuyerName = "Jane Doe",
            Amount = 75.50m,
            Currency = "USD",
            RefundReference = "REF-67890",
            OrderNumber = "ORD-12345",
            Reason = "Product returned",
            ProcessedAt = DateTimeOffset.UtcNow,
            IsFullRefund = false
        };
    }

    #endregion
}
