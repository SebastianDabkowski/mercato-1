using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Application.Services;

/// <summary>
/// Service interface for settlement operations.
/// </summary>
public interface ISettlementService
{
    /// <summary>
    /// Generates a settlement for a seller and period.
    /// </summary>
    /// <param name="command">The generate settlement command.</param>
    /// <returns>The result of the generate settlement operation.</returns>
    Task<GenerateSettlementResult> GenerateSettlementAsync(GenerateSettlementCommand command);

    /// <summary>
    /// Regenerates an existing settlement with audit trail.
    /// </summary>
    /// <param name="command">The regenerate settlement command.</param>
    /// <returns>The result of the regenerate settlement operation.</returns>
    Task<RegenerateSettlementResult> RegenerateSettlementAsync(RegenerateSettlementCommand command);

    /// <summary>
    /// Finalizes a settlement, preventing further regeneration.
    /// </summary>
    /// <param name="settlementId">The settlement identifier.</param>
    /// <returns>The result of the finalize settlement operation.</returns>
    Task<FinalizeSettlementResult> FinalizeSettlementAsync(Guid settlementId);

    /// <summary>
    /// Exports a settlement and returns CSV data.
    /// </summary>
    /// <param name="settlementId">The settlement identifier.</param>
    /// <returns>The result of the export settlement operation.</returns>
    Task<ExportSettlementResult> ExportSettlementAsync(Guid settlementId);

    /// <summary>
    /// Gets a settlement by its identifier including line items.
    /// </summary>
    /// <param name="settlementId">The settlement identifier.</param>
    /// <returns>The result containing the settlement.</returns>
    Task<GetSettlementResult> GetSettlementAsync(Guid settlementId);

    /// <summary>
    /// Gets settlements with optional filters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>The result containing settlements.</returns>
    Task<GetSettlementsResult> GetSettlementsAsync(GetSettlementsQuery query);
}

/// <summary>
/// Command to generate a settlement for a seller and period.
/// </summary>
public class GenerateSettlementCommand
{
    /// <summary>
    /// Gets or sets the seller identifier.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the settlement year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the settlement month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets an optional audit note.
    /// </summary>
    public string? AuditNote { get; set; }
}

/// <summary>
/// Result of generating a settlement.
/// </summary>
public class GenerateSettlementResult
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
    /// Gets the generated settlement.
    /// </summary>
    public Settlement? Settlement { get; private init; }

    /// <summary>
    /// Creates a successful result with the generated settlement.
    /// </summary>
    /// <param name="settlement">The generated settlement.</param>
    /// <returns>A successful result.</returns>
    public static GenerateSettlementResult Success(Settlement settlement) => new()
    {
        Succeeded = true,
        Errors = [],
        Settlement = settlement
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GenerateSettlementResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GenerateSettlementResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GenerateSettlementResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Command to regenerate an existing settlement.
/// </summary>
public class RegenerateSettlementCommand
{
    /// <summary>
    /// Gets or sets the settlement identifier.
    /// </summary>
    public Guid SettlementId { get; set; }

    /// <summary>
    /// Gets or sets the reason for regeneration.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Result of regenerating a settlement.
/// </summary>
public class RegenerateSettlementResult
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
    /// Gets the regenerated settlement.
    /// </summary>
    public Settlement? Settlement { get; private init; }

    /// <summary>
    /// Gets the previous version number before regeneration.
    /// </summary>
    public int PreviousVersion { get; private init; }

    /// <summary>
    /// Creates a successful result with the regenerated settlement.
    /// </summary>
    /// <param name="settlement">The regenerated settlement.</param>
    /// <param name="previousVersion">The previous version number.</param>
    /// <returns>A successful result.</returns>
    public static RegenerateSettlementResult Success(Settlement settlement, int previousVersion) => new()
    {
        Succeeded = true,
        Errors = [],
        Settlement = settlement,
        PreviousVersion = previousVersion
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RegenerateSettlementResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RegenerateSettlementResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RegenerateSettlementResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of finalizing a settlement.
/// </summary>
public class FinalizeSettlementResult
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
    /// Gets the finalized settlement.
    /// </summary>
    public Settlement? Settlement { get; private init; }

    /// <summary>
    /// Creates a successful result with the finalized settlement.
    /// </summary>
    /// <param name="settlement">The finalized settlement.</param>
    /// <returns>A successful result.</returns>
    public static FinalizeSettlementResult Success(Settlement settlement) => new()
    {
        Succeeded = true,
        Errors = [],
        Settlement = settlement
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static FinalizeSettlementResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static FinalizeSettlementResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static FinalizeSettlementResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of exporting a settlement.
/// </summary>
public class ExportSettlementResult
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
    /// Gets the CSV data.
    /// </summary>
    public string? CsvData { get; private init; }

    /// <summary>
    /// Gets the suggested file name for the export.
    /// </summary>
    public string? FileName { get; private init; }

    /// <summary>
    /// Gets the exported settlement.
    /// </summary>
    public Settlement? Settlement { get; private init; }

    /// <summary>
    /// Creates a successful result with the CSV data.
    /// </summary>
    /// <param name="settlement">The exported settlement.</param>
    /// <param name="csvData">The CSV data.</param>
    /// <param name="fileName">The suggested file name.</param>
    /// <returns>A successful result.</returns>
    public static ExportSettlementResult Success(Settlement settlement, string csvData, string fileName) => new()
    {
        Succeeded = true,
        Errors = [],
        Settlement = settlement,
        CsvData = csvData,
        FileName = fileName
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ExportSettlementResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ExportSettlementResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ExportSettlementResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Result of getting a single settlement.
/// </summary>
public class GetSettlementResult
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
    /// Gets the settlement.
    /// </summary>
    public Settlement? Settlement { get; private init; }

    /// <summary>
    /// Creates a successful result with the settlement.
    /// </summary>
    /// <param name="settlement">The settlement.</param>
    /// <returns>A successful result.</returns>
    public static GetSettlementResult Success(Settlement settlement) => new()
    {
        Succeeded = true,
        Errors = [],
        Settlement = settlement
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSettlementResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSettlementResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetSettlementResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}

/// <summary>
/// Query parameters for filtering settlements.
/// </summary>
public class GetSettlementsQuery
{
    /// <summary>
    /// Gets or sets the optional seller identifier filter.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the optional year filter.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the optional month filter.
    /// </summary>
    public int? Month { get; set; }

    /// <summary>
    /// Gets or sets the optional status filter.
    /// </summary>
    public SettlementStatus? Status { get; set; }
}

/// <summary>
/// Result of getting multiple settlements.
/// </summary>
public class GetSettlementsResult
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
    /// Gets the settlements.
    /// </summary>
    public IReadOnlyList<Settlement> Settlements { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with settlements.
    /// </summary>
    /// <param name="settlements">The settlements.</param>
    /// <returns>A successful result.</returns>
    public static GetSettlementsResult Success(IReadOnlyList<Settlement> settlements) => new()
    {
        Succeeded = true,
        Errors = [],
        Settlements = settlements
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSettlementsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSettlementsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetSettlementsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized."]
    };
}
