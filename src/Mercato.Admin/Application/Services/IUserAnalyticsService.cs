using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for user analytics operations.
/// Provides aggregated, anonymized metrics for user registration and activity.
/// </summary>
public interface IUserAnalyticsService
{
    /// <summary>
    /// Gets user analytics metrics for the specified period.
    /// All metrics are aggregated counts and do not expose individual user data.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analytics result with aggregated user metrics.</returns>
    Task<UserAnalyticsResult> GetAnalyticsAsync(
        UserAnalyticsQuery query,
        CancellationToken cancellationToken = default);
}
