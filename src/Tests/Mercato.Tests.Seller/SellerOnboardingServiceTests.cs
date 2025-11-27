using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Seller;

public class SellerOnboardingServiceTests
{
    private const string TestSellerId = "seller-test-123";

    [Fact]
    public async Task GetOrCreateOnboardingAsync_WithNoExistingOnboarding_CreatesNewOnboarding()
    {
        // Arrange
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(null as SellerOnboarding);
        mockRepository.Setup(r => r.CreateAsync(It.IsAny<SellerOnboarding>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetOrCreateOnboardingAsync(TestSellerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestSellerId, result.SellerId);
        Assert.Equal(OnboardingStep.StoreProfile, result.CurrentStep);
        Assert.Equal(OnboardingStatus.InProgress, result.Status);
        mockRepository.Verify(r => r.CreateAsync(It.IsAny<SellerOnboarding>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateOnboardingAsync_WithExistingOnboarding_ReturnsExisting()
    {
        // Arrange
        var existingOnboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(existingOnboarding);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.GetOrCreateOnboardingAsync(TestSellerId);

        // Assert
        Assert.Equal(existingOnboarding.Id, result.Id);
        mockRepository.Verify(r => r.CreateAsync(It.IsAny<SellerOnboarding>()), Times.Never);
    }

    [Fact]
    public async Task SaveStoreProfileAsync_WithValidData_SavesAndAdvancesStep()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerOnboarding>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SaveStoreProfileCommand
        {
            SellerId = TestSellerId,
            StoreName = "My Test Store",
            StoreDescription = "This is a test store with a description that is long enough."
        };

        // Act
        var result = await service.SaveStoreProfileAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<SellerOnboarding>(o =>
            o.StoreName == "My Test Store" &&
            o.StoreDescription == "This is a test store with a description that is long enough." &&
            o.CurrentStep == OnboardingStep.VerificationData)), Times.Once);
    }

    [Fact]
    public async Task SaveStoreProfileAsync_WithMissingStoreName_ReturnsError()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        var command = new SaveStoreProfileCommand
        {
            SellerId = TestSellerId,
            StoreName = "",
            StoreDescription = "Valid description that is long enough for the test."
        };

        // Act
        var result = await service.SaveStoreProfileAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Store name"));
    }

    [Fact]
    public async Task SaveStoreProfileAsync_WithShortDescription_ReturnsError()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        var command = new SaveStoreProfileCommand
        {
            SellerId = TestSellerId,
            StoreName = "Valid Store Name",
            StoreDescription = "Short"
        };

        // Act
        var result = await service.SaveStoreProfileAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Store description"));
    }

    [Fact]
    public async Task SaveVerificationDataAsync_WithoutStoreProfile_ReturnsError()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        var command = new SaveVerificationDataCommand
        {
            SellerId = TestSellerId,
            SellerType = SellerType.Individual,
            FullName = "Test Person",
            PersonalIdNumber = "ID12345",
            BusinessAddress = "123 Test Street",
            TaxId = "123456789"
        };

