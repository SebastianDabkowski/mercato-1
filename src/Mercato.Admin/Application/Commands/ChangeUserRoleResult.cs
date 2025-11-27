namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Represents the result of a user role change attempt.
/// </summary>
public class ChangeUserRoleResult
{
    /// <summary>
    /// Gets a value indicating whether the role change was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the collection of error messages if the role change failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful role change result.
    /// </summary>
    public static ChangeUserRoleResult Success()
    {
        return new ChangeUserRoleResult { Succeeded = true };
    }

    /// <summary>
    /// Creates a failed role change result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    public static ChangeUserRoleResult Failure(IEnumerable<string> errors)
    {
        return new ChangeUserRoleResult
        {
            Succeeded = false,
            Errors = errors.ToList().AsReadOnly()
        };
    }

    /// <summary>
    /// Creates a failed role change result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static ChangeUserRoleResult Failure(string error)
    {
        return new ChangeUserRoleResult
        {
            Succeeded = false,
            Errors = new List<string> { error }.AsReadOnly()
        };
    }
}
