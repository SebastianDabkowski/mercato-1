using Mercato.Identity.Application.Services;
using Microsoft.Extensions.Logging;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Service implementation for checking conditions that may block account deletion.
/// </summary>
public class AccountDeletionCheckService : IAccountDeletionCheckService
{
    private readonly IAccountDeletionDataProvider? _dataProvider;
    private readonly ILogger<AccountDeletionCheckService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountDeletionCheckService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataProvider">Optional data provider for checking conditions across modules.</param>
    public AccountDeletionCheckService(
        ILogger<AccountDeletionCheckService> logger,
        IAccountDeletionDataProvider? dataProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataProvider = dataProvider;
    }

    /// <inheritdoc/>
    public async Task<AccountDeletionCheckResult> CheckBlockingConditionsAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var blockingConditions = new List<string>();
        var hasOpenDisputes = false;
        var openDisputeCount = 0;
        var hasPendingRefunds = false;
        var pendingRefundCount = 0;

        if (_dataProvider != null)
        {
            // Check for open disputes
            openDisputeCount = await _dataProvider.GetOpenDisputeCountAsync(userId);
            if (openDisputeCount > 0)
            {
                hasOpenDisputes = true;
                blockingConditions.Add($"You have {openDisputeCount} open dispute(s) that must be resolved before account deletion.");
                _logger.LogInformation(
                    "Account deletion blocked for user {UserId}: {Count} open disputes",
                    userId,
                    openDisputeCount);
            }

            // Check for pending refunds
            pendingRefundCount = await _dataProvider.GetPendingRefundCountAsync(userId);
            if (pendingRefundCount > 0)
            {
                hasPendingRefunds = true;
                blockingConditions.Add($"You have {pendingRefundCount} pending refund(s) that must be completed before account deletion.");
                _logger.LogInformation(
                    "Account deletion blocked for user {UserId}: {Count} pending refunds",
                    userId,
                    pendingRefundCount);
            }
        }
        else
        {
            _logger.LogWarning(
                "No data provider configured for account deletion checks. Proceeding without blocking condition checks for user {UserId}.",
                userId);
        }

        if (blockingConditions.Count > 0)
        {
            return AccountDeletionCheckResult.CannotProceed(
                blockingConditions.AsReadOnly(),
                hasOpenDisputes,
                openDisputeCount,
                hasPendingRefunds,
                pendingRefundCount);
        }

        return AccountDeletionCheckResult.CanProceed();
    }
}
