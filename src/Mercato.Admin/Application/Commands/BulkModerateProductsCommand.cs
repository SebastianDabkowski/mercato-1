namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command for bulk approving or rejecting products in the moderation queue.
/// </summary>
public class BulkModerateProductsCommand
{
    /// <summary>
    /// Gets or sets the product IDs to moderate.
    /// </summary>
    public IReadOnlyList<Guid> ProductIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the admin user ID performing the bulk moderation.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to approve (true) or reject (false).
    /// </summary>
    public bool Approve { get; set; }

    /// <summary>
    /// Gets or sets the reason for the moderation decision.
    /// Required for rejections, optional for approvals.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the admin performing the action.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Result of a bulk product moderation operation.
/// </summary>
public class BulkModerateProductsResult
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
    /// Gets the count of products successfully moderated.
    /// </summary>
    public int SuccessCount { get; private init; }

    /// <summary>
    /// Gets the count of products that failed to moderate.
    /// </summary>
    public int FailureCount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="successCount">The number of products successfully moderated.</param>
    /// <returns>A successful result.</returns>
    public static BulkModerateProductsResult Success(int successCount) => new()
    {
        Succeeded = true,
        Errors = [],
        SuccessCount = successCount
    };

    /// <summary>
    /// Creates a partial success result.
    /// </summary>
    /// <param name="successCount">The number of products successfully moderated.</param>
    /// <param name="failureCount">The number of products that failed to moderate.</param>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A partial success result.</returns>
    public static BulkModerateProductsResult PartialSuccess(int successCount, int failureCount, IReadOnlyList<string> errors) => new()
    {
        Succeeded = successCount > 0,
        SuccessCount = successCount,
        FailureCount = failureCount,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static BulkModerateProductsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static BulkModerateProductsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static BulkModerateProductsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to moderate products."]
    };
}
