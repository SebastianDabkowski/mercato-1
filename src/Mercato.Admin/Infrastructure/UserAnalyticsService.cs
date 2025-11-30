using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for user analytics operations.
/// Provides aggregated, anonymized metrics for user registration and activity.
/// </summary>
public class UserAnalyticsService : IUserAnalyticsService
{
    private readonly IUserAnalyticsRepository _repository;
    private readonly ILogger<UserAnalyticsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAnalyticsService"/> class.
    /// </summary>
    /// <param name="repository">The user analytics repository.</param>
    /// <param name="logger">The logger.</param>
    public UserAnalyticsService(
        IUserAnalyticsRepository repository,
        ILogger<UserAnalyticsService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<UserAnalyticsResult> GetAnalyticsAsync(
        UserAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _logger.LogInformation(
            "Retrieving user analytics for period {StartDate} to {EndDate}",
            query.StartDate,
            query.EndDate);

        var result = new UserAnalyticsResult
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            RetrievedAt = DateTimeOffset.UtcNow
        };

        // Fetch all metrics concurrently for efficiency
        var newBuyersTask = SafeGetMetricAsync(
            () => _repository.GetNewBuyerRegistrationsCountAsync(query.StartDate, query.EndDate, cancellationToken),
            "new buyer registrations",
            cancellationToken);

        var newSellersTask = SafeGetMetricAsync(
            () => _repository.GetNewSellerRegistrationsCountAsync(query.StartDate, query.EndDate, cancellationToken),
            "new seller registrations",
            cancellationToken);

        var usersLoggedInTask = SafeGetMetricAsync(
            () => _repository.GetUsersLoggedInCountAsync(query.StartDate, query.EndDate, cancellationToken),
            "users logged in",
            cancellationToken);

        var usersLoggedInByRoleTask = SafeGetDictionaryMetricAsync(
            () => _repository.GetUsersLoggedInByRoleAsync(query.StartDate, query.EndDate, cancellationToken),
            "users logged in by role",
            cancellationToken);

        var usersWithOrdersTask = SafeGetMetricAsync(
            () => _repository.GetUsersWhoPlacedOrdersCountAsync(query.StartDate, query.EndDate, cancellationToken),
            "users who placed orders",
            cancellationToken);

        await Task.WhenAll(newBuyersTask, newSellersTask, usersLoggedInTask, usersLoggedInByRoleTask, usersWithOrdersTask);

        var (newBuyers, hasBuyerData) = await newBuyersTask;
        var (newSellers, hasSellerData) = await newSellersTask;
        var (usersLoggedIn, hasLoginData) = await usersLoggedInTask;
        var (usersLoggedInByRole, _) = await usersLoggedInByRoleTask;
        var (usersWithOrders, hasOrderData) = await usersWithOrdersTask;

        result.NewBuyerAccounts = newBuyers;
        result.NewSellerAccounts = newSellers;
        result.UsersWhoLoggedIn = usersLoggedIn;
        result.ActiveUsersByRole = usersLoggedInByRole;
        result.UsersWhoPlacedOrders = usersWithOrders;
        
        // Total active users is the count of unique users who logged in
        result.TotalActiveUsers = usersLoggedIn;

        // Set data availability flags
        result.HasBuyerRegistrationData = hasBuyerData;
        result.HasSellerRegistrationData = hasSellerData;
        result.HasLoginActivityData = hasLoginData;
        result.HasOrderActivityData = hasOrderData;

        // Check for insufficient data scenario
        result.HasInsufficientData = !hasBuyerData && !hasSellerData && !hasLoginData && !hasOrderData;
        if (result.HasInsufficientData)
        {
            result.InsufficientDataMessage = "No analytics data is available for the selected period. " +
                "Data will appear once user activity is recorded.";
        }
        else if (!hasLoginData && !hasBuyerData)
        {
            // Partial data scenario - login/registration data missing
            result.InsufficientDataMessage = "Registration and login metrics may be unavailable. " +
                "This could indicate that authentication event logging is not yet enabled.";
        }

        _logger.LogInformation(
            "User analytics retrieved: {NewBuyers} new buyers, {NewSellers} new sellers, " +
            "{ActiveUsers} active users, {OrderUsers} users placed orders",
            result.NewBuyerAccounts,
            result.NewSellerAccounts,
            result.TotalActiveUsers,
            result.UsersWhoPlacedOrders);

        return result;
    }

    private async Task<(int Value, bool HasData)> SafeGetMetricAsync(
        Func<Task<int>> operation,
        string metricName,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = await operation();
            return (value, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to retrieve {MetricName} metric", metricName);
            return (0, false);
        }
    }

    private async Task<(Dictionary<string, int> Value, bool HasData)> SafeGetDictionaryMetricAsync(
        Func<Task<Dictionary<string, int>>> operation,
        string metricName,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = await operation();
            return (value, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to retrieve {MetricName} metric", metricName);
            return (new Dictionary<string, int>(), false);
        }
    }
}
