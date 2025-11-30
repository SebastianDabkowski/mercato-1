using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for checking conditions that may block account deletion.
/// </summary>
public interface IAccountDeletionCheckService
{
    /// <summary>
    /// Checks whether the specified user has any conditions that block account deletion.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>A result containing any blocking conditions found.</returns>
    Task<AccountDeletionCheckResult> CheckBlockingConditionsAsync(string userId);
}

/// <summary>
/// Represents the result of checking for blocking conditions.
/// </summary>
public class AccountDeletionCheckResult
{
    /// <summary>
    /// Gets a value indicating whether the user can proceed with account deletion.
    /// </summary>
    public bool CanDelete { get; init; }

    /// <summary>
    /// Gets the list of blocking conditions that prevent deletion.
    /// </summary>
    public IReadOnlyList<string> BlockingConditions { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user has open disputes.
    /// </summary>
    public bool HasOpenDisputes { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user has pending refunds.
    /// </summary>
    public bool HasPendingRefunds { get; init; }

    /// <summary>
    /// Gets the count of open disputes.
    /// </summary>
    public int OpenDisputeCount { get; init; }

    /// <summary>
    /// Gets the count of pending refunds.
    /// </summary>
    public int PendingRefundCount { get; init; }

    /// <summary>
    /// Creates a result indicating deletion can proceed.
    /// </summary>
    public static AccountDeletionCheckResult CanProceed()
    {
        return new AccountDeletionCheckResult
        {
            CanDelete = true,
            BlockingConditions = []
        };
    }

    /// <summary>
    /// Creates a result indicating deletion is blocked.
    /// </summary>
    /// <param name="blockingConditions">The conditions blocking deletion.</param>
    /// <param name="hasOpenDisputes">Whether there are open disputes.</param>
    /// <param name="openDisputeCount">The count of open disputes.</param>
    /// <param name="hasPendingRefunds">Whether there are pending refunds.</param>
    /// <param name="pendingRefundCount">The count of pending refunds.</param>
    public static AccountDeletionCheckResult CannotProceed(
        IReadOnlyList<string> blockingConditions,
        bool hasOpenDisputes = false,
        int openDisputeCount = 0,
        bool hasPendingRefunds = false,
        int pendingRefundCount = 0)
    {
        return new AccountDeletionCheckResult
        {
            CanDelete = false,
            BlockingConditions = blockingConditions,
            HasOpenDisputes = hasOpenDisputes,
            OpenDisputeCount = openDisputeCount,
            HasPendingRefunds = hasPendingRefunds,
            PendingRefundCount = pendingRefundCount
        };
    }
}
