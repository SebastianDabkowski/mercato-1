using Mercato.Analytics.Domain.Entities;

namespace Mercato.Analytics.Application.Services;

/// <summary>
/// Service interface for recording analytics events.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Records a search event.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userId">The user ID (null for guests).</param>
    /// <param name="searchQuery">The search query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordSearchAsync(string sessionId, string? userId, string searchQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a product view event.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userId">The user ID (null for guests).</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="sellerId">The seller ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordProductViewAsync(string sessionId, string? userId, int productId, int? sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an add to cart event.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userId">The user ID (null for guests).</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="sellerId">The seller ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordAddToCartAsync(string sessionId, string? userId, int productId, int? sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a checkout start event.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordCheckoutStartAsync(string sessionId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an order completion event.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="orderId">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordOrderCompletionAsync(string sessionId, string userId, int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics events within a time range.
    /// </summary>
    /// <param name="fromDate">The start date of the range.</param>
    /// <param name="toDate">The end date of the range.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of analytics events.</returns>
    Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        AnalyticsEventType? eventType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of events by type within a time range.
    /// </summary>
    /// <param name="fromDate">The start date of the range.</param>
    /// <param name="toDate">The end date of the range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary of event types and their counts.</returns>
    Task<IDictionary<AnalyticsEventType, int>> GetEventCountsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether analytics tracking is enabled.
    /// </summary>
    bool IsEnabled { get; }
}
