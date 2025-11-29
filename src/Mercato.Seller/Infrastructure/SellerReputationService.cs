using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Implementation of seller reputation service for calculating and managing seller reputation scores.
/// </summary>
public class SellerReputationService : ISellerReputationService
{
    /// <summary>
    /// Weight for average rating in reputation calculation (40%).
    /// </summary>
    private const decimal AverageRatingWeight = 0.40m;

    /// <summary>
    /// Weight for on-time shipping rate in reputation calculation (25%).
    /// </summary>
    private const decimal OnTimeShippingWeight = 0.25m;

    /// <summary>
    /// Weight for dispute rate in reputation calculation (20%).
    /// </summary>
    private const decimal DisputeRateWeight = 0.20m;

    /// <summary>
    /// Weight for cancellation rate in reputation calculation (15%).
    /// </summary>
    private const decimal CancellationRateWeight = 0.15m;

    /// <summary>
    /// Minimum number of orders required to calculate a full reputation level.
    /// </summary>
    private const int MinOrdersForFullReputation = 10;

    private readonly ISellerReputationRepository _repository;
    private readonly ILogger<SellerReputationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerReputationService"/> class.
    /// </summary>
    /// <param name="repository">The seller reputation repository.</param>
    /// <param name="logger">The logger.</param>
    public SellerReputationService(
        ISellerReputationRepository repository,
        ILogger<SellerReputationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SellerReputation?> GetReputationByStoreIdAsync(Guid storeId)
    {
        return await _repository.GetByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<CalculateReputationResult> CalculateAndUpdateReputationAsync(
        Guid storeId,
        CalculateReputationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CalculateReputationResult.Failure(validationErrors);
        }

        // Calculate metrics
        var onTimeShippingRate = CalculateOnTimeShippingRate(command);
        var cancellationRate = CalculateCancellationRate(command);
        var disputeRate = CalculateDisputeRate(command);

        // Calculate overall reputation score
        var reputationScore = CalculateReputationScore(
            command.AverageRating,
            onTimeShippingRate,
            cancellationRate,
            disputeRate);

        // Determine reputation level
        var reputationLevel = DetermineReputationLevel(reputationScore, command.TotalOrdersCount);

        // Get or create reputation record
        var existingReputation = await _repository.GetByStoreIdAsync(storeId);
        var now = DateTimeOffset.UtcNow;

        if (existingReputation != null)
        {
            UpdateReputationRecord(existingReputation, command, onTimeShippingRate, cancellationRate, disputeRate, reputationScore, reputationLevel, now);
            await _repository.UpdateAsync(existingReputation);
            _logger.LogInformation("Updated reputation for store {StoreId}. Score: {Score}, Level: {Level}",
                storeId, reputationScore, reputationLevel);
        }
        else
        {
            var newReputation = CreateReputationRecord(storeId, command, onTimeShippingRate, cancellationRate, disputeRate, reputationScore, reputationLevel, now);
            await _repository.CreateAsync(newReputation);
            _logger.LogInformation("Created reputation for store {StoreId}. Score: {Score}, Level: {Level}",
                storeId, reputationScore, reputationLevel);
        }

        return CalculateReputationResult.Success(reputationScore, reputationLevel);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerReputation>> GetReputationsByStoreIdsAsync(IEnumerable<Guid> storeIds)
    {
        ArgumentNullException.ThrowIfNull(storeIds);
        return await _repository.GetByStoreIdsAsync(storeIds);
    }

    /// <summary>
    /// Validates the calculate reputation command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateCommand(CalculateReputationCommand command)
    {
        var errors = new List<string>();

        if (command.AverageRating.HasValue)
        {
            if (command.AverageRating < 1 || command.AverageRating > 5)
            {
                errors.Add("Average rating must be between 1 and 5.");
            }
        }

        if (command.TotalRatingsCount < 0)
        {
            errors.Add("Total ratings count cannot be negative.");
        }

        if (command.TotalOrdersCount < 0)
        {
            errors.Add("Total orders count cannot be negative.");
        }

        if (command.DeliveredOrdersCount < 0)
        {
            errors.Add("Delivered orders count cannot be negative.");
        }

        if (command.OnTimeDeliveriesCount < 0)
        {
            errors.Add("On-time deliveries count cannot be negative.");
        }

        if (command.CancelledOrdersCount < 0)
        {
            errors.Add("Cancelled orders count cannot be negative.");
        }

        if (command.DisputedOrdersCount < 0)
        {
            errors.Add("Disputed orders count cannot be negative.");
        }

        if (command.DeliveredOrdersCount > command.TotalOrdersCount)
        {
            errors.Add("Delivered orders count cannot exceed total orders count.");
        }

        if (command.OnTimeDeliveriesCount > command.DeliveredOrdersCount)
        {
            errors.Add("On-time deliveries count cannot exceed delivered orders count.");
        }

        if (command.CancelledOrdersCount > command.TotalOrdersCount)
        {
            errors.Add("Cancelled orders count cannot exceed total orders count.");
        }

        if (command.DisputedOrdersCount > command.TotalOrdersCount)
        {
            errors.Add("Disputed orders count cannot exceed total orders count.");
        }

        return errors;
    }

    /// <summary>
    /// Calculates the on-time shipping rate as a percentage.
    /// </summary>
    /// <param name="command">The command with delivery metrics.</param>
    /// <returns>The on-time shipping rate (0-100), or null if no deliveries.</returns>
    private static decimal? CalculateOnTimeShippingRate(CalculateReputationCommand command)
    {
        if (command.DeliveredOrdersCount == 0)
        {
            return null;
        }

        return (decimal)command.OnTimeDeliveriesCount / command.DeliveredOrdersCount * 100;
    }

    /// <summary>
    /// Calculates the cancellation rate as a percentage.
    /// </summary>
    /// <param name="command">The command with order metrics.</param>
    /// <returns>The cancellation rate (0-100), or null if no orders.</returns>
    private static decimal? CalculateCancellationRate(CalculateReputationCommand command)
    {
        if (command.TotalOrdersCount == 0)
        {
            return null;
        }

        return (decimal)command.CancelledOrdersCount / command.TotalOrdersCount * 100;
    }

    /// <summary>
    /// Calculates the dispute rate as a percentage.
    /// </summary>
    /// <param name="command">The command with dispute metrics.</param>
    /// <returns>The dispute rate (0-100), or null if no orders.</returns>
    private static decimal? CalculateDisputeRate(CalculateReputationCommand command)
    {
        if (command.TotalOrdersCount == 0)
        {
            return null;
        }

        return (decimal)command.DisputedOrdersCount / command.TotalOrdersCount * 100;
    }

    /// <summary>
    /// Calculates the overall reputation score using the weighted formula.
    /// </summary>
    /// <param name="averageRating">The average rating (1-5).</param>
    /// <param name="onTimeShippingRate">The on-time shipping rate (0-100).</param>
    /// <param name="cancellationRate">The cancellation rate (0-100).</param>
    /// <param name="disputeRate">The dispute rate (0-100).</param>
    /// <returns>The calculated reputation score (0-100), or null if insufficient data.</returns>
    private static decimal? CalculateReputationScore(
        decimal? averageRating,
        decimal? onTimeShippingRate,
        decimal? cancellationRate,
        decimal? disputeRate)
    {
        decimal totalWeight = 0;
        decimal weightedSum = 0;

        // Average Rating Weight: 40% (normalize rating 1-5 to 0-100)
        if (averageRating.HasValue)
        {
            var normalizedRating = (averageRating.Value - 1) / 4 * 100;
            weightedSum += normalizedRating * AverageRatingWeight;
            totalWeight += AverageRatingWeight;
        }

        // On-Time Shipping Rate Weight: 25%
        if (onTimeShippingRate.HasValue)
        {
            weightedSum += onTimeShippingRate.Value * OnTimeShippingWeight;
            totalWeight += OnTimeShippingWeight;
        }

        // Dispute Rate Weight: 20% (inverse: 100 - disputeRate)
        if (disputeRate.HasValue)
        {
            weightedSum += (100 - disputeRate.Value) * DisputeRateWeight;
            totalWeight += DisputeRateWeight;
        }

        // Cancellation Rate Weight: 15% (inverse: 100 - cancellationRate)
        if (cancellationRate.HasValue)
        {
            weightedSum += (100 - cancellationRate.Value) * CancellationRateWeight;
            totalWeight += CancellationRateWeight;
        }

        // If no metrics available, return null
        if (totalWeight == 0)
        {
            return null;
        }

        // Normalize the score by available metrics. This allows meaningful score calculation
        // when only partial metrics are present, rather than penalizing sellers for missing data.
        return Math.Round(weightedSum / totalWeight, 2);
    }

    /// <summary>
    /// Determines the reputation level based on score and order count.
    /// </summary>
    /// <param name="reputationScore">The calculated reputation score.</param>
    /// <param name="totalOrdersCount">The total number of orders.</param>
    /// <returns>The determined reputation level.</returns>
    private static ReputationLevel DetermineReputationLevel(decimal? reputationScore, int totalOrdersCount)
    {
        // No score available
        if (!reputationScore.HasValue)
        {
            return ReputationLevel.Unrated;
        }

        // New sellers with fewer than 10 orders are labeled as "New" regardless of their score.
        // This business rule ensures buyers understand the seller has limited history.
        // The actual score is still calculated and stored for reference.
        if (totalOrdersCount < MinOrdersForFullReputation)
        {
            return ReputationLevel.New;
        }

        // Determine level based on score
        return reputationScore.Value switch
        {
            >= 90 => ReputationLevel.Platinum,
            >= 75 => ReputationLevel.Gold,
            >= 60 => ReputationLevel.Silver,
            _ => ReputationLevel.Bronze
        };
    }

    /// <summary>
    /// Updates an existing reputation record with new values.
    /// </summary>
    private static void UpdateReputationRecord(
        SellerReputation reputation,
        CalculateReputationCommand command,
        decimal? onTimeShippingRate,
        decimal? cancellationRate,
        decimal? disputeRate,
        decimal? reputationScore,
        ReputationLevel reputationLevel,
        DateTimeOffset now)
    {
        reputation.AverageRating = command.AverageRating;
        reputation.TotalRatingsCount = command.TotalRatingsCount;
        reputation.DisputeRate = disputeRate;
        reputation.OnTimeShippingRate = onTimeShippingRate;
        reputation.CancellationRate = cancellationRate;
        reputation.TotalOrdersCount = command.TotalOrdersCount;
        reputation.ReputationScore = reputationScore;
        reputation.ReputationLevel = reputationLevel;
        reputation.LastCalculatedAt = now;
        reputation.LastUpdatedAt = now;
    }

    /// <summary>
    /// Creates a new reputation record with calculated values.
    /// </summary>
    private static SellerReputation CreateReputationRecord(
        Guid storeId,
        CalculateReputationCommand command,
        decimal? onTimeShippingRate,
        decimal? cancellationRate,
        decimal? disputeRate,
        decimal? reputationScore,
        ReputationLevel reputationLevel,
        DateTimeOffset now)
    {
        return new SellerReputation
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            AverageRating = command.AverageRating,
            TotalRatingsCount = command.TotalRatingsCount,
            DisputeRate = disputeRate,
            OnTimeShippingRate = onTimeShippingRate,
            CancellationRate = cancellationRate,
            TotalOrdersCount = command.TotalOrdersCount,
            ReputationScore = reputationScore,
            ReputationLevel = reputationLevel,
            LastCalculatedAt = now,
            CreatedAt = now,
            LastUpdatedAt = now
        };
    }
}
