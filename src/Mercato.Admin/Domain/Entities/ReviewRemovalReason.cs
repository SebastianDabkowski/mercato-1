namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Defines the possible reasons for removing or hiding a review.
/// </summary>
public enum ReviewRemovalReason
{
    /// <summary>
    /// Review approved - no removal reason.
    /// </summary>
    None = 0,

    /// <summary>
    /// Review contains hate speech or discriminatory content.
    /// </summary>
    HateSpeech = 1,

    /// <summary>
    /// Review is spam or promotional content.
    /// </summary>
    Spam = 2,

    /// <summary>
    /// Review is off-topic or not relevant to the product/seller.
    /// </summary>
    OffTopic = 3,

    /// <summary>
    /// Review contains personal data or private information.
    /// </summary>
    PersonalData = 4,

    /// <summary>
    /// Review contains false or misleading information.
    /// </summary>
    FalseInformation = 5,

    /// <summary>
    /// Review contains profanity or inappropriate language.
    /// </summary>
    Profanity = 6,

    /// <summary>
    /// Review violates platform terms of service.
    /// </summary>
    TermsViolation = 7,

    /// <summary>
    /// Review removed for other reasons not listed above.
    /// </summary>
    Other = 8
}
