using Mercato.Analytics.Domain.Entities;

namespace Mercato.Analytics.Domain.Interfaces;

/// <summary>
/// Repository interface for analytics event data access operations.
/// </summary>
public interface IAnalyticsEventRepository
{
    /// <summary>
    /// Adds a new analytics event.
    /// </summary>
    /// <param name="analyticsEvent">The analytics event to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added analytics event.</returns>
    Task<AnalyticsEvent> AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics events within a time range.
    /// </summary>
    /// <param name="fromDate">The start date of the range.</param>
    /// <param name="toDate">The end date of the range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of analytics events within the specified range.</returns>
    Task<IReadOnlyList<AnalyticsEvent>> GetByTimeRangeAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics events by event type within a time range.
    /// </summary>
    /// <param name="eventType">The type of event to filter by.</param>
    /// <param name="fromDate">The start date of the range.</param>
    /// <param name="toDate">The end date of the range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of analytics events matching the criteria.</returns>
    Task<IReadOnlyList<AnalyticsEvent>> GetByEventTypeAsync(
        AnalyticsEventType eventType,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of events by type within a time range.
    /// </summary>
    /// <param name="fromDate">The start date of the range.</param>
    /// <param name="toDate">The end date of the range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary of event types and their counts.</returns>
    Task<IDictionary<AnalyticsEventType, int>> GetEventCountsByTypeAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);
}
