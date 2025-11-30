using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class VatRuleManagementServiceTests
{
    private readonly Mock<IVatRuleRepository> _mockVatRuleRepository;
    private readonly Mock<IVatRuleHistoryRepository> _mockHistoryRepository;
    private readonly Mock<ILogger<VatRuleManagementService>> _mockLogger;

    public VatRuleManagementServiceTests()
    {
        _mockVatRuleRepository = new Mock<IVatRuleRepository>(MockBehavior.Strict);
        _mockHistoryRepository = new Mock<IVatRuleHistoryRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<VatRuleManagementService>>();
    }

    private VatRuleManagementService CreateService()
    {
        return new VatRuleManagementService(
            _mockVatRuleRepository.Object,
            _mockHistoryRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullVatRuleRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VatRuleManagementService(null!, _mockHistoryRepository.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHistoryRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VatRuleManagementService(_mockVatRuleRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VatRuleManagementService(_mockVatRuleRepository.Object, _mockHistoryRepository.Object, null!));
    }

    #endregion

    #region GetAllRulesAsync Tests

    [Fact]
    public async Task GetAllRulesAsync_ReturnsAllRules()
    {
        // Arrange
        var service = CreateService();
        var rules = new List<VatRule>
        {
            new VatRule
            {
                Id = Guid.NewGuid(),
                Name = "Germany Standard",
                CountryCode = "DE",
                TaxRate = 19.0m,
                IsActive = true,
                EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new VatRule
            {
                Id = Guid.NewGuid(),
                Name = "France Standard",
                CountryCode = "FR",
                TaxRate = 20.0m,
                IsActive = true,
                EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-15)
            }
        };

        _mockVatRuleRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        // Act
        var result = await service.GetAllRulesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Rules.Count);
        _mockVatRuleRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllRulesAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockVatRuleRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VatRule>());

        // Act
        var result = await service.GetAllRulesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Rules);
    }

    #endregion

    #region GetRuleByIdAsync Tests

    [Fact]
    public async Task GetRuleByIdAsync_ExistingRule_ReturnsRule()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var rule = new VatRule
        {
            Id = ruleId,
            Name = "Germany Standard",
            CountryCode = "DE",
            TaxRate = 19.0m,
            IsActive = true
        };

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await service.GetRuleByIdAsync(ruleId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal(ruleId, result.Rule.Id);
        Assert.Equal("Germany Standard", result.Rule.Name);
    }

    [Fact]
    public async Task GetRuleByIdAsync_NonExistingRule_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule?)null);

        // Act
        var result = await service.GetRuleByIdAsync(ruleId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task GetRuleByIdAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetRuleByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("VAT rule ID is required"));
    }

    #endregion

    #region CreateRuleAsync Tests

    [Fact]
    public async Task CreateRuleAsync_ValidCommand_CreatesRule()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "Germany Standard",
            CountryCode = "DE",
            TaxRate = 19.0m,
            Priority = 5,
            EffectiveFrom = DateTimeOffset.UtcNow.Date,
            IsActive = true,
            CreatedByUserId = "admin-user-1"
        };

        _mockVatRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule rule, CancellationToken _) => rule);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal("Germany Standard", result.Rule.Name);
        Assert.Equal("DE", result.Rule.CountryCode);
        Assert.Equal(19.0m, result.Rule.TaxRate);
        Assert.Equal("admin-user-1", result.Rule.CreatedByUserId);
        _mockVatRuleRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateRuleAsync_WithCategoryId_CreatesRule()
    {
        // Arrange
        var service = CreateService();
        var categoryId = Guid.NewGuid();
        var command = new CreateVatRuleCommand
        {
            Name = "Germany Reduced Rate",
            CountryCode = "DE",
            TaxRate = 7.0m,
            CategoryId = categoryId,
            Priority = 10,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(7).Date,
            IsActive = true,
            CreatedByUserId = "admin-user-1"
        };

        _mockVatRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule rule, CancellationToken _) => rule);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal(categoryId, result.Rule.CategoryId);
    }

    [Fact]
    public async Task CreateRuleAsync_EmptyName_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "",
            CountryCode = "DE",
            TaxRate = 19.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Rule name is required"));
    }

    [Fact]
    public async Task CreateRuleAsync_InvalidCountryCode_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "Invalid Country",
            CountryCode = "INVALID",
            TaxRate = 19.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("2-letter"));
    }

    [Fact]
    public async Task CreateRuleAsync_InvalidTaxRate_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "Invalid Rate",
            CountryCode = "DE",
            TaxRate = 150.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Tax rate must be between 0 and 100"));
    }

    [Fact]
    public async Task CreateRuleAsync_NegativeTaxRate_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "Negative Rate",
            CountryCode = "DE",
            TaxRate = -5.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Tax rate must be between 0 and 100"));
    }

    [Fact]
    public async Task CreateRuleAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "No User Rule",
            CountryCode = "DE",
            TaxRate = 19.0m,
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task CreateRuleAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "Test Rule",
            CountryCode = "DE",
            TaxRate = 19.0m,
            EffectiveFrom = DateTimeOffset.UtcNow.Date,
            CreatedByUserId = "admin-user-1",
            CreatedByUserEmail = "admin@test.com"
        };

        VatRuleHistory? capturedHistory = null;

        _mockVatRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule rule, CancellationToken _) => rule);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .Callback<VatRuleHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        // Act
        var result = await service.CreateRuleAsync(command);

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

    #region UpdateRuleAsync Tests

    [Fact]
    public async Task UpdateRuleAsync_ValidCommand_UpdatesRule()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new VatRule
        {
            Id = ruleId,
            Name = "Old Name",
            CountryCode = "DE",
            TaxRate = 19.0m,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateVatRuleCommand
        {
            Id = ruleId,
            Name = "Updated Name",
            CountryCode = "DE",
            TaxRate = 21.0m,
            Priority = 10,
            EffectiveFrom = DateTimeOffset.UtcNow.Date,
            IsActive = true,
            UpdatedByUserId = "admin-user-2"
        };

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRule);

        _mockVatRuleRepository
            .Setup(r => r.UpdateAsync(It.IsAny<VatRule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal("Updated Name", result.Rule.Name);
        Assert.Equal(21.0m, result.Rule.TaxRate);
        Assert.Equal("admin-user-2", result.Rule.UpdatedByUserId);
        _mockVatRuleRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateRuleAsync_NonExistingRule_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var command = new UpdateVatRuleCommand
        {
            Id = ruleId,
            Name = "Updated Name",
            CountryCode = "DE",
            TaxRate = 19.0m,
            UpdatedByUserId = "admin-user-1"
        };

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule?)null);

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateRuleAsync_EmptyId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateVatRuleCommand
        {
            Id = Guid.Empty,
            Name = "Updated Name",
            CountryCode = "DE",
            TaxRate = 19.0m,
            UpdatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("VAT rule ID is required"));
    }

    [Fact]
    public async Task UpdateRuleAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new VatRule
        {
            Id = ruleId,
            Name = "Old Name",
            CountryCode = "DE",
            TaxRate = 19.0m,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateVatRuleCommand
        {
            Id = ruleId,
            Name = "Updated Name",
            CountryCode = "DE",
            TaxRate = 21.0m,
            EffectiveFrom = DateTimeOffset.UtcNow.Date,
            UpdatedByUserId = "admin-user-2",
            UpdatedByUserEmail = "admin2@test.com"
        };

        VatRuleHistory? capturedHistory = null;

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRule);

        _mockVatRuleRepository
            .Setup(r => r.UpdateAsync(It.IsAny<VatRule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .Callback<VatRuleHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        // Act
        var result = await service.UpdateRuleAsync(command);

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

    #region DeleteRuleAsync Tests

    [Fact]
    public async Task DeleteRuleAsync_ExistingRule_DeletesRule()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new VatRule
        {
            Id = ruleId,
            Name = "Rule to Delete",
            CountryCode = "DE",
            TaxRate = 19.0m
        };

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRule);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        _mockVatRuleRepository
            .Setup(r => r.DeleteAsync(ruleId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteRuleAsync(ruleId, "admin-user-1");

        // Assert
        Assert.True(result.Succeeded);
        _mockVatRuleRepository.VerifyAll();
        _mockHistoryRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteRuleAsync_NonExistingRule_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule?)null);

        // Act
        var result = await service.DeleteRuleAsync(ruleId, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeleteRuleAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteRuleAsync(Guid.Empty, "admin-user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("VAT rule ID is required"));
    }

    [Fact]
    public async Task DeleteRuleAsync_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();

        // Act
        var result = await service.DeleteRuleAsync(ruleId, "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    [Fact]
    public async Task DeleteRuleAsync_CreatesHistoryRecord()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new VatRule
        {
            Id = ruleId,
            Name = "Rule to Delete",
            CountryCode = "DE",
            TaxRate = 19.0m
        };

        VatRuleHistory? capturedHistory = null;

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRule);

        _mockHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<VatRuleHistory>(), It.IsAny<CancellationToken>()))
            .Callback<VatRuleHistory, CancellationToken>((h, _) => capturedHistory = h)
            .ReturnsAsync((VatRuleHistory history, CancellationToken _) => history);

        _mockVatRuleRepository
            .Setup(r => r.DeleteAsync(ruleId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteRuleAsync(ruleId, "admin-user-1", "admin@test.com");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedHistory);
        Assert.Equal("Deleted", capturedHistory.ChangeType);
        Assert.NotNull(capturedHistory.PreviousValues);
        Assert.Equal("{}", capturedHistory.NewValues);
        Assert.Equal("admin-user-1", capturedHistory.ChangedByUserId);
        Assert.Equal("admin@test.com", capturedHistory.ChangedByUserEmail);
    }

    #endregion

    #region GetApplicableRateAsync Tests

    [Fact]
    public async Task GetApplicableRateAsync_FindsCountryRule()
    {
        // Arrange
        var service = CreateService();
        var rule = new VatRule
        {
            Id = Guid.NewGuid(),
            Name = "Germany Standard",
            CountryCode = "DE",
            TaxRate = 19.0m,
            IsActive = true
        };

        _mockVatRuleRepository
            .Setup(r => r.GetActiveByCountryAsync("DE", null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await service.GetApplicableRateAsync("DE", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(19.0m, result.TaxRate);
        Assert.NotNull(result.AppliedRule);
        Assert.Equal("Germany Standard", result.AppliedRule.Name);
    }

    [Fact]
    public async Task GetApplicableRateAsync_FindsCategorySpecificRule()
    {
        // Arrange
        var service = CreateService();
        var categoryId = Guid.NewGuid();
        var categoryRule = new VatRule
        {
            Id = Guid.NewGuid(),
            Name = "Germany Reduced",
            CountryCode = "DE",
            TaxRate = 7.0m,
            CategoryId = categoryId,
            IsActive = true
        };

        _mockVatRuleRepository
            .Setup(r => r.GetActiveByCountryAsync("DE", categoryId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryRule);

        // Act
        var result = await service.GetApplicableRateAsync("DE", categoryId, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(7.0m, result.TaxRate);
        Assert.NotNull(result.AppliedRule);
        Assert.Equal(categoryId, result.AppliedRule.CategoryId);
    }

    [Fact]
    public async Task GetApplicableRateAsync_FallsBackToCountryRule()
    {
        // Arrange
        var service = CreateService();
        var categoryId = Guid.NewGuid();
        var countryRule = new VatRule
        {
            Id = Guid.NewGuid(),
            Name = "Germany Standard",
            CountryCode = "DE",
            TaxRate = 19.0m,
            IsActive = true
        };

        _mockVatRuleRepository
            .Setup(r => r.GetActiveByCountryAsync("DE", categoryId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule?)null);

        _mockVatRuleRepository
            .Setup(r => r.GetActiveByCountryAsync("DE", null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countryRule);

        // Act
        var result = await service.GetApplicableRateAsync("DE", categoryId, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(19.0m, result.TaxRate);
        Assert.NotNull(result.AppliedRule);
        Assert.Null(result.AppliedRule.CategoryId);
    }

    [Fact]
    public async Task GetApplicableRateAsync_NoRuleFound_ReturnsNoRateFound()
    {
        // Arrange
        var service = CreateService();

        _mockVatRuleRepository
            .Setup(r => r.GetActiveByCountryAsync("XX", null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule?)null);

        // Act
        var result = await service.GetApplicableRateAsync("XX", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.TaxRate);
        Assert.Null(result.AppliedRule);
    }

    [Fact]
    public async Task GetApplicableRateAsync_EmptyCountryCode_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetApplicableRateAsync("", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Country code is required"));
    }

    [Fact]
    public async Task GetApplicableRateAsync_InvalidCountryCode_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetApplicableRateAsync("INVALID", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("2-letter"));
    }

    #endregion

    #region GetRuleHistoryAsync Tests

    [Fact]
    public async Task GetRuleHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var rule = new VatRule
        {
            Id = ruleId,
            Name = "Germany Standard",
            CountryCode = "DE",
            TaxRate = 19.0m
        };
        var history = new List<VatRuleHistory>
        {
            new VatRuleHistory
            {
                Id = Guid.NewGuid(),
                VatRuleId = ruleId,
                ChangeType = "Created",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ChangedByUserId = "admin-1"
            },
            new VatRuleHistory
            {
                Id = Guid.NewGuid(),
                VatRuleId = ruleId,
                ChangeType = "Updated",
                PreviousValues = "{}",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-15),
                ChangedByUserId = "admin-2"
            }
        };

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockHistoryRepository
            .Setup(r => r.GetByVatRuleIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetRuleHistoryAsync(ruleId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.History.Count);
        Assert.NotNull(result.Rule);
        Assert.Equal(ruleId, result.Rule.Id);
    }

    [Fact]
    public async Task GetRuleHistoryAsync_EmptyId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetRuleHistoryAsync(Guid.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("VAT rule ID is required"));
    }

    [Fact]
    public async Task GetRuleHistoryAsync_DeletedRule_ReturnsHistoryWithoutRule()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var history = new List<VatRuleHistory>
        {
            new VatRuleHistory
            {
                Id = Guid.NewGuid(),
                VatRuleId = ruleId,
                ChangeType = "Created",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ChangedByUserId = "admin-1"
            },
            new VatRuleHistory
            {
                Id = Guid.NewGuid(),
                VatRuleId = ruleId,
                ChangeType = "Deleted",
                PreviousValues = "{}",
                NewValues = "{}",
                ChangedAt = DateTimeOffset.UtcNow.AddDays(-1),
                ChangedByUserId = "admin-2"
            }
        };

        _mockVatRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatRule?)null);

        _mockHistoryRepository
            .Setup(r => r.GetByVatRuleIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await service.GetRuleHistoryAsync(ruleId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.History.Count);
        Assert.Null(result.Rule);
    }

    #endregion

    #region Date Validation Tests

    [Fact]
    public async Task CreateRuleAsync_EffectiveToBeforeEffectiveFrom_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateVatRuleCommand
        {
            Name = "Invalid Dates",
            CountryCode = "DE",
            TaxRate = 19.0m,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(30),
            EffectiveTo = DateTimeOffset.UtcNow.AddDays(10),
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Effective end date must be after effective start date"));
    }

    [Fact]
    public async Task UpdateRuleAsync_EffectiveToBeforeEffectiveFrom_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateVatRuleCommand
        {
            Id = Guid.NewGuid(),
            Name = "Invalid Dates",
            CountryCode = "DE",
            TaxRate = 19.0m,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(30),
            EffectiveTo = DateTimeOffset.UtcNow.AddDays(10),
            UpdatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Effective end date must be after effective start date"));
    }

    #endregion
}
