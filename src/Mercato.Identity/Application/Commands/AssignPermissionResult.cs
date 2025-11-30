namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Result of a permission assignment operation.
/// </summary>
public class AssignPermissionResult
{
    /// <summary>
    /// Gets or sets whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the error messages if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static AssignPermissionResult Success() => new() { Succeeded = true };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static AssignPermissionResult Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}
