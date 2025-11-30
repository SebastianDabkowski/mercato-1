namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a user data export operation.
/// </summary>
public class UserDataExportResult
{
    /// <summary>
    /// Gets a value indicating whether the export was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user was not found.
    /// </summary>
    public bool IsUserNotFound { get; init; }

    /// <summary>
    /// Gets the exported data in JSON format.
    /// </summary>
    public string? ExportData { get; init; }

    /// <summary>
    /// Gets the timestamp when the export was generated.
    /// </summary>
    public DateTimeOffset? ExportedAt { get; init; }

    /// <summary>
    /// Gets the error messages if the export failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result with the exported data.
    /// </summary>
    /// <param name="exportData">The exported data in JSON format.</param>
    /// <param name="exportedAt">The timestamp when the export was generated.</param>
    public static UserDataExportResult Success(string exportData, DateTimeOffset exportedAt)
    {
        return new UserDataExportResult
        {
            Succeeded = true,
            ExportData = exportData,
            ExportedAt = exportedAt,
            Errors = []
        };
    }

    /// <summary>
    /// Creates a result indicating the user was not found.
    /// </summary>
    public static UserDataExportResult UserNotFound()
    {
        return new UserDataExportResult
        {
            Succeeded = false,
            IsUserNotFound = true,
            Errors = ["User not found."]
        };
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static UserDataExportResult Failure(string error)
    {
        return new UserDataExportResult
        {
            Succeeded = false,
            Errors = [error]
        };
    }
}
