namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Result containing consent types.
/// </summary>
public class GetConsentTypesResult
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
    /// Gets the list of consent types.
    /// </summary>
    public IReadOnlyList<ConsentTypeDto> ConsentTypes { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with the provided consent types.
    /// </summary>
    /// <param name="consentTypes">The list of consent types.</param>
    /// <returns>A successful result.</returns>
    public static GetConsentTypesResult Success(IReadOnlyList<ConsentTypeDto> consentTypes) => new()
    {
        Succeeded = true,
        Errors = [],
        ConsentTypes = consentTypes
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetConsentTypesResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetConsentTypesResult Failure(string error) => Failure([error]);
}

/// <summary>
/// DTO representing a consent type with its current version.
/// </summary>
public class ConsentTypeDto
{
    /// <summary>
    /// Gets or sets the consent type ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the consent type code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent type description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this consent is mandatory.
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the current consent version ID.
    /// </summary>
    public Guid CurrentVersionId { get; set; }

    /// <summary>
    /// Gets or sets the current consent text.
    /// </summary>
    public string CurrentConsentText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current version number.
    /// </summary>
    public int CurrentVersionNumber { get; set; }
}
