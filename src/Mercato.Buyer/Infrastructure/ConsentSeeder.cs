using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Buyer.Infrastructure;

/// <summary>
/// Seeds the default consent types for the Mercato marketplace.
/// </summary>
public static class ConsentSeeder
{
    /// <summary>
    /// Consent type code for newsletter subscriptions.
    /// </summary>
    public const string NewsletterConsent = "NEWSLETTER";

    /// <summary>
    /// Consent type code for user profiling and personalization.
    /// </summary>
    public const string ProfilingConsent = "PROFILING";

    /// <summary>
    /// Consent type code for third-party data sharing.
    /// </summary>
    public const string ThirdPartySharingConsent = "THIRD_PARTY_SHARING";

    /// <summary>
    /// Consent type code for marketing communications.
    /// </summary>
    public const string MarketingConsent = "MARKETING";

    /// <summary>
    /// Seeds the default consent types if they do not exist.
    /// </summary>
    /// <param name="context">The buyer database context.</param>
    public static async Task SeedConsentTypesAsync(BuyerDbContext context)
    {
        var consentTypesToSeed = new List<(string Code, string Name, string Description, bool IsMandatory, int DisplayOrder, string ConsentText)>
        {
            (
                NewsletterConsent,
                "Newsletter",
                "Receive our newsletter with updates about new products, sellers, and promotions.",
                false,
                1,
                "I agree to receive the Mercato newsletter with updates about new products, featured sellers, and promotional offers. You can unsubscribe at any time through your account settings or by clicking the unsubscribe link in any email."
            ),
            (
                MarketingConsent,
                "Marketing Communications",
                "Receive personalized marketing emails and notifications about offers relevant to you.",
                false,
                2,
                "I agree to receive personalized marketing communications from Mercato, including email notifications about special offers, discounts, and product recommendations based on my browsing and purchase history. You can withdraw this consent at any time through your privacy settings."
            ),
            (
                ProfilingConsent,
                "Personalization",
                "Allow us to personalize your shopping experience based on your preferences and activity.",
                false,
                3,
                "I consent to Mercato creating a profile based on my browsing behavior, purchase history, and preferences to provide personalized product recommendations and a tailored shopping experience. This data is used only to improve your experience on our platform."
            ),
            (
                ThirdPartySharingConsent,
                "Third-Party Data Sharing",
                "Allow sharing of anonymized data with trusted partners for analytics purposes.",
                false,
                4,
                "I consent to Mercato sharing anonymized and aggregated data with trusted third-party partners for analytics and market research purposes. Your personal information is never sold, and any shared data cannot be used to identify you personally."
            )
        };

        foreach (var (code, name, description, isMandatory, displayOrder, consentText) in consentTypesToSeed)
        {
            var existingType = await context.ConsentTypes
                .FirstOrDefaultAsync(ct => ct.Code == code);

            if (existingType == null)
            {
                var consentType = new ConsentType
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Description = description,
                    IsActive = true,
                    IsMandatory = isMandatory,
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.ConsentTypes.Add(consentType);
                await context.SaveChangesAsync();

                var consentVersion = new ConsentVersion
                {
                    Id = Guid.NewGuid(),
                    ConsentTypeId = consentType.Id,
                    VersionNumber = 1,
                    ConsentText = consentText,
                    EffectiveFrom = DateTimeOffset.UtcNow,
                    EffectiveTo = null,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.ConsentVersions.Add(consentVersion);
                await context.SaveChangesAsync();
            }
        }
    }
}
