namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command for removing a photo in the moderation queue.
/// </summary>
public class RemovePhotoCommand
{
    /// <summary>
    /// Gets or sets the image ID to remove.
    /// </summary>
    public Guid ImageId { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID removing the photo.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the removal.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the admin performing the action.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Result of a photo removal operation.
/// </summary>
public class RemovePhotoResult
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
    /// Gets the seller ID to notify.
    /// </summary>
    public string? SellerIdToNotify { get; private init; }

    /// <summary>
    /// Gets the product title for the notification.
    /// </summary>
    public string? ProductTitle { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="sellerIdToNotify">The seller ID to notify.</param>
    /// <param name="productTitle">The product title.</param>
    /// <returns>A successful result.</returns>
    public static RemovePhotoResult Success(string sellerIdToNotify, string productTitle) => new()
    {
        Succeeded = true,
        Errors = [],
        SellerIdToNotify = sellerIdToNotify,
        ProductTitle = productTitle
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RemovePhotoResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RemovePhotoResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static RemovePhotoResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to remove photos."]
    };
}
