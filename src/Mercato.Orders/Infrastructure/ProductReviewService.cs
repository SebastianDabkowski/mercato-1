using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Service implementation for product review management operations.
/// </summary>
public class ProductReviewService : IProductReviewService
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly ISellerSubOrderRepository _sellerSubOrderRepository;
    private readonly ILogger<ProductReviewService> _logger;

    /// <summary>
    /// The minimum time in seconds between review submissions for rate limiting.
    /// </summary>
    private const int RateLimitSeconds = 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductReviewService"/> class.
    /// </summary>
    /// <param name="productReviewRepository">The product review repository.</param>
    /// <param name="sellerSubOrderRepository">The seller sub-order repository.</param>
    /// <param name="logger">The logger.</param>
    public ProductReviewService(
        IProductReviewRepository productReviewRepository,
        ISellerSubOrderRepository sellerSubOrderRepository,
        ILogger<ProductReviewService> logger)
    {
        _productReviewRepository = productReviewRepository;
        _sellerSubOrderRepository = sellerSubOrderRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubmitProductReviewResult> SubmitReviewAsync(SubmitProductReviewCommand command)
    {
        var validationErrors = ValidateSubmitReviewCommand(command);
        if (validationErrors.Count > 0)
        {
            return SubmitProductReviewResult.Failure(validationErrors);
        }

        try
        {
            // Get the seller sub-order item
            var item = await GetSellerSubOrderItemAsync(command.SellerSubOrderItemId);
            if (item == null)
            {
                return SubmitProductReviewResult.Failure("Item not found.");
            }

            // Check authorization - buyer must own the parent order
            if (item.SellerSubOrder?.Order == null || item.SellerSubOrder.Order.BuyerId != command.BuyerId)
            {
                return SubmitProductReviewResult.NotAuthorized();
            }

            // Check if item is delivered
            if (item.Status != SellerSubOrderItemStatus.Delivered)
            {
                return SubmitProductReviewResult.Failure("Reviews can only be submitted for delivered items.");
            }

            // Check if review already exists for this item
            var existingReview = await _productReviewRepository.ExistsForItemAsync(command.SellerSubOrderItemId, command.BuyerId);
            if (existingReview)
            {
                return SubmitProductReviewResult.Failure("You have already submitted a review for this item.");
            }

            // Check rate limiting
            var lastReviewTime = await _productReviewRepository.GetBuyerLastReviewTimeAsync(command.BuyerId);
            if (lastReviewTime.HasValue)
            {
                var timeSinceLastReview = DateTimeOffset.UtcNow - lastReviewTime.Value;
                if (timeSinceLastReview.TotalSeconds < RateLimitSeconds)
                {
                    var remainingSeconds = Math.Max(1, (int)Math.Ceiling(RateLimitSeconds - timeSinceLastReview.TotalSeconds));
                    return SubmitProductReviewResult.Failure(
                        $"Please wait {remainingSeconds} seconds before submitting another review.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var reviewId = Guid.NewGuid();

            var review = new ProductReview
            {
                Id = reviewId,
                OrderId = item.SellerSubOrder.OrderId,
                SellerSubOrderId = item.SellerSubOrderId,
                SellerSubOrderItemId = command.SellerSubOrderItemId,
                ProductId = item.ProductId,
                StoreId = item.SellerSubOrder.StoreId,
                BuyerId = command.BuyerId,
                Rating = command.Rating,
                ReviewText = command.ReviewText,
                Status = ReviewStatus.Published,
                CreatedAt = now,
                LastUpdatedAt = now
            };

            await _productReviewRepository.AddAsync(review);

            _logger.LogInformation(
                "Created product review {ReviewId} for product {ProductId} by buyer {BuyerId} with rating {Rating}",
                reviewId, item.ProductId, command.BuyerId, command.Rating);

            return SubmitProductReviewResult.Success(reviewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review for item {ItemId}", command.SellerSubOrderItemId);
            return SubmitProductReviewResult.Failure("An error occurred while submitting the review.");
        }
    }

    /// <inheritdoc />
    public async Task<GetProductReviewsResult> GetReviewsByProductIdAsync(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            return GetProductReviewsResult.Failure("Product ID is required.");
        }

        try
        {
            var reviews = await _productReviewRepository.GetByProductIdAsync(productId);
            return GetProductReviewsResult.Success(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for product {ProductId}", productId);
            return GetProductReviewsResult.Failure("An error occurred while getting the reviews.");
        }
    }

    /// <inheritdoc />
    public async Task<GetProductReviewsPagedResult> GetReviewsByProductIdPagedAsync(GetProductReviewsQuery query)
    {
        var validationErrors = ValidateGetProductReviewsQuery(query);
        if (validationErrors.Count > 0)
        {
            return GetProductReviewsPagedResult.Failure(validationErrors);
        }

        try
        {
            var (reviews, totalCount, averageRating) = await _productReviewRepository.GetPagedByProductIdAsync(
                query.ProductId,
                query.Page,
                query.PageSize,
                query.SortBy);

            return GetProductReviewsPagedResult.Success(reviews, totalCount, averageRating, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged reviews for product {ProductId}", query.ProductId);
            return GetProductReviewsPagedResult.Failure("An error occurred while getting the reviews.");
        }
    }

    /// <inheritdoc />
    public async Task<GetProductReviewsResult> GetReviewsByBuyerIdAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetProductReviewsResult.Failure("Buyer ID is required.");
        }

        try
        {
            var reviews = await _productReviewRepository.GetByBuyerIdAsync(buyerId);
            return GetProductReviewsResult.Success(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for buyer {BuyerId}", buyerId);
            return GetProductReviewsResult.Failure("An error occurred while getting the reviews.");
        }
    }

    /// <inheritdoc />
    public async Task<CanSubmitReviewResult> CanSubmitReviewAsync(Guid sellerSubOrderItemId, string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return CanSubmitReviewResult.Failure("Buyer ID is required.");
        }

        if (sellerSubOrderItemId == Guid.Empty)
        {
            return CanSubmitReviewResult.Failure("Item ID is required.");
        }

        try
        {
            // Get the seller sub-order item
            var item = await GetSellerSubOrderItemAsync(sellerSubOrderItemId);
            if (item == null)
            {
                return CanSubmitReviewResult.Failure("Item not found.");
            }

            // Check authorization - buyer must own the parent order
            if (item.SellerSubOrder?.Order == null || item.SellerSubOrder.Order.BuyerId != buyerId)
            {
                return CanSubmitReviewResult.NotAuthorized();
            }

            // Check if item is delivered
            if (item.Status != SellerSubOrderItemStatus.Delivered)
            {
                return CanSubmitReviewResult.No("Reviews can only be submitted for delivered items.");
            }

            // Check if review already exists for this item
            var existingReview = await _productReviewRepository.ExistsForItemAsync(sellerSubOrderItemId, buyerId);
            if (existingReview)
            {
                return CanSubmitReviewResult.No("You have already submitted a review for this item.");
            }

            return CanSubmitReviewResult.Yes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking review eligibility for item {ItemId}", sellerSubOrderItemId);
            return CanSubmitReviewResult.Failure("An error occurred while checking review eligibility.");
        }
    }

    /// <summary>
    /// Gets a seller sub-order item with its related sub-order and order.
    /// </summary>
    private async Task<SellerSubOrderItem?> GetSellerSubOrderItemAsync(Guid itemId)
    {
        return await _sellerSubOrderRepository.GetItemByIdAsync(itemId);
    }

    /// <summary>
    /// Validates the submit review command.
    /// </summary>
    private static List<string> ValidateSubmitReviewCommand(SubmitProductReviewCommand command)
    {
        var errors = new List<string>();

        if (command.SellerSubOrderItemId == Guid.Empty)
        {
            errors.Add("Item ID is required.");
        }

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.Rating < 1 || command.Rating > 5)
        {
            errors.Add("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(command.ReviewText))
        {
            errors.Add("Review text is required.");
        }
        else if (command.ReviewText.Length > 2000)
        {
            errors.Add("Review text must not exceed 2000 characters.");
        }

        return errors;
    }

    /// <summary>
    /// Validates the get product reviews query.
    /// </summary>
    private static List<string> ValidateGetProductReviewsQuery(GetProductReviewsQuery query)
    {
        var errors = new List<string>();

        if (query.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (query.Page < 1)
        {
            errors.Add("Page must be at least 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100.");
        }

        return errors;
    }
}
