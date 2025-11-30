namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for user analytics data aggregations.
/// </summary>
public interface IUserAnalyticsRepository
{
    /// <summary>
    /// Gets the count of unique users who successfully logged in within the specified period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of unique users who logged in.</returns>
    Task<int> GetUsersLoggedInCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unique users who successfully logged in within the specified period, grouped by role.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary with user role as key and count as value.</returns>
    Task<Dictionary<string, int>> GetUsersLoggedInByRoleAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unique users who placed at least one order within the specified period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of unique users who placed orders.</returns>
    Task<int> GetUsersWhoPlacedOrdersCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of new buyer registrations within the specified period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of new buyer accounts.</returns>
    Task<int> GetNewBuyerRegistrationsCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of new seller registrations within the specified period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of new seller accounts.</returns>
    Task<int> GetNewSellerRegistrationsCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);
}
