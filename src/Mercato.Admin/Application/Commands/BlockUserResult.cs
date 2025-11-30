namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Result of a block user operation.
/// </summary>
public class BlockUserResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful block user result.</returns>
    public static BlockUserResult Success() => new() { Succeeded = true, Errors = [] };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed block user result.</returns>
    public static BlockUserResult Failure(string error) => new() { Succeeded = false, Errors = [error] };

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed block user result.</returns>
    public static BlockUserResult Failure(IReadOnlyList<string> errors) => new() { Succeeded = false, Errors = errors };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized block user result.</returns>
    public static BlockUserResult NotAuthorized() => new() { Succeeded = false, IsNotAuthorized = true, Errors = [] };
}
