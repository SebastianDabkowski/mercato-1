using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for SLA tracking record management.
/// </summary>
public interface ISlaTrackingRepository
{
    /// <summary>
    /// Gets an SLA tracking record by its unique identifier.
    /// </summary>
    /// <param name="id">The tracking record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SLA tracking record if found; otherwise, null.</returns>
    Task<SlaTrackingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an SLA tracking record by case ID.
    /// </summary>
    /// <param name="caseId">The case ID (ReturnRequest ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SLA tracking record if found; otherwise, null.</returns>
    Task<SlaTrackingRecord?> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SLA tracking records for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of SLA tracking records for the store.</returns>
    Task<IReadOnlyList<SlaTrackingRecord>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SLA tracking records that are breached and pending action.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of breached SLA tracking records.</returns>
    Task<IReadOnlyList<SlaTrackingRecord>> GetBreachedCasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SLA tracking records within a date range with optional filters.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of filtered SLA tracking records.</returns>
    Task<IReadOnlyList<SlaTrackingRecord>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? storeId = null,
        SlaStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregate SLA statistics for a specific store within a date range.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SLA statistics for the store.</returns>
    Task<SlaStoreStatistics> GetStoreStatisticsAsync(
        Guid storeId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregate SLA statistics for all stores within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of SLA statistics per store.</returns>
    Task<IReadOnlyList<SlaStoreStatistics>> GetAllStoreStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new SLA tracking record.
    /// </summary>
    /// <param name="record">The tracking record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added tracking record.</returns>
    Task<SlaTrackingRecord> AddAsync(SlaTrackingRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing SLA tracking record.
    /// </summary>
    /// <param name="record">The tracking record to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(SlaTrackingRecord record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents aggregate SLA statistics for a store.
/// </summary>
public class SlaStoreStatistics
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of cases in the period.
    /// </summary>
    public int TotalCases { get; set; }

    /// <summary>
    /// Gets or sets the number of cases resolved within SLA.
    /// </summary>
    public int CasesResolvedWithinSla { get; set; }

    /// <summary>
    /// Gets or sets the number of cases with first response within SLA.
    /// </summary>
    public int CasesRespondedWithinSla { get; set; }

    /// <summary>
    /// Gets or sets the number of cases with first response SLA breached.
    /// </summary>
    public int FirstResponseBreaches { get; set; }

    /// <summary>
    /// Gets or sets the number of cases with resolution SLA breached.
    /// </summary>
    public int ResolutionBreaches { get; set; }

    /// <summary>
    /// Gets or sets the average response time in hours.
    /// </summary>
    public double AverageResponseTimeHours { get; set; }

    /// <summary>
    /// Gets or sets the average resolution time in hours.
    /// </summary>
    public double AverageResolutionTimeHours { get; set; }

    /// <summary>
    /// Gets the percentage of cases resolved within SLA.
    /// </summary>
    public double SlaCompliancePercentage => TotalCases > 0
        ? Math.Round((double)CasesResolvedWithinSla / TotalCases * 100, 2)
        : 0;

    /// <summary>
    /// Gets the percentage of cases with first response within SLA.
    /// </summary>
    public double FirstResponseCompliancePercentage => TotalCases > 0
        ? Math.Round((double)CasesRespondedWithinSla / TotalCases * 100, 2)
        : 0;
}
