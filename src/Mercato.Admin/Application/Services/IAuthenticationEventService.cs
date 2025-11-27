using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing and querying authentication events.
/// </summary>
public interface IAuthenticationEventService
{
    /// <summary>
    /// Logs an authentication event.
    /// </summary>
    /// <param name="eventType">The type of authentication event.</param>
    /// <param name="email">The email address used in the attempt.</param>
    /// <param name="isSuccessful">Whether the authentication was successful.</param>
    /// <param name="userId">The user ID (if known).</param>
    /// <param name="userRole">The user's role (if known).</param>
    /// <param name="ipAddress">The IP address (will be hashed for privacy).</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="failureReason">The reason for failure (if applicable).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogEventAsync(
        AuthenticationEventType eventType,
        string email,
        bool isSuccessful,
        string? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? failureReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authentication statistics for a specified time period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication statistics for the period.</returns>
    Task<AuthenticationStatistics> GetStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects and returns suspicious authentication patterns.
    /// </summary>
    /// <param name="startDate">The start date of the period to analyze.</param>
    /// <param name="endDate">The end date of the period to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of detected suspicious activities.</returns>
    Task<IReadOnlyList<SuspiciousActivityInfo>> GetSuspiciousActivityAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authentication events with optional filtering.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="userRole">Optional user role filter.</param>
    /// <param name="isSuccessful">Optional success status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A filtered list of authentication events.</returns>
    Task<IReadOnlyList<AuthenticationEvent>> GetEventsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        AuthenticationEventType? eventType = null,
        string? userRole = null,
        bool? isSuccessful = null,
        CancellationToken cancellationToken = default);
}
