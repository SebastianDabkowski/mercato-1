using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Service implementation for seller rating management operations.
/// </summary>
public class SellerRatingService : ISellerRatingService
{
    private readonly ISellerRatingRepository _sellerRatingRepository;
    private readonly ISellerSubOrderRepository _sellerSubOrderRepository;
    private readonly ILogger<SellerRatingService> _logger;

    /// <summary>
    /// The minimum time in seconds between rating submissions for rate limiting.
    /// </summary>
    private const int RateLimitSeconds = 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerRatingService"/> class.
    /// </summary>
    /// <param name="sellerRatingRepository">The seller rating repository.</param>
    /// <param name="sellerSubOrderRepository">The seller sub-order repository.</param>
    /// <param name="logger">The logger.</param>
    public SellerRatingService(
        ISellerRatingRepository sellerRatingRepository,
        ISellerSubOrderRepository sellerSubOrderRepository,
        ILogger<SellerRatingService> logger)
    {
        _sellerRatingRepository = sellerRatingRepository;
        _sellerSubOrderRepository = sellerSubOrderRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubmitSellerRatingResult> SubmitRatingAsync(SubmitSellerRatingCommand command)
    {
        var validationErrors = ValidateSubmitRatingCommand(command);
        if (validationErrors.Count > 0)
        {
            return SubmitSellerRatingResult.Failure(validationErrors);
        }

        try
        {
            // Get the seller sub-order
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(command.SellerSubOrderId);
            if (subOrder == null)
            {
                return SubmitSellerRatingResult.Failure("Sub-order not found.");
            }

            // Check authorization - buyer must own the parent order
            if (subOrder.Order == null || subOrder.Order.BuyerId != command.BuyerId)
            {
                return SubmitSellerRatingResult.NotAuthorized();
            }

            // Check if sub-order is delivered
            if (subOrder.Status != SellerSubOrderStatus.Delivered)
            {
                return SubmitSellerRatingResult.Failure("Ratings can only be submitted for delivered sub-orders.");
            }

            // Check if rating already exists for this sub-order
            var existingRating = await _sellerRatingRepository.ExistsForSubOrderAsync(command.SellerSubOrderId, command.BuyerId);
            if (existingRating)
            {
                return SubmitSellerRatingResult.Failure("You have already submitted a rating for this seller on this order.");
            }

            // Check rate limiting
            var lastRatingTime = await _sellerRatingRepository.GetBuyerLastRatingTimeAsync(command.BuyerId);
            if (lastRatingTime.HasValue)
            {
                var timeSinceLastRating = DateTimeOffset.UtcNow - lastRatingTime.Value;
                if (timeSinceLastRating.TotalSeconds < RateLimitSeconds)
                {
                    var remainingSeconds = Math.Max(1, (int)Math.Ceiling(RateLimitSeconds - timeSinceLastRating.TotalSeconds));
                    return SubmitSellerRatingResult.Failure(
                        $"Please wait {remainingSeconds} seconds before submitting another rating.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var ratingId = Guid.NewGuid();

            var rating = new SellerRating
            {
                Id = ratingId,
                OrderId = subOrder.OrderId,
                SellerSubOrderId = command.SellerSubOrderId,
                StoreId = subOrder.StoreId,
                BuyerId = command.BuyerId,
                Rating = command.Rating,
                CreatedAt = now,
                LastUpdatedAt = now
            };

            await _sellerRatingRepository.AddAsync(rating);

            _logger.LogInformation(
                "Created seller rating {RatingId} for store {StoreId} by buyer {BuyerId} with rating {Rating}",
                ratingId, subOrder.StoreId, command.BuyerId, command.Rating);

            return SubmitSellerRatingResult.Success(ratingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting rating for sub-order {SubOrderId}", command.SellerSubOrderId);
            return SubmitSellerRatingResult.Failure("An error occurred while submitting the rating.");
        }
    }

    /// <inheritdoc />
    public async Task<GetSellerRatingsResult> GetRatingsByStoreIdAsync(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return GetSellerRatingsResult.Failure("Store ID is required.");
        }

        try
        {
            var ratings = await _sellerRatingRepository.GetByStoreIdAsync(storeId);
            return GetSellerRatingsResult.Success(ratings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for store {StoreId}", storeId);
            return GetSellerRatingsResult.Failure("An error occurred while getting the ratings.");
        }
    }

    /// <inheritdoc />
    public async Task<GetSellerRatingsResult> GetRatingsByBuyerIdAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetSellerRatingsResult.Failure("Buyer ID is required.");
        }

        try
        {
            var ratings = await _sellerRatingRepository.GetByBuyerIdAsync(buyerId);
            return GetSellerRatingsResult.Success(ratings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for buyer {BuyerId}", buyerId);
            return GetSellerRatingsResult.Failure("An error occurred while getting the ratings.");
        }
    }

    /// <inheritdoc />
    public async Task<CanSubmitSellerRatingResult> CanSubmitRatingAsync(Guid sellerSubOrderId, string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return CanSubmitSellerRatingResult.Failure("Buyer ID is required.");
        }

        if (sellerSubOrderId == Guid.Empty)
        {
            return CanSubmitSellerRatingResult.Failure("Sub-order ID is required.");
        }

        try
        {
            // Get the seller sub-order
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(sellerSubOrderId);
            if (subOrder == null)
            {
                return CanSubmitSellerRatingResult.Failure("Sub-order not found.");
            }

            // Check authorization - buyer must own the parent order
            if (subOrder.Order == null || subOrder.Order.BuyerId != buyerId)
            {
                return CanSubmitSellerRatingResult.NotAuthorized();
            }

            // Check if sub-order is delivered
            if (subOrder.Status != SellerSubOrderStatus.Delivered)
            {
                return CanSubmitSellerRatingResult.No("Ratings can only be submitted for delivered sub-orders.");
            }

            // Check if rating already exists for this sub-order
            var existingRating = await _sellerRatingRepository.ExistsForSubOrderAsync(sellerSubOrderId, buyerId);
            if (existingRating)
            {
                return CanSubmitSellerRatingResult.No("You have already submitted a rating for this seller on this order.");
            }

            return CanSubmitSellerRatingResult.Yes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rating eligibility for sub-order {SubOrderId}", sellerSubOrderId);
            return CanSubmitSellerRatingResult.Failure("An error occurred while checking rating eligibility.");
        }
    }

    /// <inheritdoc />
    public async Task<GetAverageRatingResult> GetAverageRatingForStoreAsync(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return GetAverageRatingResult.Failure("Store ID is required.");
        }

        try
        {
            var averageRating = await _sellerRatingRepository.GetAverageRatingForStoreAsync(storeId);
            return GetAverageRatingResult.Success(averageRating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting average rating for store {StoreId}", storeId);
            return GetAverageRatingResult.Failure("An error occurred while getting the average rating.");
        }
    }

    /// <summary>
    /// Validates the submit rating command.
    /// </summary>
    private static List<string> ValidateSubmitRatingCommand(SubmitSellerRatingCommand command)
    {
        var errors = new List<string>();

        if (command.SellerSubOrderId == Guid.Empty)
        {
            errors.Add("Sub-order ID is required.");
        }

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.Rating < 1 || command.Rating > 5)
        {
            errors.Add("Rating must be between 1 and 5.");
        }

        return errors;
    }
}
