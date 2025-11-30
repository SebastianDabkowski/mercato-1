using Mercato.Payments.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing commission rules from the admin panel.
/// </summary>
public interface ICommissionRuleManagementService
{
    /// <summary>
    /// Gets all commission rules for display in the admin panel.
    /// </summary>
    /// <returns>The result containing all commission rules.</returns>
    Task<GetCommissionRulesResult> GetAllRulesAsync();

    /// <summary>
    /// Gets a specific commission rule by ID.
    /// </summary>
    /// <param name="id">The commission rule identifier.</param>
    /// <returns>The result containing the commission rule if found.</returns>
    Task<GetCommissionRuleResult> GetRuleByIdAsync(Guid id);

    /// <summary>
    /// Creates a new commission rule.
    /// </summary>
    /// <param name="command">The command containing rule details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateCommissionRuleResult> CreateRuleAsync(CreateCommissionRuleCommand command);

    /// <summary>
    /// Updates an existing commission rule.
    /// </summary>
    /// <param name="command">The command containing updated rule details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCommissionRuleResult> UpdateRuleAsync(UpdateCommissionRuleCommand command);
}

/// <summary>
/// Result of getting all commission rules.
/// </summary>
public class GetCommissionRulesResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the commission rules.
    /// </summary>
    public IReadOnlyList<CommissionRule> Rules { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with commission rules.
    /// </summary>
    /// <param name="rules">The commission rules.</param>
    /// <returns>A successful result.</returns>
    public static GetCommissionRulesResult Success(IReadOnlyList<CommissionRule> rules) => new()
    {
        Succeeded = true,
        Errors = [],
        Rules = rules
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionRulesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionRulesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCommissionRulesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single commission rule.
/// </summary>
public class GetCommissionRuleResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the commission rule.
    /// </summary>
    public CommissionRule? Rule { get; private init; }

    /// <summary>
    /// Creates a successful result with a commission rule.
    /// </summary>
    /// <param name="rule">The commission rule.</param>
    /// <returns>A successful result.</returns>
    public static GetCommissionRuleResult Success(CommissionRule rule) => new()
    {
        Succeeded = true,
        Errors = [],
        Rule = rule
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCommissionRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new commission rule.
/// </summary>
public class CreateCommissionRuleCommand
{
    /// <summary>
    /// Gets or sets the name of the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID this rule applies to (null for global/category rules).
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the category ID this rule applies to (null for global/seller rules).
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the commission rate as a percentage.
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Gets or sets the fixed fee amount.
    /// </summary>
    public decimal FixedFee { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum commission amount.
    /// </summary>
    public decimal? MinCommission { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum commission amount.
    /// </summary>
    public decimal? MaxCommission { get; set; }

    /// <summary>
    /// Gets or sets the priority of this rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the effective date when this rule becomes applicable.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID of the admin creating this rule.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of creating a commission rule.
/// </summary>
public class CreateCommissionRuleResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the created commission rule.
    /// </summary>
    public CommissionRule? Rule { get; private init; }

    /// <summary>
    /// Gets the list of conflicting rules found during validation.
    /// </summary>
    public IReadOnlyList<CommissionRule> ConflictingRules { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with the created rule.
    /// </summary>
    /// <param name="rule">The created commission rule.</param>
    /// <returns>A successful result.</returns>
    public static CreateCommissionRuleResult Success(CommissionRule rule) => new()
    {
        Succeeded = true,
        Errors = [],
        Rule = rule
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateCommissionRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateCommissionRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a failed result with conflicting rules.
    /// </summary>
    /// <param name="conflictingRules">The list of conflicting rules.</param>
    /// <returns>A failed result with conflicts.</returns>
    public static CreateCommissionRuleResult ConflictFailure(IReadOnlyList<CommissionRule> conflictingRules) => new()
    {
        Succeeded = false,
        Errors = ["Conflicting commission rules found. Please resolve the overlapping configurations."],
        ConflictingRules = conflictingRules
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateCommissionRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing commission rule.
/// </summary>
public class UpdateCommissionRuleCommand
{
    /// <summary>
    /// Gets or sets the ID of the rule to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID this rule applies to (null for global/category rules).
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the category ID this rule applies to (null for global/seller rules).
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the commission rate as a percentage.
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Gets or sets the fixed fee amount.
    /// </summary>
    public decimal FixedFee { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum commission amount.
    /// </summary>
    public decimal? MinCommission { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum commission amount.
    /// </summary>
    public decimal? MaxCommission { get; set; }

    /// <summary>
    /// Gets or sets the priority of this rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the effective date when this rule becomes applicable.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the admin updating this rule.
    /// </summary>
    public string ModifiedByUserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of updating a commission rule.
/// </summary>
public class UpdateCommissionRuleResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the updated commission rule.
    /// </summary>
    public CommissionRule? Rule { get; private init; }

    /// <summary>
    /// Gets the list of conflicting rules found during validation.
    /// </summary>
    public IReadOnlyList<CommissionRule> ConflictingRules { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with the updated rule.
    /// </summary>
    /// <param name="rule">The updated commission rule.</param>
    /// <returns>A successful result.</returns>
    public static UpdateCommissionRuleResult Success(CommissionRule rule) => new()
    {
        Succeeded = true,
        Errors = [],
        Rule = rule
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateCommissionRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateCommissionRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a failed result with conflicting rules.
    /// </summary>
    /// <param name="conflictingRules">The list of conflicting rules.</param>
    /// <returns>A failed result with conflicts.</returns>
    public static UpdateCommissionRuleResult ConflictFailure(IReadOnlyList<CommissionRule> conflictingRules) => new()
    {
        Succeeded = false,
        Errors = ["Conflicting commission rules found. Please resolve the overlapping configurations."],
        ConflictingRules = conflictingRules
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateCommissionRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
