using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for payout operations.
/// </summary>
public class PayoutService : IPayoutService
{
    private readonly IPayoutRepository _payoutRepository;
    private readonly IEscrowRepository _escrowRepository;
    private readonly ILogger<PayoutService> _logger;
    private readonly PayoutSettings _payoutSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayoutService"/> class.
    /// </summary>
    /// <param name="payoutRepository">The payout repository.</param>
    /// <param name="escrowRepository">The escrow repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="payoutSettings">The payout settings.</param>
    public PayoutService(
        IPayoutRepository payoutRepository,
        IEscrowRepository escrowRepository,
        ILogger<PayoutService> logger,
        IOptions<PayoutSettings> payoutSettings)
    {
        _payoutRepository = payoutRepository;
        _escrowRepository = escrowRepository;
        _logger = logger;
        _payoutSettings = payoutSettings.Value;
    }

    /// <inheritdoc />
    public async Task<SchedulePayoutsResult> SchedulePayoutsAsync(SchedulePayoutsCommand command)
    {
        var errors = ValidateSchedulePayoutsCommand(command);
        if (errors.Count > 0)
        {
            return SchedulePayoutsResult.Failure(errors);
        }

        // Get all released escrow entries that are eligible for payout
        var allReleasedEntries = await GetEligibleEscrowEntriesAsync();
        
        if (allReleasedEntries.Count == 0)
        {
            _logger.LogInformation("No eligible escrow entries found for payout scheduling.");
            return SchedulePayoutsResult.Success([], 0);
        }

        // Group by seller and aggregate balances
        var sellerBalances = allReleasedEntries
            .GroupBy(e => new { e.SellerId, e.Currency })
            .Select(g => new
            {
                g.Key.SellerId,
                g.Key.Currency,
                TotalAmount = g.Sum(e => e.Amount),
                EscrowEntryIds = g.Select(e => e.Id).ToList()
            })
            .ToList();

        var scheduledPayouts = new List<Payout>();
        var rolledOverCount = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var balance in sellerBalances)
        {
            // Check if balance meets minimum threshold
            if (balance.TotalAmount < _payoutSettings.MinimumPayoutThreshold)
            {
                rolledOverCount++;
                _logger.LogInformation(
                    "Seller {SellerId} balance {Amount} {Currency} is below threshold {Threshold}. Rolling over to next payout period.",
                    balance.SellerId,
                    balance.TotalAmount,
                    balance.Currency,
                    _payoutSettings.MinimumPayoutThreshold);
                continue;
            }

            var payout = new Payout
            {
                Id = Guid.NewGuid(),
                SellerId = balance.SellerId,
                Amount = balance.TotalAmount,
                Currency = balance.Currency,
                Status = PayoutStatus.Scheduled,
                ScheduleFrequency = command.ScheduleFrequency,
                ScheduledAt = command.ScheduledAt,
                CreatedAt = now,
                LastUpdatedAt = now,
                RetryCount = 0,
                AuditNote = command.AuditNote ?? $"Scheduled payout for {command.ScheduleFrequency} cycle.",
                EscrowEntryIds = string.Join(",", balance.EscrowEntryIds)
            };

            scheduledPayouts.Add(payout);
        }

