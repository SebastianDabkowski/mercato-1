namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of accepting a store user invitation.
/// </summary>
public class AcceptStoreUserInvitationResult
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
    /// Gets the store ID the user now has access to.
    /// </summary>
    public Guid? StoreId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="storeId">The store ID the user now has access to.</param>
    /// <returns>A successful result.</returns>
    public static AcceptStoreUserInvitationResult Success(Guid storeId) => new()
    {
        Succeeded = true,
        Errors = [],
        StoreId = storeId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static AcceptStoreUserInvitationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static AcceptStoreUserInvitationResult Failure(string error) => Failure([error]);
}
