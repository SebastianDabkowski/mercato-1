using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class PayoutSettingsServiceTests
{
    private const string TestSellerId = "seller-test-123";

    [Fact]
    public async Task GetPayoutSettingsAsync_WithExistingSettings_ReturnsSettings()
    {
        // Arrange
        var existingSettings = CreatePayoutSettings();
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(existingSettings);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetPayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingSettings.Id, result.Id);
    }

    [Fact]
    public async Task GetPayoutSettingsAsync_WithNoSettings_ReturnsNull()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync((PayoutSettings?)null);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetPayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrCreatePayoutSettingsAsync_WithNoExistingSettings_CreatesNewSettings()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync((PayoutSettings?)null);
        mockRepository.Setup(r => r.CreateAsync(It.IsAny<PayoutSettings>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetOrCreatePayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestSellerId, result.SellerId);
        Assert.Equal(PayoutMethod.BankTransfer, result.PreferredPayoutMethod);
        Assert.False(result.IsComplete);
        mockRepository.Verify(r => r.CreateAsync(It.IsAny<PayoutSettings>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreatePayoutSettingsAsync_WithExistingSettings_ReturnsExisting()
    {
        // Arrange
        var existingSettings = CreatePayoutSettings();
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(existingSettings);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetOrCreatePayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.Equal(existingSettings.Id, result.Id);
        mockRepository.Verify(r => r.CreateAsync(It.IsAny<PayoutSettings>()), Times.Never);
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_WithValidBankTransferData_SavesData()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(CreatePayoutSettings());
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PayoutSettings>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            BankName = "Test Bank",
            BankAccountNumber = "1234567890",
            AccountHolderName = "Test Account Holder"
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<PayoutSettings>(p =>
            p.BankName == "Test Bank" &&
            p.BankAccountNumber == "1234567890" &&
            p.AccountHolderName == "Test Account Holder" &&
            p.PreferredPayoutMethod == PayoutMethod.BankTransfer &&
            p.IsComplete == true)), Times.Once);
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_WithMissingBankName_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(CreatePayoutSettings());

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            BankAccountNumber = "1234567890",
            AccountHolderName = "Test Account Holder"
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Bank name is required"));
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_WithMissingBankAccountNumber_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(CreatePayoutSettings());

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            BankName = "Test Bank",
            AccountHolderName = "Test Account Holder"
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Bank account number is required"));
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_WithValidPaymentAccountData_SavesData()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(CreatePayoutSettings());
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PayoutSettings>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.PaymentAccount,
            PaymentAccountEmail = "seller@example.com"
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<PayoutSettings>(p =>
            p.PaymentAccountEmail == "seller@example.com" &&
            p.PreferredPayoutMethod == PayoutMethod.PaymentAccount &&
            p.IsComplete == true)), Times.Once);
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_WithMissingPaymentAccountEmailAndId_ReturnsError()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(CreatePayoutSettings());

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.PaymentAccount
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Payment account email or ID is required"));
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_SwitchingToBankTransfer_ClearsPaymentAccountFields()
    {
        // Arrange
        var existingSettings = CreatePayoutSettings();
        existingSettings.PreferredPayoutMethod = PayoutMethod.PaymentAccount;
        existingSettings.PaymentAccountEmail = "seller@example.com";

        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(existingSettings);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PayoutSettings>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            BankName = "New Bank",
            BankAccountNumber = "9876543210",
            AccountHolderName = "New Holder"
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<PayoutSettings>(p =>
            p.BankName == "New Bank" &&
            p.PaymentAccountEmail == null &&
            p.PaymentAccountId == null)), Times.Once);
    }

    [Fact]
    public async Task SavePayoutSettingsAsync_SwitchingToPaymentAccount_ClearsBankFields()
    {
        // Arrange
        var existingSettings = CreatePayoutSettings();
        existingSettings.PreferredPayoutMethod = PayoutMethod.BankTransfer;
        existingSettings.BankName = "Old Bank";
        existingSettings.BankAccountNumber = "1234567890";
        existingSettings.AccountHolderName = "Old Holder";

        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(existingSettings);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PayoutSettings>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutSettingsCommand
        {
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.PaymentAccount,
            PaymentAccountEmail = "seller@example.com"
        };

        // Act
        var result = await service.SavePayoutSettingsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<PayoutSettings>(p =>
            p.PaymentAccountEmail == "seller@example.com" &&
            p.BankName == null &&
            p.BankAccountNumber == null &&
            p.AccountHolderName == null)), Times.Once);
    }

    [Fact]
    public async Task HasCompletePayoutSettingsAsync_WithCompleteSettings_ReturnsTrue()
    {
        // Arrange
        var settings = CreatePayoutSettings();
        settings.IsComplete = true;

        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(settings);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.HasCompletePayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasCompletePayoutSettingsAsync_WithIncompleteSettings_ReturnsFalse()
    {
        // Arrange
        var settings = CreatePayoutSettings();
        settings.IsComplete = false;

        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(settings);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.HasCompletePayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasCompletePayoutSettingsAsync_WithNoSettings_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<IPayoutSettingsRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync((PayoutSettings?)null);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.HasCompletePayoutSettingsAsync(TestSellerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetPayoutSettingsValidationErrors_ForBankTransfer_WithMissingFields_ReturnsErrors()
    {
        // Arrange
        var settings = new PayoutSettings
        {
            PreferredPayoutMethod = PayoutMethod.BankTransfer
        };
        var service = CreateService(new Mock<IPayoutSettingsRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetPayoutSettingsValidationErrors(settings);

        // Assert
        Assert.Equal(3, errors.Count);
        Assert.Contains("Bank name is required for bank transfer payout.", errors);
        Assert.Contains("Bank account number is required for bank transfer payout.", errors);
        Assert.Contains("Account holder name is required for bank transfer payout.", errors);
    }

    [Fact]
    public void GetPayoutSettingsValidationErrors_ForPaymentAccount_WithMissingFields_ReturnsErrors()
    {
        // Arrange
        var settings = new PayoutSettings
        {
            PreferredPayoutMethod = PayoutMethod.PaymentAccount
        };
        var service = CreateService(new Mock<IPayoutSettingsRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetPayoutSettingsValidationErrors(settings);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Payment account email or ID is required for payment account payout.", errors);
    }

    [Fact]
    public void GetPayoutSettingsValidationErrors_ForBankTransfer_WithAllFields_ReturnsNoErrors()
    {
        // Arrange
        var settings = new PayoutSettings
        {
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            BankName = "Test Bank",
            BankAccountNumber = "1234567890",
            AccountHolderName = "Test Holder"
        };
        var service = CreateService(new Mock<IPayoutSettingsRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetPayoutSettingsValidationErrors(settings);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GetPayoutSettingsValidationErrors_ForPaymentAccount_WithEmail_ReturnsNoErrors()
    {
        // Arrange
        var settings = new PayoutSettings
        {
            PreferredPayoutMethod = PayoutMethod.PaymentAccount,
            PaymentAccountEmail = "seller@example.com"
        };
        var service = CreateService(new Mock<IPayoutSettingsRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetPayoutSettingsValidationErrors(settings);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GetPayoutSettingsValidationErrors_ForPaymentAccount_WithAccountId_ReturnsNoErrors()
    {
        // Arrange
        var settings = new PayoutSettings
        {
            PreferredPayoutMethod = PayoutMethod.PaymentAccount,
            PaymentAccountId = "acct_1234567890"
        };
        var service = CreateService(new Mock<IPayoutSettingsRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetPayoutSettingsValidationErrors(settings);

        // Assert
        Assert.Empty(errors);
    }

    private static PayoutSettingsService CreateService(IPayoutSettingsRepository repository)
    {
        var mockLogger = new Mock<ILogger<PayoutSettingsService>>();
        return new PayoutSettingsService(repository, mockLogger.Object);
    }

    private static PayoutSettings CreatePayoutSettings()
    {
        return new PayoutSettings
        {
            Id = Guid.NewGuid(),
            SellerId = TestSellerId,
            PreferredPayoutMethod = PayoutMethod.BankTransfer,
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