        // Act
        var result = await service.SaveVerificationDataAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("store profile"));
    }

    [Fact]
    public async Task SaveVerificationDataAsync_WithStoreProfileComplete_SavesIndividualData()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.StoreName = "My Store";
        onboarding.StoreDescription = "My store description that is long enough";
        onboarding.CurrentStep = OnboardingStep.VerificationData;

        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerOnboarding>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SaveVerificationDataCommand
        {
            SellerId = TestSellerId,
            SellerType = SellerType.Individual,
            FullName = "Test Person",
            PersonalIdNumber = "ID12345",
            BusinessAddress = "123 Test Street, City, Country",
            TaxId = "123456789"
        };

        // Act
        var result = await service.SaveVerificationDataAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<SellerOnboarding>(o =>
            o.FullName == "Test Person" &&
            o.PersonalIdNumber == "ID12345" &&
            o.SellerType == SellerType.Individual &&
            o.CurrentStep == OnboardingStep.PayoutBasics)), Times.Once);
    }

    [Fact]
    public async Task SaveVerificationDataAsync_ForCompanySeller_WithValidData_SavesData()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.StoreName = "My Store";
        onboarding.StoreDescription = "My store description that is long enough";
        onboarding.CurrentStep = OnboardingStep.VerificationData;

        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerOnboarding>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SaveVerificationDataCommand
        {
            SellerId = TestSellerId,
            SellerType = SellerType.Company,
            BusinessName = "Test Company LLC",
            BusinessAddress = "123 Business Street, City, Country",
            TaxId = "TAX123456",
            BusinessRegistrationNumber = "REG123456",
            ContactPersonName = "John Doe",
            ContactPersonEmail = "john@company.com",
            ContactPersonPhone = "+1234567890"
        };

        // Act
        var result = await service.SaveVerificationDataAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<SellerOnboarding>(o =>
            o.BusinessName == "Test Company LLC" &&
            o.BusinessRegistrationNumber == "REG123456" &&
            o.ContactPersonName == "John Doe" &&
            o.ContactPersonEmail == "john@company.com" &&
            o.ContactPersonPhone == "+1234567890" &&
            o.SellerType == SellerType.Company &&
            o.CurrentStep == OnboardingStep.PayoutBasics)), Times.Once);
    }

    [Fact]
    public async Task SaveVerificationDataAsync_ForCompanySeller_WithMissingRequiredFields_ReturnsErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.StoreName = "My Store";
        onboarding.StoreDescription = "My store description that is long enough";
        onboarding.CurrentStep = OnboardingStep.VerificationData;

        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        var command = new SaveVerificationDataCommand
        {
            SellerId = TestSellerId,
            SellerType = SellerType.Company,
            BusinessAddress = "123 Business Street, City, Country",
            TaxId = "TAX123456"
            // Missing: BusinessName, BusinessRegistrationNumber, ContactPersonName, ContactPersonEmail, ContactPersonPhone
        };

        // Act
        var result = await service.SaveVerificationDataAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Business name is required"));
        Assert.Contains(result.Errors, e => e.Contains("Business registration number is required"));
        Assert.Contains(result.Errors, e => e.Contains("Contact person name is required"));
        Assert.Contains(result.Errors, e => e.Contains("Contact person email is required"));
        Assert.Contains(result.Errors, e => e.Contains("Contact person phone is required"));
    }

    [Fact]
    public async Task SaveVerificationDataAsync_ForIndividualSeller_WithMissingRequiredFields_ReturnsErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.StoreName = "My Store";
        onboarding.StoreDescription = "My store description that is long enough";
        onboarding.CurrentStep = OnboardingStep.VerificationData;

        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        var command = new SaveVerificationDataCommand
        {
            SellerId = TestSellerId,
            SellerType = SellerType.Individual,
            BusinessAddress = "123 Home Street, City, Country",
            TaxId = "TAX123456"
            // Missing: FullName, PersonalIdNumber
        };

        // Act
        var result = await service.SaveVerificationDataAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Full name is required"));
        Assert.Contains(result.Errors, e => e.Contains("Personal ID number is required"));
    }

    [Fact]
    public async Task SavePayoutBasicsAsync_WithoutPreviousSteps_ReturnsError()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutBasicsCommand
        {
            SellerId = TestSellerId,
            BankName = "Test Bank",
            BankAccountNumber = "1234567890",
            AccountHolderName = "Test Account Holder"
        };

        // Act
        var result = await service.SavePayoutBasicsAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("store profile"));
    }

    [Fact]
    public async Task SavePayoutBasicsAsync_WithPreviousStepsComplete_SavesData()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.StoreName = "My Store";
        onboarding.StoreDescription = "My store description that is long enough";
        onboarding.SellerType = SellerType.Individual;
        onboarding.FullName = "Test Person";
        onboarding.PersonalIdNumber = "ID12345678";
        onboarding.BusinessAddress = "123 Test Street";
        onboarding.TaxId = "123456789";
        onboarding.CurrentStep = OnboardingStep.PayoutBasics;

        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerOnboarding>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        var command = new SavePayoutBasicsCommand
        {
            SellerId = TestSellerId,
            BankName = "Test Bank",
            BankAccountNumber = "1234567890",
            AccountHolderName = "Test Account Holder"
        };

        // Act
        var result = await service.SavePayoutBasicsAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<SellerOnboarding>(o =>
            o.BankName == "Test Bank" &&
            o.CurrentStep == OnboardingStep.Completed)), Times.Once);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_WithAllStepsComplete_SetsStatusToPendingVerification()
    {
        // Arrange
        var onboarding = CreateCompleteOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SellerOnboarding>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.CompleteOnboardingAsync(TestSellerId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(onboarding.Id, result.OnboardingId);
        mockRepository.Verify(r => r.UpdateAsync(It.Is<SellerOnboarding>(o =>
            o.Status == OnboardingStatus.PendingVerification &&
            o.CompletedAt != null)), Times.Once);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_WithIncompleteSteps_ReturnsErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.CompleteOnboardingAsync(TestSellerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_WhenAlreadyPendingVerification_ReturnsError()
    {
        // Arrange
        var onboarding = CreateCompleteOnboarding();
        onboarding.Status = OnboardingStatus.PendingVerification;
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.CompleteOnboardingAsync(TestSellerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("pending verification"));
    }

    [Fact]
    public async Task IsOnboardingCompleteAsync_WithNoOnboarding_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync((SellerOnboarding?)null);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.IsOnboardingCompleteAsync(TestSellerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsOnboardingCompleteAsync_WithPendingVerificationStatus_ReturnsTrue()
    {
        // Arrange
        var onboarding = CreateCompleteOnboarding();
        onboarding.Status = OnboardingStatus.PendingVerification;
        var mockRepository = new Mock<ISellerOnboardingRepository>(MockBehavior.Strict);
        mockRepository.Setup(r => r.GetBySellerIdAsync(TestSellerId))
            .ReturnsAsync(onboarding);

        var service = CreateService(mockRepository.Object);

        // Act
        var result = await service.IsOnboardingCompleteAsync(TestSellerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetStoreProfileValidationErrors_WithMissingStoreName_ReturnsError()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.StoreDescription = "Valid description";
        var service = CreateService(new Mock<ISellerOnboardingRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetStoreProfileValidationErrors(onboarding);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Store name", errors[0]);
    }

    [Fact]
    public void GetVerificationDataValidationErrors_ForIndividual_WithMissingFields_ReturnsErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.SellerType = SellerType.Individual;
        var service = CreateService(new Mock<ISellerOnboardingRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetVerificationDataValidationErrors(onboarding);

        // Assert
        Assert.Equal(4, errors.Count);
        Assert.Contains("Address is required.", errors);
        Assert.Contains("Tax ID is required.", errors);
        Assert.Contains("Full name is required.", errors);
        Assert.Contains("Personal ID number is required.", errors);
    }

    [Fact]
    public void GetVerificationDataValidationErrors_ForCompany_WithMissingFields_ReturnsErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.SellerType = SellerType.Company;
        var service = CreateService(new Mock<ISellerOnboardingRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetVerificationDataValidationErrors(onboarding);

        // Assert
        Assert.Equal(7, errors.Count);
        Assert.Contains("Address is required.", errors);
        Assert.Contains("Tax ID is required.", errors);
        Assert.Contains("Business name is required.", errors);
        Assert.Contains("Business registration number is required.", errors);
        Assert.Contains("Contact person name is required.", errors);
        Assert.Contains("Contact person email is required.", errors);
        Assert.Contains("Contact person phone is required.", errors);
    }

    [Fact]
    public void GetVerificationDataValidationErrors_ForIndividual_WithAllFieldsComplete_ReturnsNoErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.SellerType = SellerType.Individual;
        onboarding.FullName = "John Doe";
        onboarding.PersonalIdNumber = "ID12345678";
        onboarding.BusinessAddress = "123 Main Street, City";
        onboarding.TaxId = "TAX123456789";
        var service = CreateService(new Mock<ISellerOnboardingRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetVerificationDataValidationErrors(onboarding);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GetVerificationDataValidationErrors_ForCompany_WithAllFieldsComplete_ReturnsNoErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        onboarding.SellerType = SellerType.Company;
        onboarding.BusinessName = "Test Company LLC";
        onboarding.BusinessAddress = "123 Business Street, City";
        onboarding.TaxId = "TAX123456789";
        onboarding.BusinessRegistrationNumber = "REG123456";
        onboarding.ContactPersonName = "Jane Smith";
        onboarding.ContactPersonEmail = "jane@company.com";
        onboarding.ContactPersonPhone = "+1234567890";
        var service = CreateService(new Mock<ISellerOnboardingRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetVerificationDataValidationErrors(onboarding);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GetPayoutBasicsValidationErrors_WithMissingFields_ReturnsErrors()
    {
        // Arrange
        var onboarding = CreateOnboarding();
        var service = CreateService(new Mock<ISellerOnboardingRepository>(MockBehavior.Strict).Object);

        // Act
        var errors = service.GetPayoutBasicsValidationErrors(onboarding);

        // Assert
        Assert.Equal(3, errors.Count);
    }

    private static SellerOnboardingService CreateService(ISellerOnboardingRepository repository)
    {
        var mockLogger = new Mock<ILogger<SellerOnboardingService>>();
        return new SellerOnboardingService(repository, mockLogger.Object);
    }

    private static SellerOnboarding CreateOnboarding()
    {
        return new SellerOnboarding
        {
            Id = Guid.NewGuid(),
            SellerId = TestSellerId,
            CurrentStep = OnboardingStep.StoreProfile,
            Status = OnboardingStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static SellerOnboarding CreateCompleteOnboarding()
    {
        return new SellerOnboarding
        {
            Id = Guid.NewGuid(),
            SellerId = TestSellerId,
            CurrentStep = OnboardingStep.Completed,
            Status = OnboardingStatus.InProgress,
            SellerType = SellerType.Company,
            StoreName = "Test Store",
            StoreDescription = "This is a test store description that is long enough.",
            BusinessName = "Test Business",
            BusinessAddress = "123 Test Street, City, Country",
            TaxId = "123456789",
            BusinessRegistrationNumber = "REG123456",
            ContactPersonName = "Test Contact",
            ContactPersonEmail = "contact@test.com",
            ContactPersonPhone = "+1234567890",
            BankName = "Test Bank",
            BankAccountNumber = "1234567890",
            AccountHolderName = "Test Account Holder",
            StartedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
