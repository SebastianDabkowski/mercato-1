using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller
{
    [Authorize(Roles = "Buyer,Seller")]
    public class IndexModel : PageModel
    {
        private readonly IKycService _kycService;
        private readonly ISellerOnboardingService _onboardingService;
        private readonly IPayoutSettingsService _payoutSettingsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IKycService kycService,
            ISellerOnboardingService onboardingService,
            IPayoutSettingsService payoutSettingsService,
            ILogger<IndexModel> logger)
        {
            _kycService = kycService;
            _onboardingService = onboardingService;
            _payoutSettingsService = payoutSettingsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the overall KYC verification status for the seller.
        /// </summary>
        public KycVerificationStatus KycStatus { get; private set; } = KycVerificationStatus.NotSubmitted;

        /// <summary>
        /// Gets the most recent KYC submission, if any.
        /// </summary>
        public KycSubmission? LatestSubmission { get; private set; }

        /// <summary>
        /// Gets whether there was an error loading KYC status.
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// Gets the error message if an error occurred.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the seller onboarding record.
        /// </summary>
        public SellerOnboarding? Onboarding { get; private set; }

        /// <summary>
        /// Gets whether the seller needs to complete onboarding.
        /// </summary>
        public bool NeedsOnboarding { get; private set; }

        /// <summary>
        /// Gets whether the seller has complete payout settings.
        /// </summary>
        public bool HasCompletePayoutSettings { get; private set; }

        public async Task OnGetAsync()
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
            {
                _logger.LogWarning("User ID not found in claims");
                HasError = true;
                ErrorMessage = "Unable to identify the current user.";
                return;
            }

            try
            {
                // Check onboarding status
                Onboarding = await _onboardingService.GetOnboardingAsync(sellerId);
                NeedsOnboarding = Onboarding == null ||
                    (Onboarding.Status != OnboardingStatus.PendingVerification &&
                     Onboarding.Status != OnboardingStatus.Verified);

                // Check payout settings status (only for verified sellers)
                if (!NeedsOnboarding)
                {
                    HasCompletePayoutSettings = await _payoutSettingsService.HasCompletePayoutSettingsAsync(sellerId);
                }

                var submissions = await _kycService.GetSubmissionsBySellerAsync(sellerId);
                
                if (submissions.Count == 0)
                {
                    KycStatus = KycVerificationStatus.NotSubmitted;
                    return;
                }

                LatestSubmission = submissions.OrderByDescending(s => s.SubmittedAt).FirstOrDefault();
                
                // Determine overall KYC status based on submissions
                // Priority order: Approved > UnderReview > Pending > Rejected
                // Once verified, a seller remains verified even if new submissions are rejected
                // Only show Rejected if all submissions have been rejected
                if (submissions.Any(s => s.Status == Mercato.Seller.Domain.Entities.KycStatus.Approved))
                {
                    KycStatus = KycVerificationStatus.Verified;
                }
                else if (submissions.Any(s => s.Status == Mercato.Seller.Domain.Entities.KycStatus.UnderReview))
                {
                    KycStatus = KycVerificationStatus.UnderReview;
                }
                else if (submissions.Any(s => s.Status == Mercato.Seller.Domain.Entities.KycStatus.Pending))
                {
                    KycStatus = KycVerificationStatus.Pending;
                }
                else if (submissions.All(s => s.Status == Mercato.Seller.Domain.Entities.KycStatus.Rejected))
                {
                    KycStatus = KycVerificationStatus.Rejected;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading KYC status for seller {SellerId}", sellerId);
                HasError = true;
                ErrorMessage = "An error occurred while loading your KYC verification status. Please try again later.";
            }
        }
    }

    /// <summary>
    /// Represents the overall KYC verification status for display purposes.
    /// </summary>
    public enum KycVerificationStatus
    {
        /// <summary>
        /// No KYC documents have been submitted.
        /// </summary>
        NotSubmitted,

        /// <summary>
        /// KYC documents submitted and awaiting review.
        /// </summary>
        Pending,

        /// <summary>
        /// KYC documents are currently under review.
        /// </summary>
        UnderReview,

        /// <summary>
        /// KYC verification has been approved.
        /// </summary>
        Verified,

        /// <summary>
        /// KYC submission was rejected. Action required.
        /// </summary>
        Rejected
    }
}
