using Mercato.Analytics.Domain.Entities;
using Mercato.Analytics.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Analytics.Application.Services;

/// <summary>
/// Service implementation for recording analytics events.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsEventRepository _repository;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly AnalyticsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsService"/> class.
    /// </summary>
    /// <param name="repository">The analytics event repository.</param>
    /// <param name="options">The analytics options.</param>
    /// <param name="logger">The logger.</param>
    public AnalyticsService(
        IAnalyticsEventRepository repository,
        IOptions<AnalyticsOptions> options,
        ILogger<AnalyticsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled;

    /// <inheritdoc />
    public async Task RecordSearchAsync(string sessionId, string? userId, string searchQuery, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping search event");
            return;
        }

        var analyticsEvent = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId,
            UserId = userId,
            EventType = AnalyticsEventType.Search,
            SearchQuery = searchQuery
        };

        await RecordEventAsync(analyticsEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordProductViewAsync(string sessionId, string? userId, int productId, int? sellerId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping product view event");
            return;
        }

        var analyticsEvent = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId,
            UserId = userId,
            EventType = AnalyticsEventType.ProductView,
            ProductId = productId,
            SellerId = sellerId
        };

        await RecordEventAsync(analyticsEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordAddToCartAsync(string sessionId, string? userId, int productId, int? sellerId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping add to cart event");
            return;
        }

        var analyticsEvent = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId,
            UserId = userId,
            EventType = AnalyticsEventType.AddToCart,
            ProductId = productId,
            SellerId = sellerId
        };

        await RecordEventAsync(analyticsEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordCheckoutStartAsync(string sessionId, string userId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping checkout start event");
            return;
        }

        var analyticsEvent = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId,
            UserId = userId,
            EventType = AnalyticsEventType.CheckoutStart
        };

        await RecordEventAsync(analyticsEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordOrderCompletionAsync(string sessionId, string userId, int orderId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping order completion event");
            return;
        }

        var analyticsEvent = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId,
            UserId = userId,
            EventType = AnalyticsEventType.OrderCompletion,
            OrderId = orderId
        };

        await RecordEventAsync(analyticsEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        AnalyticsEventType? eventType = null,
        CancellationToken cancellationToken = default)
    {
        if (eventType.HasValue)
        {
            return await _repository.GetByEventTypeAsync(eventType.Value, fromDate, toDate, cancellationToken);
        }

        return await _repository.GetByTimeRangeAsync(fromDate, toDate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IDictionary<AnalyticsEventType, int>> GetEventCountsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetEventCountsByTypeAsync(fromDate, toDate, cancellationToken);
    }

    private async Task RecordEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.AddAsync(analyticsEvent, cancellationToken);
            _logger.LogDebug("Recorded analytics event: {EventType} for session {SessionId}", analyticsEvent.EventType, analyticsEvent.SessionId);
        }
        catch (Exception ex)
        {
            // Log but don't throw - analytics should not impact core flows
            _logger.LogWarning(ex, "Failed to record analytics event: {EventType} for session {SessionId}", analyticsEvent.EventType, analyticsEvent.SessionId);
        }
    }
}
