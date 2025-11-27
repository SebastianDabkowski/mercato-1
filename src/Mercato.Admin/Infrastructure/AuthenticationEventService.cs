using System.Security.Cryptography;
using System.Text;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing and querying authentication events.
/// </summary>
public class AuthenticationEventService : IAuthenticationEventService
{
    private readonly IAuthenticationEventRepository _repository;
    private readonly ILogger<AuthenticationEventService> _logger;

    // Thresholds for detecting suspicious activity
    private const int BruteForceThreshold = 5;
    private const int CredentialStuffingThreshold = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationEventService"/> class.
    /// </summary>
    /// <param name="repository">The authentication event repository.</param>
    /// <param name="logger">The logger.</param>
    public AuthenticationEventService(
        IAuthenticationEventRepository repository,
        ILogger<AuthenticationEventService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task LogEventAsync(
        AuthenticationEventType eventType,
        string email,
        bool isSuccessful,
        string? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        var authEvent = new AuthenticationEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Email = email ?? string.Empty,
            IsSuccessful = isSuccessful,
            UserId = userId,
            UserRole = userRole,
            IpAddressHash = HashIpAddress(ipAddress),
            UserAgent = TruncateUserAgent(userAgent),
            FailureReason = failureReason,
            OccurredAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _repository.AddAsync(authEvent, cancellationToken);
            _logger.LogInformation(
                "Logged authentication event: {EventType} for {Email}, Success: {IsSuccessful}",
                eventType,
                email,
                isSuccessful);
        }
        catch (Exception ex)
        {
            // Don't fail the authentication flow if logging fails
            _logger.LogError(ex, "Failed to log authentication event for {Email}", email);
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationStatistics> GetStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var eventCounts = await _repository.GetEventCountsByTypeAsync(startDate, endDate, cancellationToken);
        var events = await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken);

        var statistics = new AuthenticationStatistics
        {
            StartDate = startDate,
            EndDate = endDate,
            EventsByType = eventCounts,
            TotalSuccessfulLogins = events.Count(e => e.EventType == AuthenticationEventType.Login && e.IsSuccessful),
            TotalFailedLogins = events.Count(e => e.EventType == AuthenticationEventType.Login && !e.IsSuccessful),
            TotalLockouts = eventCounts.GetValueOrDefault(AuthenticationEventType.Lockout, 0),
            TotalPasswordResets = eventCounts.GetValueOrDefault(AuthenticationEventType.PasswordReset, 0)
        };

        _logger.LogInformation(
            "Generated authentication statistics from {StartDate} to {EndDate}: {SuccessfulLogins} successful logins, {FailedLogins} failed logins",
            startDate,
            endDate,
            statistics.TotalSuccessfulLogins,
            statistics.TotalFailedLogins);

        return statistics;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SuspiciousActivityInfo>> GetSuspiciousActivityAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var suspiciousActivities = new List<SuspiciousActivityInfo>();
        var now = DateTimeOffset.UtcNow;

        // Detect brute force attempts (multiple failed logins from same IP)
        var failedByIp = await _repository.GetFailedAttemptsByIpAsync(
            startDate,
            endDate,
            BruteForceThreshold,
            cancellationToken);

        foreach (var (ipHash, count) in failedByIp)
        {
            var severity = count switch
            {
                >= 50 => AlertSeverity.Critical,
                >= 20 => AlertSeverity.High,
                >= 10 => AlertSeverity.Medium,
                _ => AlertSeverity.Low
            };

            suspiciousActivities.Add(new SuspiciousActivityInfo
            {
                ActivityType = SuspiciousActivityType.BruteForce,
                Description = $"Multiple failed login attempts ({count}) from the same IP address",
                Severity = severity,
                Count = count,
                Identifier = ipHash,
                DetectedAt = now
            });
        }

        // Detect credential stuffing (rapid attempts across accounts)
        var rapidAttempts = await _repository.GetRapidLoginAttemptsAsync(
            startDate,
            endDate,
            CredentialStuffingThreshold,
            cancellationToken);

        foreach (var (email, count) in rapidAttempts)
        {
            var severity = count switch
            {
                >= 100 => AlertSeverity.Critical,
                >= 50 => AlertSeverity.High,
                >= 25 => AlertSeverity.Medium,
                _ => AlertSeverity.Low
            };

            suspiciousActivities.Add(new SuspiciousActivityInfo
            {
                ActivityType = SuspiciousActivityType.RapidAttempts,
                Description = $"Unusually high number of login attempts ({count}) for account",
                Severity = severity,
                Count = count,
                Identifier = MaskEmail(email),
                DetectedAt = now
            });
        }

        _logger.LogInformation(
            "Detected {Count} suspicious activities between {StartDate} and {EndDate}",
            suspiciousActivities.Count,
            startDate,
            endDate);

        return suspiciousActivities
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.Count)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuthenticationEvent>> GetEventsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        AuthenticationEventType? eventType = null,
        string? userRole = null,
        bool? isSuccessful = null,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetFilteredAsync(
            startDate,
            endDate,
            eventType,
            userRole,
            ipAddressHash: null,
            isSuccessful,
            cancellationToken);
    }

    /// <summary>
    /// Hashes an IP address for privacy protection.
    /// </summary>
    private static string? HashIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToBase64String(bytes)[..16]; // Use first 16 chars for brevity
    }

    /// <summary>
    /// Truncates the user agent string to a reasonable length.
    /// </summary>
    private static string? TruncateUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return null;
        }

        return userAgent.Length > 500 ? userAgent[..500] : userAgent;
    }

    /// <summary>
    /// Masks an email address for privacy in logs.
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***@" + (atIndex >= 0 ? email[(atIndex + 1)..] : "***");
        }

        return email[0] + "***" + email[(atIndex - 1)..];
    }
}
