using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for managing authentication events.
/// </summary>
public interface IAuthenticationEventRepository
{
    /// <summary>
    /// Adds a new authentication event.
    /// </summary>
    /// <param name="authEvent">The authentication event to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(AuthenticationEvent authEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authentication events within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of authentication events in the specified range.</returns>
    Task<IReadOnlyList<AuthenticationEvent>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authentication events filtered by various criteria.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="userRole">Optional user role filter.</param>
    /// <param name="ipAddressHash">Optional IP address hash filter.</param>
    /// <param name="isSuccessful">Optional success status filter.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A filtered list of authentication events.</returns>
    Task<IReadOnlyList<AuthenticationEvent>> GetFilteredAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        AuthenticationEventType? eventType = null,
        string? userRole = null,
        string? ipAddressHash = null,
        bool? isSuccessful = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of events grouped by event type within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary with event type as key and count as value.</returns>
    Task<Dictionary<AuthenticationEventType, int>> GetEventCountsByTypeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of failed login attempts grouped by IP address hash within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="minimumAttempts">Minimum number of failed attempts to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary with IP address hash as key and failed attempt count as value.</returns>
    Task<Dictionary<string, int>> GetFailedAttemptsByIpAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int minimumAttempts = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with rapid login attempts (potential credential stuffing).
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="minimumAttempts">Minimum number of attempts to flag as suspicious.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary with email as key and attempt count as value.</returns>
    Task<Dictionary<string, int>> GetRapidLoginAttemptsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int minimumAttempts = 10,
        CancellationToken cancellationToken = default);
}
