using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for SLA tracking operations.
/// </summary>
public interface ISlaTrackingService
{
    /// <summary>
    /// Creates an SLA tracking record for a new case.
    /// Calculates deadlines based on applicable SLA configuration.
    /// </summary>
    /// <param name="caseId">The case ID (ReturnRequest ID).</param>
    /// <param name="caseNumber">The human-readable case number.</param>
    /// <param name="caseType">The case type (e.g., "Return", "Complaint").</param>
    /// <param name="storeId">The store/seller ID.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="caseCreatedAt">The date and time when the case was created.</param>
    /// <param name="category">Optional category for SLA configuration lookup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created SLA tracking record.</returns>
    Task<SlaTrackingRecord> CreateTrackingRecordAsync(
        Guid caseId,
        string caseNumber,
        string caseType,
        Guid storeId,
        string storeName,
        DateTimeOffset caseCreatedAt,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the seller's first response to a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="respondedAt">The date and time of the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordFirstResponseAsync(
        Guid caseId,
        DateTimeOffset respondedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the resolution of a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="resolvedAt">The date and time of resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordResolutionAsync(
        Guid caseId,
        DateTimeOffset resolvedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks and updates SLA breach status for all pending cases.
    /// This should be called periodically to flag breached cases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of cases flagged as breached.</returns>
    Task<int> CheckAndUpdateBreachesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SLA statistics for the admin dashboard.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate SLA statistics.</returns>
    Task<SlaDashboardStatistics> GetDashboardStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SLA statistics per seller within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of SLA statistics per seller.</returns>
    Task<IReadOnlyList<SlaStoreStatistics>> GetSellerStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cases with SLA breaches that need admin attention.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of breached SLA tracking records.</returns>
    Task<IReadOnlyList<SlaTrackingRecord>> GetBreachedCasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active SLA configurations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active SLA configurations.</returns>
    Task<IReadOnlyList<SlaConfiguration>> GetSlaConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an SLA configuration.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="adminUserId">The admin user ID making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved SLA configuration.</returns>
    Task<SlaConfiguration> SaveSlaConfigurationAsync(
        SlaConfiguration configuration,
        string adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an SLA configuration.
    /// </summary>
    /// <param name="configurationId">The configuration ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSlaConfigurationAsync(Guid configurationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents aggregate SLA statistics for the admin dashboard.
/// </summary>
public class SlaDashboardStatistics
{
    /// <summary>
    /// Gets or sets the start date of the statistics period.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the statistics period.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Gets or sets the total number of cases in the period.
    /// </summary>
    public int TotalCases { get; set; }

    /// <summary>
    /// Gets or sets the number of open/pending cases.
    /// </summary>
    public int OpenCases { get; set; }

    /// <summary>
    /// Gets or sets the number of cases resolved within SLA.
    /// </summary>
    public int CasesResolvedWithinSla { get; set; }

    /// <summary>
    /// Gets or sets the number of cases with first response within SLA.
    /// </summary>
    public int CasesRespondedWithinSla { get; set; }

    /// <summary>
    /// Gets or sets the number of cases currently breaching SLA.
    /// </summary>
    public int CurrentlyBreachedCases { get; set; }

    /// <summary>
    /// Gets or sets the number of first response SLA breaches.
    /// </summary>
    public int TotalFirstResponseBreaches { get; set; }

    /// <summary>
    /// Gets or sets the number of resolution SLA breaches.
    /// </summary>
    public int TotalResolutionBreaches { get; set; }

    /// <summary>
    /// Gets or sets the average response time in hours.
    /// </summary>
    public double AverageResponseTimeHours { get; set; }

    /// <summary>
    /// Gets or sets the average resolution time in hours.
    /// </summary>
    public double AverageResolutionTimeHours { get; set; }

    /// <summary>
    /// Gets the overall SLA compliance percentage.
    /// </summary>
    public double SlaCompliancePercentage => TotalCases > 0
        ? Math.Round((double)CasesResolvedWithinSla / TotalCases * 100, 2)
        : 0;

    /// <summary>
    /// Gets the first response SLA compliance percentage.
    /// </summary>
    public double FirstResponseCompliancePercentage => TotalCases > 0
        ? Math.Round((double)CasesRespondedWithinSla / TotalCases * 100, 2)
        : 0;
}
