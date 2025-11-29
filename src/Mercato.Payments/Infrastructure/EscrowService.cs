using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for escrow operations.
/// </summary>
public class EscrowService : IEscrowService
{
    private readonly IEscrowRepository _escrowRepository;
    private readonly ILogger<EscrowService> _logger;
    private readonly EscrowSettings _escrowSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="EscrowService"/> class.
    /// </summary>
    /// <param name="escrowRepository">The escrow repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="escrowSettings">The escrow settings.</param>
    public EscrowService(
        IEscrowRepository escrowRepository,
        ILogger<EscrowService> logger,
        IOptions<EscrowSettings> escrowSettings)
    {
        _escrowRepository = escrowRepository;
        _logger = logger;
        _escrowSettings = escrowSettings.Value;
    }

    /// <inheritdoc />
    public async Task<HoldEscrowResult> HoldEscrowAsync(HoldEscrowCommand command)
    {
        var errors = ValidateHoldEscrowCommand(command);
        if (errors.Count > 0)
        {
            return HoldEscrowResult.Failure(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var entries = new List<EscrowEntry>();

        foreach (var allocation in command.SellerAllocations)
        {
            var entry = new EscrowEntry
            {
                Id = Guid.NewGuid(),
                PaymentTransactionId = command.PaymentTransactionId,
                OrderId = command.OrderId,
                SellerId = allocation.SellerId,
                Amount = allocation.Amount,
                Currency = command.Currency,
                Status = EscrowStatus.Held,
                CreatedAt = now,
                LastUpdatedAt = now,
                IsEligibleForPayout = false,
                AuditNote = command.AuditNote ?? $"Escrow created on payment confirmation. Eligible for payout after {_escrowSettings.PayoutEligibilityDays} days."
            };
            entries.Add(entry);
        }

        await _escrowRepository.AddRangeAsync(entries);

        _logger.LogInformation(
            "Escrow held for order {OrderId}: {EntryCount} entries created, total amount: {TotalAmount} {Currency}",
            command.OrderId,
            entries.Count,
            entries.Sum(e => e.Amount),
            command.Currency);

        return HoldEscrowResult.Success(entries);
    }

    /// <inheritdoc />
    public async Task<ReleaseEscrowResult> ReleaseEscrowAsync(ReleaseEscrowCommand command)
    {
        var errors = ValidateReleaseEscrowCommand(command);
        if (errors.Count > 0)
        {
            return ReleaseEscrowResult.Failure(errors);
        }

        var entries = await _escrowRepository.GetByOrderIdAsync(command.OrderId);
        
        if (entries.Count == 0)
        {
            return ReleaseEscrowResult.Failure("No escrow entries found for the specified order.");
        }

        // Filter by seller if specified
        var entriesToRelease = command.SellerId.HasValue
            ? entries.Where(e => e.SellerId == command.SellerId.Value).ToList()
            : entries.ToList();

        if (entriesToRelease.Count == 0)
        {
            return ReleaseEscrowResult.Failure("No escrow entries found for the specified seller.");
        }

        // Check if any entries are already released or refunded
        var alreadyProcessed = entriesToRelease.Where(e => e.Status != EscrowStatus.Held).ToList();
        if (alreadyProcessed.Count > 0)
        {
            var alreadyReleasedCount = alreadyProcessed.Count(e => e.Status == EscrowStatus.Released);
            var alreadyRefundedCount = alreadyProcessed.Count(e => e.Status == EscrowStatus.Refunded);
            
            if (alreadyReleasedCount > 0)
            {
                return ReleaseEscrowResult.Failure($"Escrow has already been released for {alreadyReleasedCount} entry(ies).");
            }
            if (alreadyRefundedCount > 0)
            {
                return ReleaseEscrowResult.Failure($"Escrow has already been refunded for {alreadyRefundedCount} entry(ies).");
            }
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entriesToRelease)
        {
            entry.Status = EscrowStatus.Released;
            entry.ReleasedAt = now;
            entry.LastUpdatedAt = now;
            entry.AuditNote = command.AuditNote ?? "Escrow released after order fulfillment.";
        }

        await _escrowRepository.UpdateRangeAsync(entriesToRelease);

        _logger.LogInformation(
            "Escrow released for order {OrderId}: {EntryCount} entries released, total amount: {TotalAmount}",
            command.OrderId,
            entriesToRelease.Count,
            entriesToRelease.Sum(e => e.Amount));

        return ReleaseEscrowResult.Success(entriesToRelease);
    }

    /// <inheritdoc />
    public async Task<RefundEscrowResult> RefundEscrowAsync(RefundEscrowCommand command)
    {
        var errors = ValidateRefundEscrowCommand(command);
        if (errors.Count > 0)
        {
            return RefundEscrowResult.Failure(errors);
        }

        var entries = await _escrowRepository.GetByOrderIdAsync(command.OrderId);
        
        if (entries.Count == 0)
        {
            return RefundEscrowResult.Failure("No escrow entries found for the specified order.");
        }

        // Filter by seller if specified
        var entriesToRefund = command.SellerId.HasValue
            ? entries.Where(e => e.SellerId == command.SellerId.Value).ToList()
            : entries.ToList();

        if (entriesToRefund.Count == 0)
        {
            return RefundEscrowResult.Failure("No escrow entries found for the specified seller.");
        }

        // Check if any entries are already released or refunded
        var alreadyProcessed = entriesToRefund.Where(e => e.Status != EscrowStatus.Held).ToList();
        if (alreadyProcessed.Count > 0)
        {
            var alreadyReleasedCount = alreadyProcessed.Count(e => e.Status == EscrowStatus.Released);
            var alreadyRefundedCount = alreadyProcessed.Count(e => e.Status == EscrowStatus.Refunded);
            
            if (alreadyRefundedCount > 0)
            {
                return RefundEscrowResult.Failure($"Escrow has already been refunded for {alreadyRefundedCount} entry(ies).");
            }
            if (alreadyReleasedCount > 0)
            {
                return RefundEscrowResult.Failure($"Escrow has already been released for {alreadyReleasedCount} entry(ies). Cannot refund released escrow.");
            }
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entriesToRefund)
        {
            entry.Status = EscrowStatus.Refunded;
            entry.RefundedAt = now;
            entry.LastUpdatedAt = now;
            entry.AuditNote = command.AuditNote ?? "Escrow refunded due to order cancellation.";
        }

        await _escrowRepository.UpdateRangeAsync(entriesToRefund);

        _logger.LogInformation(
            "Escrow refunded for order {OrderId}: {EntryCount} entries refunded, total amount: {TotalAmount}",
            command.OrderId,
            entriesToRefund.Count,
            entriesToRefund.Sum(e => e.Amount));

        return RefundEscrowResult.Success(entriesToRefund);
    }

    /// <inheritdoc />
    public async Task<GetEscrowEntriesResult> GetEscrowEntriesByOrderIdAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            return GetEscrowEntriesResult.Failure("Order ID is required.");
        }

        var entries = await _escrowRepository.GetByOrderIdAsync(orderId);
        return GetEscrowEntriesResult.Success(entries);
    }

