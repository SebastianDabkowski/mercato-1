namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Result of merging a guest cart with a user's cart.
/// </summary>
public class MergeGuestCartResult
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
    /// Gets the number of items that were merged from the guest cart.
    /// </summary>
    public int ItemsMerged { get; private init; }

    /// <summary>
    /// Gets a value indicating whether a guest cart was found and merged.
    /// </summary>
    public bool GuestCartFound { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="itemsMerged">The number of items merged.</param>
    /// <param name="guestCartFound">Whether a guest cart was found.</param>
    /// <returns>A successful result.</returns>
    public static MergeGuestCartResult Success(int itemsMerged, bool guestCartFound = true) => new()
    {
        Succeeded = true,
        Errors = [],
        ItemsMerged = itemsMerged,
        GuestCartFound = guestCartFound
    };

    /// <summary>
    /// Creates a result indicating no guest cart was found.
    /// </summary>
    /// <returns>A successful result with no items merged.</returns>
    public static MergeGuestCartResult NoGuestCart() => new()
    {
        Succeeded = true,
        Errors = [],
        ItemsMerged = 0,
        GuestCartFound = false
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static MergeGuestCartResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static MergeGuestCartResult Failure(string error) => Failure([error]);
}
