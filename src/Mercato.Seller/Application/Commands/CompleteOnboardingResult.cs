namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of completing the seller onboarding wizard.
/// </summary>
public class CompleteOnboardingResult
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
    /// Gets the onboarding ID if successful.
    /// </summary>
    public Guid? OnboardingId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="onboardingId">The ID of the completed onboarding.</param>
    /// <returns>A successful result.</returns>
    public static CompleteOnboardingResult Success(Guid onboardingId) => new()
    {
        Succeeded = true,
        OnboardingId = onboardingId,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CompleteOnboardingResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors,
        OnboardingId = null
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CompleteOnboardingResult Failure(string error) => Failure([error]);
}
