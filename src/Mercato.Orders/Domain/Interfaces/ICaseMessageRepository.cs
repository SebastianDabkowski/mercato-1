using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for case message data access operations.
/// </summary>
public interface ICaseMessageRepository
{
    /// <summary>
    /// Gets all messages for a specific return request in chronological order.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <returns>A list of messages for the return request.</returns>
    Task<IReadOnlyList<CaseMessage>> GetByReturnRequestIdAsync(Guid returnRequestId);

    /// <summary>
    /// Adds a new message to the repository.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <returns>The added message.</returns>
    Task<CaseMessage> AddAsync(CaseMessage message);
}