        if (scheduledPayouts.Count > 0)
        {
            await _payoutRepository.AddRangeAsync(scheduledPayouts);

            // Mark escrow entries as eligible for payout
            var allEntryIds = scheduledPayouts
                .SelectMany(p => p.EscrowEntryIds?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
                .Select(id => Guid.TryParse(id, out var parsedId) ? parsedId : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            var entriesToUpdate = allReleasedEntries.Where(e => allEntryIds.Contains(e.Id)).ToList();
            foreach (var entry in entriesToUpdate)
            {
                entry.IsEligibleForPayout = true;
                entry.LastUpdatedAt = now;
            }
            await _escrowRepository.UpdateRangeAsync(entriesToUpdate);
        }

        _logger.LogInformation(
            "Scheduled {PayoutCount} payouts for {ScheduledAt}. {RolledOverCount} sellers below threshold.",
            scheduledPayouts.Count,
            command.ScheduledAt,
            rolledOverCount);

        return SchedulePayoutsResult.Success(scheduledPayouts, rolledOverCount);
    }

    /// <inheritdoc />
    public async Task<ProcessPayoutsResult> ProcessPayoutsAsync(ProcessPayoutsCommand command)
    {
        var processBefore = command.ProcessBefore ?? DateTimeOffset.UtcNow;
        var scheduledPayouts = await _payoutRepository.GetScheduledPayoutsAsync(processBefore);

        if (scheduledPayouts.Count == 0)
        {
            _logger.LogInformation("No scheduled payouts found to process.");
            return ProcessPayoutsResult.Success([], null, 0, 0);
        }

        var now = DateTimeOffset.UtcNow;
        Guid? batchId = command.CreateBatch ? Guid.NewGuid() : null;
        var successCount = 0;
        var failedCount = 0;

        // Apply batching limit if enabled
        var payoutsToProcess = _payoutSettings.EnableBatching
            ? scheduledPayouts.Take(_payoutSettings.MaxPayoutsPerBatch).ToList()
            : scheduledPayouts.ToList();

        foreach (var payout in payoutsToProcess)
        {
            payout.Status = PayoutStatus.Processing;
            payout.ProcessingStartedAt = now;
            payout.LastUpdatedAt = now;
            payout.BatchId = batchId;
        }

        await _payoutRepository.UpdateRangeAsync(payoutsToProcess);

        // Simulate processing each payout
        foreach (var payout in payoutsToProcess)
        {
            try
            {
                // In a real implementation, this would call the payment provider
                // For now, we mark all as paid
                payout.Status = PayoutStatus.Paid;
                payout.CompletedAt = DateTimeOffset.UtcNow;
                payout.LastUpdatedAt = DateTimeOffset.UtcNow;
                payout.AuditNote = command.AuditNote ?? "Payout processed successfully.";
                successCount++;
            }
            catch (Exception ex)
            {
                payout.Status = PayoutStatus.Failed;
                payout.CompletedAt = DateTimeOffset.UtcNow;
                payout.LastUpdatedAt = DateTimeOffset.UtcNow;
                payout.ErrorReference = Guid.NewGuid().ToString();
                payout.ErrorMessage = ex.Message;
                failedCount++;

                _logger.LogError(ex, 
                    "Failed to process payout {PayoutId} for seller {SellerId}. Error reference: {ErrorReference}",
                    payout.Id,
                    payout.SellerId,
                    payout.ErrorReference);
            }
        }

        await _payoutRepository.UpdateRangeAsync(payoutsToProcess);

        _logger.LogInformation(
            "Processed {TotalCount} payouts. Success: {SuccessCount}, Failed: {FailedCount}. BatchId: {BatchId}",
            payoutsToProcess.Count,
            successCount,
            failedCount,
            batchId);

        return ProcessPayoutsResult.Success(payoutsToProcess, batchId, successCount, failedCount);
    }

    /// <inheritdoc />
    public async Task<RetryPayoutsResult> RetryPayoutsAsync(RetryPayoutsCommand command)
    {
        List<Payout> payoutsToRetry;

        if (command.PayoutId.HasValue)
        {
            var payout = await _payoutRepository.GetByIdAsync(command.PayoutId.Value);
            if (payout == null)
            {
                return RetryPayoutsResult.Failure("Payout not found.");
            }
            if (payout.Status != PayoutStatus.Failed)
            {
                return RetryPayoutsResult.Failure("Only failed payouts can be retried.");
            }
            if (payout.RetryCount >= _payoutSettings.MaxRetryAttempts)
            {
                return RetryPayoutsResult.Failure($"Payout has exceeded maximum retry attempts ({_payoutSettings.MaxRetryAttempts}).");
            }
            payoutsToRetry = [payout];
        }
        else
        {
            payoutsToRetry = (await _payoutRepository.GetPayoutsForRetryAsync(_payoutSettings.MaxRetryAttempts)).ToList();
        }

        if (payoutsToRetry.Count == 0)
        {
            _logger.LogInformation("No payouts eligible for retry.");
            return RetryPayoutsResult.Success([], 0, 0);
        }

        var now = DateTimeOffset.UtcNow;
        var successCount = 0;
        var failedCount = 0;

        foreach (var payout in payoutsToRetry)
        {
            payout.RetryCount++;
            payout.Status = PayoutStatus.Processing;
            payout.ProcessingStartedAt = now;
            payout.LastUpdatedAt = now;
            payout.ErrorReference = null;
            payout.ErrorMessage = null;
        }

        await _payoutRepository.UpdateRangeAsync(payoutsToRetry);

        // Simulate retry processing
        foreach (var payout in payoutsToRetry)
        {
            try
            {
                // In a real implementation, this would retry the payment provider call
                payout.Status = PayoutStatus.Paid;
                payout.CompletedAt = DateTimeOffset.UtcNow;
                payout.LastUpdatedAt = DateTimeOffset.UtcNow;
                payout.AuditNote = command.AuditNote ?? $"Payout succeeded on retry attempt {payout.RetryCount}.";
                successCount++;
            }
            catch (Exception ex)
            {
                payout.Status = PayoutStatus.Failed;
                payout.CompletedAt = DateTimeOffset.UtcNow;
                payout.LastUpdatedAt = DateTimeOffset.UtcNow;
                payout.ErrorReference = Guid.NewGuid().ToString();
                payout.ErrorMessage = ex.Message;
                failedCount++;

                _logger.LogError(ex,
                    "Retry failed for payout {PayoutId} (attempt {RetryCount}). Error reference: {ErrorReference}",
                    payout.Id,
                    payout.RetryCount,
                    payout.ErrorReference);
            }
        }

        await _payoutRepository.UpdateRangeAsync(payoutsToRetry);

        _logger.LogInformation(
            "Retried {TotalCount} payouts. Success: {SuccessCount}, Failed: {FailedCount}",
            payoutsToRetry.Count,
            successCount,
            failedCount);

        return RetryPayoutsResult.Success(payoutsToRetry, successCount, failedCount);
    }

    /// <inheritdoc />
    public async Task<GetPayoutResult> GetPayoutAsync(Guid payoutId)
    {
        if (payoutId == Guid.Empty)
        {
            return GetPayoutResult.Failure("Payout ID is required.");
        }

        var payout = await _payoutRepository.GetByIdAsync(payoutId);
        if (payout == null)
        {
            return GetPayoutResult.Failure("Payout not found.");
        }

        return GetPayoutResult.Success(payout);
    }

    /// <inheritdoc />
    public async Task<GetPayoutsResult> GetPayoutsBySellerIdAsync(Guid sellerId, PayoutStatus? status = null)
    {
        if (sellerId == Guid.Empty)
        {
            return GetPayoutsResult.Failure("Seller ID is required.");
        }

        IReadOnlyList<Payout> payouts;

        if (status.HasValue)
        {
            payouts = await _payoutRepository.GetBySellerIdAndStatusAsync(sellerId, status.Value);
        }
        else
        {
            payouts = await _payoutRepository.GetBySellerIdAsync(sellerId);
        }

        return GetPayoutsResult.Success(payouts);
    }

    /// <inheritdoc />
    public async Task<GetPayoutsResult> GetPayoutsFilteredAsync(GetPayoutsFilteredQuery query)
    {
        var errors = ValidateGetPayoutsFilteredQuery(query);
        if (errors.Count > 0)
        {
            return GetPayoutsResult.Failure(errors);
        }

        var payouts = await _payoutRepository.GetBySellerIdWithFiltersAsync(
            query.SellerId,
            query.Status,
            query.FromDate,
            query.ToDate);

        return GetPayoutsResult.Success(payouts);
    }

    private async Task<IReadOnlyList<EscrowEntry>> GetEligibleEscrowEntriesAsync()
    {
        // Get all released escrow entries that haven't been included in a payout yet
        return await _escrowRepository.GetByStatusForPayoutAsync(EscrowStatus.Released, excludeAlreadyInPayout: true);
    }

    private static List<string> ValidateSchedulePayoutsCommand(SchedulePayoutsCommand command)
    {
        var errors = new List<string>();

        if (command.ScheduledAt == default)
        {
            errors.Add("Scheduled date is required.");
        }

        return errors;
    }

    private static List<string> ValidateGetPayoutsFilteredQuery(GetPayoutsFilteredQuery query)
    {
        var errors = new List<string>();

        if (query.SellerId == Guid.Empty)
        {
            errors.Add("Seller ID is required.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            errors.Add("From date must be before or equal to To date.");
        }

        return errors;
    }
}
