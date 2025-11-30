using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for admin review moderation operations.
/// </summary>
public class ReviewModerationService : IReviewModerationService
{
    /// <summary>
    /// Minimum buyer ID length for partial display.
    /// </summary>
    private const int MinBuyerIdLengthForPartialDisplay = 8;

    /// <summary>
    /// Number of characters to show at start and end of buyer ID.
    /// </summary>
    private const int BuyerAliasDisplayChars = 4;

    /// <summary>
    /// Maximum length for review text preview.
    /// </summary>
    private const int ReviewTextPreviewMaxLength = 100;

    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ILogger<ReviewModerationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewModerationService"/> class.
    /// </summary>
    /// <param name="productReviewRepository">The product review repository.</param>
    /// <param name="adminAuditRepository">The admin audit repository.</param>
    /// <param name="logger">The logger.</param>
    public ReviewModerationService(
        IProductReviewRepository productReviewRepository,
        IAdminAuditRepository adminAuditRepository,
        ILogger<ReviewModerationService> logger)
    {
        _productReviewRepository = productReviewRepository ?? throw new ArgumentNullException(nameof(productReviewRepository));
        _adminAuditRepository = adminAuditRepository ?? throw new ArgumentNullException(nameof(adminAuditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GetAdminReviewsResult> GetReviewsAsync(AdminReviewFilterQuery query)
    {
        try
        {
            var (reviews, totalCount) = await _productReviewRepository.GetAllFilteredAsync(
                query.SearchTerm,
                query.Statuses.Count > 0 ? query.Statuses : null,
                query.FromDate,
                query.ToDate,
                query.Page,
                query.PageSize);

            var reviewSummaries = reviews.Select(r => new AdminReviewSummary
            {
                Id = r.Id,
                ProductId = r.ProductId,
                StoreId = r.StoreId,
                BuyerAlias = GenerateBuyerAlias(r.BuyerId),
                Rating = r.Rating,
                ReviewTextPreview = TruncateReviewText(r.ReviewText),
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                LastUpdatedAt = r.LastUpdatedAt
            }).ToList();

            return GetAdminReviewsResult.Success(reviewSummaries, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for moderation");
            return GetAdminReviewsResult.Failure("An error occurred while retrieving reviews.");
        }
    }

    /// <inheritdoc />
    public async Task<GetAdminReviewDetailsResult> GetReviewDetailsAsync(Guid reviewId)
    {
        try
        {
            var review = await _productReviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                return GetAdminReviewDetailsResult.Failure("Review not found.");
            }

            var reviewDetails = new AdminReviewDetails
            {
                Id = review.Id,
                OrderId = review.OrderId,
                SellerSubOrderId = review.SellerSubOrderId,
                SellerSubOrderItemId = review.SellerSubOrderItemId,
                ProductId = review.ProductId,
                StoreId = review.StoreId,
                BuyerAlias = GenerateBuyerAlias(review.BuyerId),
                Rating = review.Rating,
                ReviewText = review.ReviewText,
                Status = review.Status,
                CreatedAt = review.CreatedAt,
                LastUpdatedAt = review.LastUpdatedAt
            };

            return GetAdminReviewDetailsResult.Success(reviewDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review details for review {ReviewId}", reviewId);
            return GetAdminReviewDetailsResult.Failure("An error occurred while retrieving review details.");
        }
    }

    /// <inheritdoc />
    public async Task<ModerateReviewResult> ModerateReviewAsync(ModerateReviewCommand command)
    {
        try
        {
            // Validate command
            var validationErrors = ValidateModerateReviewCommand(command);
            if (validationErrors.Count > 0)
            {
                return ModerateReviewResult.Failure(validationErrors);
            }

            var review = await _productReviewRepository.GetByIdAsync(command.ReviewId);
            if (review == null)
            {
                return ModerateReviewResult.Failure("Review not found.");
            }

            var oldStatus = review.Status;

            // Update review status
            review.Status = command.NewStatus;
            review.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _productReviewRepository.UpdateAsync(review);

            // Build audit log details including removal reason if applicable
            var auditDetails = $"Changed review status from {oldStatus} to {command.NewStatus}. Reason: {command.ModerationReason}";
            if (command.NewStatus == ReviewStatus.Hidden && command.RemovalReason.HasValue)
            {
                auditDetails += $" Removal category: {command.RemovalReason.Value}";
            }

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "ModerateReview",
                EntityType = "ProductReview",
                EntityId = review.Id.ToString(),
                Details = auditDetails,
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Review {ReviewId} moderated by admin {AdminUserId}: {OldStatus} -> {NewStatus}",
                review.Id, command.AdminUserId, oldStatus, command.NewStatus);

            return ModerateReviewResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating review {ReviewId}", command.ReviewId);
            return ModerateReviewResult.Failure("An error occurred while moderating the review.");
        }
    }

    /// <inheritdoc />
    public async Task<FlagReviewResult> FlagReviewAsync(FlagReviewCommand command)
    {
        try
        {
            // Validate command
            var validationErrors = ValidateFlagReviewCommand(command);
            if (validationErrors.Count > 0)
            {
                return FlagReviewResult.Failure(validationErrors);
            }

            var review = await _productReviewRepository.GetByIdAsync(command.ReviewId);
            if (review == null)
            {
                return FlagReviewResult.Failure("Review not found.");
            }

            if (review.Status == ReviewStatus.Pending)
            {
                return FlagReviewResult.Failure("Review is already flagged for moderation.");
            }

            var oldStatus = review.Status;

            // Update review status to Pending for moderation
            review.Status = ReviewStatus.Pending;
            review.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _productReviewRepository.UpdateAsync(review);

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "FlagReview",
                EntityType = "ProductReview",
                EntityId = review.Id.ToString(),
                Details = $"Flagged review for moderation. Previous status: {oldStatus}. Reason: {command.FlagReason}",
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Review {ReviewId} flagged for moderation by admin {AdminUserId}",
                review.Id, command.AdminUserId);

            return FlagReviewResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging review {ReviewId}", command.ReviewId);
            return FlagReviewResult.Failure("An error occurred while flagging the review.");
        }
    }

    private static List<string> ValidateModerateReviewCommand(ModerateReviewCommand command)
    {
        var errors = new List<string>();

        if (command.ReviewId == Guid.Empty)
        {
            errors.Add("Review ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ModerationReason))
        {
            errors.Add("Moderation reason is required.");
        }

        // Require removal reason when hiding a review
        if (command.NewStatus == ReviewStatus.Hidden && 
            (!command.RemovalReason.HasValue || command.RemovalReason.Value == ReviewRemovalReason.None))
        {
            errors.Add("Removal reason category is required when hiding a review.");
        }

        return errors;
    }

    private static List<string> ValidateFlagReviewCommand(FlagReviewCommand command)
    {
        var errors = new List<string>();

        if (command.ReviewId == Guid.Empty)
        {
            errors.Add("Review ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FlagReason))
        {
            errors.Add("Flag reason is required.");
        }

        return errors;
    }

    private static string GenerateBuyerAlias(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return "Unknown";
        }

        // Create a privacy-preserving alias using partial display
        if (buyerId.Length <= MinBuyerIdLengthForPartialDisplay)
        {
            return $"Buyer-{buyerId[..Math.Min(BuyerAliasDisplayChars, buyerId.Length)]}***";
        }

        return $"Buyer-{buyerId[..BuyerAliasDisplayChars]}***{buyerId[^BuyerAliasDisplayChars..]}";
    }

    private static string TruncateReviewText(string reviewText)
    {
        if (string.IsNullOrEmpty(reviewText))
        {
            return string.Empty;
        }

        if (reviewText.Length <= ReviewTextPreviewMaxLength)
        {
            return reviewText;
        }

        return reviewText[..ReviewTextPreviewMaxLength] + "...";
    }
}