    /// <inheritdoc />
    public async Task<GetEscrowEntriesResult> GetEscrowEntriesBySellerIdAsync(Guid sellerId, EscrowStatus? status = null)
    {
        if (sellerId == Guid.Empty)
        {
            return GetEscrowEntriesResult.Failure("Seller ID is required.");
        }

        IReadOnlyList<EscrowEntry> entries;
        
        if (status.HasValue)
        {
            entries = await _escrowRepository.GetBySellerIdAndStatusAsync(sellerId, status.Value);
        }
        else
        {
            entries = await _escrowRepository.GetBySellerIdAsync(sellerId);
        }

        return GetEscrowEntriesResult.Success(entries);
    }

    private static List<string> ValidateHoldEscrowCommand(HoldEscrowCommand command)
    {
        var errors = new List<string>();

        if (command.PaymentTransactionId == Guid.Empty)
        {
            errors.Add("Payment transaction ID is required.");
        }

        if (command.OrderId == Guid.Empty)
        {
            errors.Add("Order ID is required.");
        }

        if (command.SellerAllocations == null || command.SellerAllocations.Count == 0)
        {
            errors.Add("At least one seller allocation is required.");
        }
        else
        {
            var hasEmptySellerId = false;
            var hasInvalidAmount = false;

            foreach (var allocation in command.SellerAllocations)
            {
                if (allocation.SellerId == Guid.Empty && !hasEmptySellerId)
                {
                    errors.Add("Seller ID is required for all allocations.");
                    hasEmptySellerId = true;
                }

                if (allocation.Amount <= 0 && !hasInvalidAmount)
                {
                    errors.Add("Amount must be greater than zero for all allocations.");
                    hasInvalidAmount = true;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            errors.Add("Currency is required.");
        }

        return errors;
    }

    private static List<string> ValidateReleaseEscrowCommand(ReleaseEscrowCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
        {
            errors.Add("Order ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRefundEscrowCommand(RefundEscrowCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
        {
            errors.Add("Order ID is required.");
        }

        return errors;
    }
}
