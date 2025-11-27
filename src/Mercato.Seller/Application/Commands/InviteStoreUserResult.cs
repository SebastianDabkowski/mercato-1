namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of inviting a store user.
/// </summary>
public class InviteStoreUserResult
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
    /// Gets the ID of the created store user invitation.
    /// </summary>
    public Guid? StoreUserId { get; private init; }

    /// <summary>
    /// Gets the invitation token for the invited user.
    /// </summary>
    public string? InvitationToken { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="storeUserId">The ID of the created store user.</param>
    /// <param name="invitationToken">The invitation token.</param>
    /// <returns>A successful result.</returns>
    public static InviteStoreUserResult Success(Guid storeUserId, string invitationToken) => new()
    {
        Succeeded = true,
        Errors = [],
        StoreUserId = storeUserId,
        InvitationToken = invitationToken
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static InviteStoreUserResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static InviteStoreUserResult Failure(string error) => Failure([error]);
}
