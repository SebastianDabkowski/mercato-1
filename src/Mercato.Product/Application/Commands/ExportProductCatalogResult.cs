namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of exporting a product catalog.
/// </summary>
public class ExportProductCatalogResult
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
    /// Gets a value indicating whether the request was not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the exported file content as a byte array.
    /// </summary>
    public byte[]? FileContent { get; private init; }

    /// <summary>
    /// Gets the file name for the export.
    /// </summary>
    public string? FileName { get; private init; }

    /// <summary>
    /// Gets the content type for the export file.
    /// </summary>
    public string? ContentType { get; private init; }

    /// <summary>
    /// Gets the total number of products exported.
    /// </summary>
    public int ExportedCount { get; private init; }

    /// <summary>
    /// Creates a successful result with export data.
    /// </summary>
    /// <param name="fileContent">The exported file content.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="exportedCount">The number of products exported.</param>
    /// <returns>A successful result.</returns>
    public static ExportProductCatalogResult Success(
        byte[] fileContent,
        string fileName,
        string contentType,
        int exportedCount) => new()
    {
        Succeeded = true,
        Errors = [],
        FileContent = fileContent,
        FileName = fileName,
        ContentType = contentType,
        ExportedCount = exportedCount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed result.</returns>
    public static ExportProductCatalogResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ExportProductCatalogResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <param name="message">The authorization failure message.</param>
    /// <returns>A not authorized result.</returns>
    public static ExportProductCatalogResult NotAuthorized(string message) => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = [message]
    };
}
