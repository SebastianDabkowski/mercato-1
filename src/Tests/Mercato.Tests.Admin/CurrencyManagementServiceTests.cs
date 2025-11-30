using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class CurrencyManagementServiceTests
{
    private readonly Mock<ICurrencyRepository> _mockCurrencyRepository;
    private readonly Mock<ICurrencyHistoryRepository> _mockHistoryRepository;
    private readonly Mock<ILogger<CurrencyManagementService>> _mockLogger;

    public CurrencyManagementServiceTests()
    {
        _mockCurrencyRepository = new Mock<ICurrencyRepository>(MockBehavior.Strict);
        _mockHistoryRepository = new Mock<ICurrencyHistoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CurrencyManagementService>>();
    }

    private CurrencyManagementService CreateService()
    {
        return new CurrencyManagementService(
            _mockCurrencyRepository.Object,
            _mockHistoryRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCurrencyRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrencyManagementService(null!, _mockHistoryRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHistoryRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrencyManagementService(_mockCurrencyRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrencyManagementService(_mockCurrencyRepository.Object, _mockHistoryRepository.Object, null!));
    }

    #endregion

    #region GetAllCurrenciesAsync Tests

    [Fact]
    public async Task GetAllCurrenciesAsync_ReturnsAllCurrencies()
    {
        // Arrange
        var service = CreateService();
        var currencies = new List<Currency>
        {
            new Currency
            {
                Id = Guid.NewGuid(),
                Code = "USD",
                Name = "US Dollar",
                Symbol = "$",
                IsBaseCurrency = true,
                IsEnabled = true
            },
            new Currency
            {
                Id = Guid.NewGuid(),
                Code = "EUR",
                Name = "Euro",
                Symbol = "€",
                IsBaseCurrency = false,
                IsEnabled = true
            }
        };

        _mockCurrencyRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        // Act
        var result = await service.GetAllCurrenciesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Currencies.Count);
        _mockCurrencyRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllCurrenciesAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockCurrencyRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Currency>());

        // Act
        var result = await service.GetAllCurrenciesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Currencies);
    }

    #endregion

    #region GetCurrencyByIdAsync Tests

    [Fact]
    public async Task GetCurrencyByIdAsync_ExistingCurrency_ReturnsCurrency()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$",
            IsEnabled = true
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Act
        var result = await service.GetCurrencyByIdAsync(currencyId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.Equal(currencyId, result.Currency.Id);
        Assert.Equal("USD", result.Currency.Code);
    }

    [Fact]
    public async Task GetCurrencyByIdAsync_NonExistingCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        // Act
        var result = await service.GetCurrencyByIdAsync(currencyId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task GetCurrencyByIdAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetCurrencyByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency ID is required"));
    }

    #endregion

    #region CreateCurrencyAsync Tests

    [Fact]
    public async Task CreateCurrencyAsync_ValidCommand_CreatesCurrency()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$",
            DecimalPlaces = 2,
            IsEnabled = true,
            CreatedByUserId = "admin-user-1"
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        _mockCurrencyRepository
            .Setup(r => r.AddAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency c, CancellationToken _) => c);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.Equal("USD", result.Currency.Code);
        Assert.Equal("US Dollar", result.Currency.Name);
        Assert.Equal("$", result.Currency.Symbol);
        Assert.Equal("admin-user-1", result.Currency.CreatedByUserId);
        _mockCurrencyRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateCurrencyAsync_DuplicateCode_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var existingCurrency = new Currency { Id = Guid.NewGuid(), Code = "USD" };
        var command = new CreateCurrencyCommand
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$",
            CreatedByUserId = "admin-user-1"
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCurrency);

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_EmptyCode_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "",
            Name = "US Dollar",
            Symbol = "$",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency code is required"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_InvalidCodeLength_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "US",
            Name = "US Dollar",
            Symbol = "$",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("3 characters"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_EmptyName_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "USD",
            Name = "",
            Symbol = "$",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency name is required"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_EmptySymbol_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "",
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency symbol is required"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_InvalidDecimalPlaces_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$",
            DecimalPlaces = 10,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Decimal places must be between 0 and 8"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$",
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task CreateCurrencyAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCurrencyCommand
        {
            Code = "EUR",
            Name = "Euro",
            Symbol = "€",
            CreatedByUserId = "admin-user-1",
            CreatedByUserEmail = "admin@test.com"
        };

        CurrencyHistory? capturedHistory = null;

        _mockCurrencyRepository
            .Setup(r => r.GetByCodeAsync("EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        _mockCurrencyRepository
            .Setup(r => r.AddAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency c, CancellationToken _) => c);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .Callback<CurrencyHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.CreateCurrencyAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Created", capturedHistory.ChangeType);
        Assert.Null(capturedHistory.PreviousValues);
        Assert.NotEmpty(capturedHistory.NewValues);
        Assert.Equal("admin-user-1", capturedHistory.ChangedByUserId);
        Assert.Equal("admin@test.com", capturedHistory.ChangedByUserEmail);
    }

    #endregion

    #region UpdateCurrencyAsync Tests

    [Fact]
    public async Task UpdateCurrencyAsync_ValidCommand_UpdatesCurrency()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var existingCurrency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            Name = "Old Name",
            Symbol = "$",
            DecimalPlaces = 2,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateCurrencyCommand
        {
            Id = currencyId,
            Name = "US Dollar",
            Symbol = "$",
            DecimalPlaces = 2,
            UpdatedByUserId = "admin-user-2"
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCurrency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.UpdateCurrencyAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.Equal("US Dollar", result.Currency.Name);
        Assert.Equal("admin-user-2", result.Currency.UpdatedByUserId);
        _mockCurrencyRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateCurrencyAsync_NonExistingCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var command = new UpdateCurrencyCommand
        {
            Id = currencyId,
            Name = "US Dollar",
            Symbol = "$",
            UpdatedByUserId = "admin-user-1"
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        // Act
        var result = await service.UpdateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateCurrencyAsync_EmptyId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateCurrencyCommand
        {
            Id = Guid.Empty,
            Name = "US Dollar",
            Symbol = "$",
            UpdatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateCurrencyAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency ID is required"));
    }

    [Fact]
    public async Task UpdateCurrencyAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var existingCurrency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            Name = "Old Name",
            Symbol = "$",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateCurrencyCommand
        {
            Id = currencyId,
            Name = "US Dollar",
            Symbol = "$",
            UpdatedByUserId = "admin-user-2",
            UpdatedByUserEmail = "admin2@test.com"
        };

        CurrencyHistory? capturedHistory = null;

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCurrency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .Callback<CurrencyHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.UpdateCurrencyAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Updated", capturedHistory.ChangeType);
        Assert.NotNull(capturedHistory.PreviousValues);
        Assert.NotEmpty(capturedHistory.NewValues);
        Assert.Equal("admin-user-2", capturedHistory.ChangedByUserId);
        Assert.Equal("admin2@test.com", capturedHistory.ChangedByUserEmail);
    }

    #endregion

    #region EnableCurrencyAsync Tests

    [Fact]
    public async Task EnableCurrencyAsync_DisabledCurrency_EnablesCurrency()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            Name = "US Dollar",
            IsEnabled = false
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.EnableCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.True(result.Currency.IsEnabled);
        _mockCurrencyRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task EnableCurrencyAsync_AlreadyEnabled_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            IsEnabled = true
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Act
        var result = await service.EnableCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already enabled"));
    }

    [Fact]
    public async Task EnableCurrencyAsync_NonExistingCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        // Act
        var result = await service.EnableCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task EnableCurrencyAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.EnableCurrencyAsync(Guid.Empty, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency ID is required"));
    }

    [Fact]
    public async Task EnableCurrencyAsync_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();

        // Act
        var result = await service.EnableCurrencyAsync(currencyId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task EnableCurrencyAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            IsEnabled = false
        };

        CurrencyHistory? capturedHistory = null;

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .Callback<CurrencyHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.EnableCurrencyAsync(currencyId, "admin-user-1", "admin@test.com");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Enabled", capturedHistory.ChangeType);
        Assert.Equal("admin-user-1", capturedHistory.ChangedByUserId);
    }

    #endregion

    #region DisableCurrencyAsync Tests

    [Fact]
    public async Task DisableCurrencyAsync_EnabledCurrency_DisablesCurrency()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            Name = "Euro",
            IsEnabled = true,
            IsBaseCurrency = false
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.DisableCurrencyAsync(currencyId, "admin-user-1", null, "No longer needed");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.False(result.Currency.IsEnabled);
        _mockCurrencyRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task DisableCurrencyAsync_AlreadyDisabled_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            IsEnabled = false
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Act
        var result = await service.DisableCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already disabled"));
    }

    [Fact]
    public async Task DisableCurrencyAsync_BaseCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            IsEnabled = true,
            IsBaseCurrency = true
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Act
        var result = await service.DisableCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Cannot disable the base currency"));
    }

    [Fact]
    public async Task DisableCurrencyAsync_NonExistingCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        // Act
        var result = await service.DisableCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DisableCurrencyAsync_CreatesHistoryRecordWithReason()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            IsEnabled = true,
            IsBaseCurrency = false
        };

        CurrencyHistory? capturedHistory = null;

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .Callback<CurrencyHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.DisableCurrencyAsync(currencyId, "admin-user-1", "admin@test.com", "Currency no longer supported");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Disabled", capturedHistory.ChangeType);
        Assert.Equal("Currency no longer supported", capturedHistory.Reason);
    }

    #endregion

    #region SetBaseCurrencyAsync Tests

    [Fact]
    public async Task SetBaseCurrencyAsync_WithoutConfirmation_ReturnsConfirmationRequired()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            Name = "Euro",
            IsEnabled = true,
            IsBaseCurrency = false
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.GetBaseCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        // Act
        var result = await service.SetBaseCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.RequiresConfirmation);
        Assert.NotNull(result.WarningMessage);
        Assert.Contains("significant operation", result.WarningMessage);
    }

    [Fact]
    public async Task SetBaseCurrencyAsync_WithConfirmation_SetsBaseCurrency()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            Name = "Euro",
            IsEnabled = true,
            IsBaseCurrency = false
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.GetBaseCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.SetBaseCurrencyAsync(currencyId, "admin-user-1", null, "CONFIRM_BASE_CURRENCY_CHANGE");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.True(result.Currency.IsBaseCurrency);
        Assert.Null(result.PreviousBaseCurrency);
    }

    [Fact]
    public async Task SetBaseCurrencyAsync_WithExistingBaseCurrency_UpdatesBoth()
    {
        // Arrange
        var service = CreateService();
        var newBaseId = Guid.NewGuid();
        var oldBaseId = Guid.NewGuid();
        
        var newBaseCurrency = new Currency
        {
            Id = newBaseId,
            Code = "EUR",
            Name = "Euro",
            IsEnabled = true,
            IsBaseCurrency = false
        };
        
        var oldBaseCurrency = new Currency
        {
            Id = oldBaseId,
            Code = "USD",
            Name = "US Dollar",
            IsEnabled = true,
            IsBaseCurrency = true
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(newBaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newBaseCurrency);

        _mockCurrencyRepository
            .Setup(r => r.GetBaseCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldBaseCurrency);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.SetBaseCurrencyAsync(newBaseId, "admin-user-1", null, "CONFIRM_BASE_CURRENCY_CHANGE");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Currency);
        Assert.True(result.Currency.IsBaseCurrency);
        Assert.NotNull(result.PreviousBaseCurrency);
        Assert.Equal("USD", result.PreviousBaseCurrency.Code);
        
        // Verify old base currency was updated
        _mockCurrencyRepository.Verify(r => r.UpdateAsync(
            It.Is<Currency>(c => c.Id == oldBaseId && !c.IsBaseCurrency), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetBaseCurrencyAsync_AlreadyBaseCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            IsEnabled = true,
            IsBaseCurrency = true
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Act
        var result = await service.SetBaseCurrencyAsync(currencyId, "admin-user-1", null, "CONFIRM_BASE_CURRENCY_CHANGE");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already the base currency"));
    }

    [Fact]
    public async Task SetBaseCurrencyAsync_DisabledCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            IsEnabled = false,
            IsBaseCurrency = false
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Act
        var result = await service.SetBaseCurrencyAsync(currencyId, "admin-user-1", null, "CONFIRM_BASE_CURRENCY_CHANGE");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Cannot set a disabled currency as base"));
    }

    [Fact]
    public async Task SetBaseCurrencyAsync_NonExistingCurrency_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        // Act
        var result = await service.SetBaseCurrencyAsync(currencyId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task SetBaseCurrencyAsync_CreatesHistoryRecordWithSetAsBase()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "EUR",
            Name = "Euro",
            IsEnabled = true,
            IsBaseCurrency = false
        };

        CurrencyHistory? capturedHistory = null;

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockCurrencyRepository
            .Setup(r => r.GetBaseCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        _mockCurrencyRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<CurrencyHistory>(), It.IsAny<CancellationToken>()))
            .Callback<CurrencyHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((CurrencyHistory h, CancellationToken _) => h);

        // Act
        var result = await service.SetBaseCurrencyAsync(currencyId, "admin-user-1", "admin@test.com", "CONFIRM_BASE_CURRENCY_CHANGE");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("SetAsBase", capturedHistory.ChangeType);
        Assert.Equal("admin-user-1", capturedHistory.ChangedByUserId);
    }

    #endregion

    #region GetCurrencyHistoryAsync Tests

    [Fact]
    public async Task GetCurrencyHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var currency = new Currency
        {
            Id = currencyId,
            Code = "USD",
            Name = "US Dollar"
        };
        var history = new List<CurrencyHistory>
        {
            new CurrencyHistory
            {
                Id = Guid.NewGuid(),
                CurrencyId = currencyId,
                ChangeType = "Created",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ChangedByUserId = "admin-1"
            },
            new CurrencyHistory
            {
                Id = Guid.NewGuid(),
                CurrencyId = currencyId,
                ChangeType = "Updated",
                PreviousValues = "{}",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-15),
                ChangedByUserId = "admin-2"
            }
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        _mockHistoryRepository
            .Setup(r => r.GetByCurrencyIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetCurrencyHistoryAsync(currencyId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.History.Count);
        Assert.NotNull(result.Currency);
        Assert.Equal(currencyId, result.Currency.Id);
    }

    [Fact]
    public async Task GetCurrencyHistoryAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetCurrencyHistoryAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Currency ID is required"));
    }

    [Fact]
    public async Task GetCurrencyHistoryAsync_DeletedCurrency_ReturnsHistoryWithoutCurrency()
    {
        // Arrange
        var service = CreateService();
        var currencyId = Guid.NewGuid();
        var history = new List<CurrencyHistory>
        {
            new CurrencyHistory
            {
                Id = Guid.NewGuid(),
                CurrencyId = currencyId,
                ChangeType = "Created",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ChangedByUserId = "admin-1"
            }
        };

        _mockCurrencyRepository
            .Setup(r => r.GetByIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);

        _mockHistoryRepository
            .Setup(r => r.GetByCurrencyIdAsync(currencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetCurrencyHistoryAsync(currencyId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.History);
        Assert.Null(result.Currency);
    }

    #endregion
}
