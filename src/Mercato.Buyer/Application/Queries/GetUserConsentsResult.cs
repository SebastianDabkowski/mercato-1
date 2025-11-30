namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Result containing user consents.
/// </summary>
public class GetUserConsentsResult
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
    /// Gets the list of user consents.
    /// </summary>
    public IReadOnlyList<UserConsentDto> Consents { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with the provided consents.
    /// </summary>
    /// <param name="consents">The list of user consents.</param>
    /// <returns>A successful result.</returns>
    public static GetUserConsentsResult Success(IReadOnlyList<UserConsentDto> consents) => new()
    {
        Succeeded = true,
        Errors = [],
        Consents = consents
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetUserConsentsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetUserConsentsResult Failure(string error) => Failure([error]);
}

/// <summary>
/// DTO representing a user's consent with details.
/// </summary>
public class UserConsentDto
{
    /// <summary>
    /// Gets or sets the consent type code.
    /// </summary>
    public string ConsentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent type name.
    /// </summary>
    public string ConsentTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent type description.
    /// </summary>
    public string ConsentTypeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether consent is currently granted.
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the consent was given or withdrawn.
    /// </summary>
    public DateTimeOffset ConsentedAt { get; set; }

    /// <summary>
    /// Gets or sets the version number of the consent text when consent was given.
    /// </summary>
    public int ConsentVersionNumber { get; set; }

    /// <summary>
    /// Gets or sets the consent text that was agreed to.
    /// </summary>
    public string ConsentText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current version ID for updating consent.
    /// </summary>
    public Guid CurrentVersionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is a newer version available.
    /// </summary>
    public bool HasNewerVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this consent is mandatory.
    /// </summary>
    public bool IsMandatory { get; set; }
}
