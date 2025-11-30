namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command for approving a product in the moderation queue.
/// </summary>
public class ApproveProductCommand
{
    /// <summary>
    /// Gets or sets the product ID to approve.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID approving the product.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional reason for the approval.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the admin performing the action.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Result of a product approval operation.
/// </summary>
public class ApproveProductResult
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
    public static ApproveProductResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ApproveProductResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ApproveProductResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ApproveProductResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to approve products."]
    };
}
