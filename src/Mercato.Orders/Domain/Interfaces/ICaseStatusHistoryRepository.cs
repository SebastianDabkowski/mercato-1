using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for case status history data access operations.
/// </summary>
public interface ICaseStatusHistoryRepository
{
    /// <summary>
    /// Gets all status history entries for a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <returns>A list of status history entries ordered by date.</returns>
    Task<IReadOnlyList<CaseStatusHistory>> GetByReturnRequestIdAsync(Guid returnRequestId);

    /// <summary>
    /// Adds a new status history entry.
    /// </summary>
    /// <param name="history">The status history entry to add.</param>
    /// <returns>The added status history entry.</returns>
    Task<CaseStatusHistory> AddAsync(CaseStatusHistory history);
}
