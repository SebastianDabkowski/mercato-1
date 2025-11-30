using Mercato.Admin.Application.Services;
using Mercato.Admin.Infrastructure;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class CommissionRuleManagementServiceTests
{
    private readonly Mock<ICommissionRuleRepository> _mockRuleRepository;
    private readonly Mock<ILogger<CommissionRuleManagementService>> _mockLogger;

    public CommissionRuleManagementServiceTests()
    {
        _mockRuleRepository = new Mock<ICommissionRuleRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<CommissionRuleManagementService>>();
    }

    private CommissionRuleManagementService CreateService()
    {
        return new CommissionRuleManagementService(
            _mockRuleRepository.Object,
            _mockLogger.Object);
    }

    #region GetAllRulesAsync Tests

    [Fact]
    public async Task GetAllRulesAsync_ReturnsAllRules()
    {
        // Arrange
        var service = CreateService();
        var rules = new List<CommissionRule>
        {
            new CommissionRule
            {
                Id = Guid.NewGuid(),
                Name = "Global Default",
                CommissionRate = 10.0m,
                IsActive = true,
                EffectiveDate = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new CommissionRule
            {
                Id = Guid.NewGuid(),
                Name = "Electronics Category",
                CategoryId = "electronics",
                CommissionRate = 15.0m,
                IsActive = true,
                EffectiveDate = DateTimeOffset.UtcNow.AddDays(-15)
            }
        };

        _mockRuleRepository
            .Setup(r => r.GetAllRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await service.GetAllRulesAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Rules.Count);
        _mockRuleRepository.VerifyAll();
    }

    [Fact]
    public async Task GetAllRulesAsync_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockRuleRepository
            .Setup(r => r.GetAllRulesAsync())
            .ReturnsAsync(new List<CommissionRule>());

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
        var rule = new CommissionRule
        {
            Id = ruleId,
            Name = "Test Rule",
            CommissionRate = 12.5m,
            IsActive = true
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync(rule);

        // Act
        var result = await service.GetRuleByIdAsync(ruleId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal(ruleId, result.Rule.Id);
        Assert.Equal("Test Rule", result.Rule.Name);
    }

    [Fact]
    public async Task GetRuleByIdAsync_NonExistingRule_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync((CommissionRule?)null);

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
        Assert.Contains(result.Errors, e => e.Contains("Rule ID is required"));
    }

    #endregion

    #region CreateRuleAsync Tests

    [Fact]
    public async Task CreateRuleAsync_ValidCommand_CreatesRule()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "New Rule",
            CommissionRate = 10.0m,
            FixedFee = 0.50m,
            Priority = 5,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            IsActive = true,
            CreatedByUserId = "admin-user-1"
        };

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, null))
            .ReturnsAsync(new List<CommissionRule>());

        _mockRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionRule>()))
            .ReturnsAsync((CommissionRule rule) => rule);

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal("New Rule", result.Rule.Name);
        Assert.Equal(10.0m, result.Rule.CommissionRate);
        Assert.Equal(0.50m, result.Rule.FixedFee);
        Assert.Equal("admin-user-1", result.Rule.CreatedByUserId);
        Assert.Equal(1, result.Rule.Version);
        _mockRuleRepository.VerifyAll();
    }

    [Fact]
    public async Task CreateRuleAsync_WithSellerAndCategory_CreatesRule()
    {
        // Arrange
        var service = CreateService();
        var sellerId = Guid.NewGuid();
        var command = new CreateCommissionRuleCommand
        {
            Name = "Seller Category Rule",
            SellerId = sellerId,
            CategoryId = "electronics",
            CommissionRate = 8.0m,
            Priority = 10,
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(7).Date,
            IsActive = true,
            CreatedByUserId = "admin-user-1"
        };

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(sellerId, "electronics", command.EffectiveDate, null))
            .ReturnsAsync(new List<CommissionRule>());

        _mockRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionRule>()))
            .ReturnsAsync((CommissionRule rule) => rule);

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal(sellerId, result.Rule.SellerId);
        Assert.Equal("electronics", result.Rule.CategoryId);
    }

    [Fact]
    public async Task CreateRuleAsync_WithConflict_ReturnsConflictFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "Conflicting Rule",
            CommissionRate = 10.0m,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            CreatedByUserId = "admin-user-1"
        };

        var conflictingRule = new CommissionRule
        {
            Id = Guid.NewGuid(),
            Name = "Existing Global Rule",
            CommissionRate = 12.0m,
            EffectiveDate = command.EffectiveDate
        };

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, null))
            .ReturnsAsync(new List<CommissionRule> { conflictingRule });

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.ConflictingRules);
        Assert.Contains(result.Errors, e => e.Contains("Conflicting"));
    }

    [Fact]
    public async Task CreateRuleAsync_EmptyName_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "",
            CommissionRate = 10.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Rule name is required"));
    }

    [Fact]
    public async Task CreateRuleAsync_InvalidCommissionRate_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "Invalid Rate Rule",
            CommissionRate = 150.0m, // Over 100%
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Commission rate must be between 0 and 100"));
    }

    [Fact]
    public async Task CreateRuleAsync_NegativeFixedFee_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "Negative Fee Rule",
            CommissionRate = 10.0m,
            FixedFee = -5.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Fixed fee cannot be negative"));
    }

    [Fact]
    public async Task CreateRuleAsync_MinGreaterThanMax_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "Invalid Min Max Rule",
            CommissionRate = 10.0m,
            MinCommission = 50.0m,
            MaxCommission = 20.0m,
            CreatedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Minimum commission cannot be greater than maximum"));
    }

    [Fact]
    public async Task CreateRuleAsync_MissingUserId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateCommissionRuleCommand
        {
            Name = "No User Rule",
            CommissionRate = 10.0m,
            CreatedByUserId = ""
        };

        // Act
        var result = await service.CreateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("User ID is required"));
    }

    #endregion

    #region UpdateRuleAsync Tests

    [Fact]
    public async Task UpdateRuleAsync_ValidCommand_UpdatesRule()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new CommissionRule
        {
            Id = ruleId,
            Name = "Old Name",
            CommissionRate = 10.0m,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedByUserId = "original-admin"
        };

        var command = new UpdateCommissionRuleCommand
        {
            Id = ruleId,
            Name = "Updated Name",
            CommissionRate = 12.5m,
            FixedFee = 1.0m,
            Priority = 10,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            IsActive = true,
            ModifiedByUserId = "admin-user-2"
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync(existingRule);

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, ruleId))
            .ReturnsAsync(new List<CommissionRule>());

        _mockRuleRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionRule>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal("Updated Name", result.Rule.Name);
        Assert.Equal(12.5m, result.Rule.CommissionRate);
        Assert.Equal(1.0m, result.Rule.FixedFee);
        Assert.Equal(2, result.Rule.Version); // Version incremented
        Assert.Equal("admin-user-2", result.Rule.LastModifiedByUserId);
        _mockRuleRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateRuleAsync_NonExistingRule_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var command = new UpdateCommissionRuleCommand
        {
            Id = ruleId,
            Name = "Updated Name",
            CommissionRate = 10.0m,
            ModifiedByUserId = "admin-user-1"
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync((CommissionRule?)null);

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task UpdateRuleAsync_WithConflict_ReturnsConflictFailure()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new CommissionRule
        {
            Id = ruleId,
            Name = "Rule to Update",
            CommissionRate = 10.0m,
            Version = 1
        };

        var command = new UpdateCommissionRuleCommand
        {
            Id = ruleId,
            Name = "Updated Rule",
            CommissionRate = 10.0m,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            ModifiedByUserId = "admin-user-1"
        };

        var conflictingRule = new CommissionRule
        {
            Id = Guid.NewGuid(),
            Name = "Conflicting Rule",
            CommissionRate = 12.0m,
            EffectiveDate = command.EffectiveDate
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync(existingRule);

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, ruleId))
            .ReturnsAsync(new List<CommissionRule> { conflictingRule });

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.ConflictingRules);
    }

    [Fact]
    public async Task UpdateRuleAsync_EmptyId_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateCommissionRuleCommand
        {
            Id = Guid.Empty,
            Name = "Updated Name",
            CommissionRate = 10.0m,
            ModifiedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Rule ID is required"));
    }

    [Fact]
    public async Task UpdateRuleAsync_NameTooLong_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var command = new UpdateCommissionRuleCommand
        {
            Id = Guid.NewGuid(),
            Name = new string('A', 250), // Over 200 characters
            CommissionRate = 10.0m,
            ModifiedByUserId = "admin-user-1"
        };

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("must not exceed 200 characters"));
    }

    #endregion

    #region Version Tracking Tests

    [Fact]
    public async Task UpdateRuleAsync_IncrementsVersion()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var existingRule = new CommissionRule
        {
            Id = ruleId,
            Name = "Original",
            CommissionRate = 10.0m,
            Version = 5
        };

        var command = new UpdateCommissionRuleCommand
        {
            Id = ruleId,
            Name = "Updated",
            CommissionRate = 12.0m,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            ModifiedByUserId = "admin-user-1"
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync(existingRule);

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, ruleId))
            .ReturnsAsync(new List<CommissionRule>());

        _mockRuleRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionRule>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateRuleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(6, result.Rule!.Version);
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public async Task CreateRuleAsync_SetsAuditFields()
    {
        // Arrange
        var service = CreateService();
        var beforeCreate = DateTimeOffset.UtcNow;
        var command = new CreateCommissionRuleCommand
        {
            Name = "Audit Test Rule",
            CommissionRate = 10.0m,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            CreatedByUserId = "test-admin"
        };

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, null))
            .ReturnsAsync(new List<CommissionRule>());

        CommissionRule? addedRule = null;
        _mockRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<CommissionRule>()))
            .Callback<CommissionRule>(rule => addedRule = rule)
            .ReturnsAsync((CommissionRule rule) => rule);

        // Act
        var result = await service.CreateRuleAsync(command);
        var afterCreate = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(addedRule);
        Assert.Equal("test-admin", addedRule.CreatedByUserId);
        Assert.Equal("test-admin", addedRule.LastModifiedByUserId);
        Assert.True(addedRule.CreatedAt >= beforeCreate && addedRule.CreatedAt <= afterCreate);
        Assert.True(addedRule.LastUpdatedAt >= beforeCreate && addedRule.LastUpdatedAt <= afterCreate);
        Assert.Equal(1, addedRule.Version);
    }

    [Fact]
    public async Task UpdateRuleAsync_UpdatesAuditFields()
    {
        // Arrange
        var service = CreateService();
        var ruleId = Guid.NewGuid();
        var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var existingRule = new CommissionRule
        {
            Id = ruleId,
            Name = "Original Rule",
            CommissionRate = 10.0m,
            Version = 1,
            CreatedAt = originalCreatedAt,
            LastUpdatedAt = originalCreatedAt,
            CreatedByUserId = "original-admin",
            LastModifiedByUserId = "original-admin"
        };

        var command = new UpdateCommissionRuleCommand
        {
            Id = ruleId,
            Name = "Updated Rule",
            CommissionRate = 15.0m,
            EffectiveDate = DateTimeOffset.UtcNow.Date,
            ModifiedByUserId = "updating-admin"
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(ruleId))
            .ReturnsAsync(existingRule);

        _mockRuleRepository
            .Setup(r => r.GetConflictingRulesAsync(null, null, command.EffectiveDate, ruleId))
            .ReturnsAsync(new List<CommissionRule>());

        _mockRuleRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommissionRule>()))
            .Returns(Task.CompletedTask);

        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        var result = await service.UpdateRuleAsync(command);
        var afterUpdate = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Rule);
        Assert.Equal("original-admin", result.Rule.CreatedByUserId); // Preserved
        Assert.Equal("updating-admin", result.Rule.LastModifiedByUserId); // Updated
        Assert.Equal(originalCreatedAt, result.Rule.CreatedAt); // Preserved
        Assert.True(result.Rule.LastUpdatedAt >= beforeUpdate && result.Rule.LastUpdatedAt <= afterUpdate);
    }

    #endregion
}
