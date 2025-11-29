using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for commission operations.
/// </summary>
public class CommissionService : ICommissionService
{
    private readonly ICommissionRuleRepository _ruleRepository;
    private readonly ICommissionRecordRepository _recordRepository;
    private readonly ILogger<CommissionService> _logger;
    private readonly CommissionSettings _commissionSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionService"/> class.
    /// </summary>
    /// <param name="ruleRepository">The commission rule repository.</param>
    /// <param name="recordRepository">The commission record repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="commissionSettings">The commission settings.</param>
    public CommissionService(
        ICommissionRuleRepository ruleRepository,
        ICommissionRecordRepository recordRepository,
        ILogger<CommissionService> logger,
        IOptions<CommissionSettings> commissionSettings)
    {
        _ruleRepository = ruleRepository;
        _recordRepository = recordRepository;
        _logger = logger;
        _commissionSettings = commissionSettings.Value;
    }

    /// <inheritdoc />
    public async Task<CalculateCommissionResult> CalculateCommissionAsync(CalculateCommissionCommand command)
    {
        var errors = ValidateCalculateCommissionCommand(command);
        if (errors.Count > 0)
        {
            return CalculateCommissionResult.Failure(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var records = new List<CommissionRecord>();

        foreach (var allocation in command.SellerAllocations)
        {
            var rule = await _ruleRepository.GetBestMatchingRuleAsync(allocation.SellerId, allocation.CategoryId);

            var commissionRate = rule?.CommissionRate ?? _commissionSettings.DefaultCommissionRate;
            var calculatedCommission = allocation.Amount * (commissionRate / 100m);

            // Apply min/max constraints if a rule was found
            if (rule != null)
            {
                if (rule.MinCommission.HasValue && calculatedCommission < rule.MinCommission.Value)
                {
                    calculatedCommission = rule.MinCommission.Value;
                }
                if (rule.MaxCommission.HasValue && calculatedCommission > rule.MaxCommission.Value)
                {
                    calculatedCommission = rule.MaxCommission.Value;
                }
            }

            var record = new CommissionRecord
            {
                Id = Guid.NewGuid(),
                PaymentTransactionId = command.PaymentTransactionId,
                OrderId = command.OrderId,
                SellerId = allocation.SellerId,
                OrderAmount = allocation.Amount,
                CommissionRate = commissionRate,
                CommissionAmount = calculatedCommission,
                RefundedAmount = 0m,
                RefundedCommissionAmount = 0m,
                NetCommissionAmount = calculatedCommission,
                AppliedRuleId = rule?.Id,
                AppliedRuleDescription = rule?.GetDescription() ?? $"Default rate: {_commissionSettings.DefaultCommissionRate:F2}%",
                CreatedAt = now,
                LastUpdatedAt = now,
                CalculatedAt = now,
                LastRefundRecalculatedAt = null
            };

            records.Add(record);
        }

        await _recordRepository.AddRangeAsync(records);

        _logger.LogInformation(
            "Commission calculated for order {OrderId}: {RecordCount} records created, total commission: {TotalCommission}",
            command.OrderId,
            records.Count,
            records.Sum(r => r.CommissionAmount));

        return CalculateCommissionResult.Success(records);
    }

    /// <inheritdoc />
    public async Task<RecalculatePartialRefundResult> RecalculatePartialRefundAsync(RecalculatePartialRefundCommand command)
    {
        var errors = ValidateRecalculatePartialRefundCommand(command);
        if (errors.Count > 0)
        {
            return RecalculatePartialRefundResult.Failure(errors);
        }

        var record = await _recordRepository.GetByOrderIdAndSellerIdAsync(command.OrderId, command.SellerId);
        if (record == null)
        {
            return RecalculatePartialRefundResult.Failure("No commission record found for the specified order and seller.");
        }

        // Calculate the remaining refundable amount
        var remainingOrderAmount = record.OrderAmount - record.RefundedAmount;
        if (command.RefundAmount > remainingOrderAmount)
        {
            return RecalculatePartialRefundResult.Failure(
                $"Refund amount ({command.RefundAmount:F2}) exceeds remaining order amount ({remainingOrderAmount:F2}).");
        }

        // Calculate proportional commission to refund
        // The refund commission is proportional to the refund amount relative to the original order amount
        var refundCommission = record.CommissionAmount * (command.RefundAmount / record.OrderAmount);

        var now = DateTimeOffset.UtcNow;

        record.RefundedAmount += command.RefundAmount;
        record.RefundedCommissionAmount += refundCommission;
        record.NetCommissionAmount = record.CommissionAmount - record.RefundedCommissionAmount;
        record.LastUpdatedAt = now;
        record.LastRefundRecalculatedAt = now;

        await _recordRepository.UpdateAsync(record);

        _logger.LogInformation(
            "Partial refund recalculated for order {OrderId}, seller {SellerId}: refunded {RefundAmount}, commission refunded {RefundCommission}",
            command.OrderId,
            command.SellerId,
            command.RefundAmount,
            refundCommission);

        return RecalculatePartialRefundResult.Success(record);
    }

    /// <inheritdoc />
    public async Task<GetCommissionRecordsResult> GetCommissionRecordsByOrderIdAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            return GetCommissionRecordsResult.Failure("Order ID is required.");
        }

        var records = await _recordRepository.GetByOrderIdAsync(orderId);
        return GetCommissionRecordsResult.Success(records);
    }

    /// <inheritdoc />
    public async Task<GetCommissionRecordsResult> GetCommissionRecordsBySellerIdAsync(Guid sellerId)
    {
        if (sellerId == Guid.Empty)
        {
            return GetCommissionRecordsResult.Failure("Seller ID is required.");
        }

        var records = await _recordRepository.GetBySellerIdAsync(sellerId);
        return GetCommissionRecordsResult.Success(records);
    }

    private static List<string> ValidateCalculateCommissionCommand(CalculateCommissionCommand command)
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
            if (command.SellerAllocations.Any(a => a.SellerId == Guid.Empty))
            {
                errors.Add("Seller ID is required for all allocations.");
            }

            if (command.SellerAllocations.Any(a => a.Amount <= 0))
            {
                errors.Add("Amount must be greater than zero for all allocations.");
            }
        }

        return errors;
    }

    private static List<string> ValidateRecalculatePartialRefundCommand(RecalculatePartialRefundCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
        {
            errors.Add("Order ID is required.");
        }

        if (command.SellerId == Guid.Empty)
        {
            errors.Add("Seller ID is required.");
        }

        if (command.RefundAmount <= 0)
        {
            errors.Add("Refund amount must be greater than zero.");
        }

        return errors;
    }
}
