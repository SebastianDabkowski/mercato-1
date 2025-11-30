using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing VAT rules from the admin panel.
/// </summary>
public interface IVatRuleManagementService
{
    /// <summary>
    /// Gets all VAT rules.
    /// </summary>
    /// <returns>The result containing all VAT rules.</returns>
    Task<GetVatRulesResult> GetAllRulesAsync();

    /// <summary>
    /// Gets a specific VAT rule by ID.
    /// </summary>
    /// <param name="id">The VAT rule identifier.</param>
    /// <returns>The result containing the VAT rule if found.</returns>
    Task<GetVatRuleResult> GetRuleByIdAsync(Guid id);

    /// <summary>
    /// Creates a new VAT rule.
    /// </summary>
    /// <param name="command">The command containing rule details.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<CreateVatRuleResult> CreateRuleAsync(CreateVatRuleCommand command);

    /// <summary>
    /// Updates an existing VAT rule.
    /// </summary>
    /// <param name="command">The command containing updated rule details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateVatRuleResult> UpdateRuleAsync(UpdateVatRuleCommand command);

    /// <summary>
    /// Deletes a VAT rule.
    /// </summary>
    /// <param name="id">The VAT rule ID to delete.</param>
    /// <param name="deletedByUserId">The user ID performing the deletion.</param>
    /// <param name="deletedByUserEmail">The email of the user performing the deletion.</param>
    /// <returns>The result of the deletion operation.</returns>
    Task<DeleteVatRuleResult> DeleteRuleAsync(Guid id, string deletedByUserId, string? deletedByUserEmail = null);

    /// <summary>
    /// Gets the applicable tax rate for a specific country, optional category, and date.
    /// </summary>
    /// <param name="countryCode">The ISO 3166-1 alpha-2 country code.</param>
    /// <param name="categoryId">The optional category ID.</param>
    /// <param name="asOfDate">The date for which to find the applicable rate.</param>
    /// <returns>The result containing the applicable tax rate.</returns>
    Task<GetApplicableRateResult> GetApplicableRateAsync(string countryCode, Guid? categoryId, DateTimeOffset asOfDate);

    /// <summary>
    /// Gets the history of changes for a specific VAT rule.
    /// </summary>
    /// <param name="vatRuleId">The VAT rule ID.</param>
    /// <returns>The result containing the rule history.</returns>
    Task<GetVatRuleHistoryResult> GetRuleHistoryAsync(Guid vatRuleId);
}

/// <summary>
/// Result of getting all VAT rules.
/// </summary>
public class GetVatRulesResult
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
    /// Gets the VAT rules.
    /// </summary>
    public IReadOnlyList<VatRule> Rules { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with VAT rules.
    /// </summary>
    /// <param name="rules">The VAT rules.</param>
    /// <returns>A successful result.</returns>
    public static GetVatRulesResult Success(IReadOnlyList<VatRule> rules) => new()
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
    public static GetVatRulesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetVatRulesResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetVatRulesResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single VAT rule.
/// </summary>
public class GetVatRuleResult
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
    /// Gets the VAT rule.
    /// </summary>
    public VatRule? Rule { get; private init; }

    /// <summary>
    /// Creates a successful result with a VAT rule.
    /// </summary>
    /// <param name="rule">The VAT rule.</param>
    /// <returns>A successful result.</returns>
    public static GetVatRuleResult Success(VatRule rule) => new()
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
    public static GetVatRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetVatRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetVatRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to create a new VAT rule.
/// </summary>
public class CreateVatRuleCommand
{
    /// <summary>
    /// Gets or sets the descriptive name of the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax rate as a percentage.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Gets or sets the optional category ID.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the date when the rule becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the optional date when the rule expires.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// Gets or sets the priority of this rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID creating this rule.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user creating this rule.
    /// </summary>
    public string? CreatedByUserEmail { get; set; }
}

/// <summary>
/// Result of creating a VAT rule.
/// </summary>
public class CreateVatRuleResult
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
    /// Gets the created VAT rule.
    /// </summary>
    public VatRule? Rule { get; private init; }

    /// <summary>
    /// Creates a successful result with the created rule.
    /// </summary>
    /// <param name="rule">The created VAT rule.</param>
    /// <returns>A successful result.</returns>
    public static CreateVatRuleResult Success(VatRule rule) => new()
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
    public static CreateVatRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateVatRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateVatRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to update an existing VAT rule.
/// </summary>
public class UpdateVatRuleCommand
{
    /// <summary>
    /// Gets or sets the ID of the rule to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the descriptive name of the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax rate as a percentage.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Gets or sets the optional category ID.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the date when the rule becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the optional date when the rule expires.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// Gets or sets the priority of this rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the user ID updating this rule.
    /// </summary>
    public string UpdatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user updating this rule.
    /// </summary>
    public string? UpdatedByUserEmail { get; set; }
}

/// <summary>
/// Result of updating a VAT rule.
/// </summary>
public class UpdateVatRuleResult
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
    /// Gets the updated VAT rule.
    /// </summary>
    public VatRule? Rule { get; private init; }

    /// <summary>
    /// Creates a successful result with the updated rule.
    /// </summary>
    /// <param name="rule">The updated VAT rule.</param>
    /// <returns>A successful result.</returns>
    public static UpdateVatRuleResult Success(VatRule rule) => new()
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
    public static UpdateVatRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateVatRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateVatRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of deleting a VAT rule.
/// </summary>
public class DeleteVatRuleResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static DeleteVatRuleResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static DeleteVatRuleResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static DeleteVatRuleResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static DeleteVatRuleResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting the applicable tax rate.
/// </summary>
public class GetApplicableRateResult
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
    /// Gets the applicable tax rate as a percentage.
    /// </summary>
    public decimal? TaxRate { get; private init; }

    /// <summary>
    /// Gets the VAT rule that was applied.
    /// </summary>
    public VatRule? AppliedRule { get; private init; }

    /// <summary>
    /// Creates a successful result with the applicable rate.
    /// </summary>
    /// <param name="taxRate">The applicable tax rate.</param>
    /// <param name="appliedRule">The VAT rule that was applied.</param>
    /// <returns>A successful result.</returns>
    public static GetApplicableRateResult Success(decimal taxRate, VatRule appliedRule) => new()
    {
        Succeeded = true,
        Errors = [],
        TaxRate = taxRate,
        AppliedRule = appliedRule
    };

    /// <summary>
    /// Creates a successful result indicating no applicable rate was found.
    /// </summary>
    /// <returns>A successful result with no rate.</returns>
    public static GetApplicableRateResult NoRateFound() => new()
    {
        Succeeded = true,
        Errors = [],
        TaxRate = null,
        AppliedRule = null
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetApplicableRateResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetApplicableRateResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetApplicableRateResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting VAT rule history.
/// </summary>
public class GetVatRuleHistoryResult
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
    /// Gets the VAT rule history records.
    /// </summary>
    public IReadOnlyList<VatRuleHistory> History { get; private init; } = [];

    /// <summary>
    /// Gets the VAT rule for context.
    /// </summary>
    public VatRule? Rule { get; private init; }

    /// <summary>
    /// Creates a successful result with history records.
    /// </summary>
    /// <param name="history">The history records.</param>
    /// <param name="rule">The VAT rule.</param>
    /// <returns>A successful result.</returns>
    public static GetVatRuleHistoryResult Success(IReadOnlyList<VatRuleHistory> history, VatRule? rule) => new()
    {
        Succeeded = true,
        Errors = [],
        History = history,
        Rule = rule
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetVatRuleHistoryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetVatRuleHistoryResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetVatRuleHistoryResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
