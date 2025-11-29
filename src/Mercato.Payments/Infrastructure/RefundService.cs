using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for refund operations.
/// </summary>
public class RefundService : IRefundService
{
    private readonly IRefundRepository _refundRepository;
    private readonly IEscrowService _escrowService;
    private readonly ICommissionService _commissionService;
    private readonly ILogger<RefundService> _logger;
    private readonly RefundSettings _refundSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundService"/> class.
    /// </summary>
    /// <param name="refundRepository">The refund repository.</param>
    /// <param name="escrowService">The escrow service.</param>
    /// <param name="commissionService">The commission service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="refundSettings">The refund settings.</param>
    public RefundService(
        IRefundRepository refundRepository,
        IEscrowService escrowService,
        ICommissionService commissionService,
        ILogger<RefundService> logger,
        IOptions<RefundSettings> refundSettings)
    {
        _refundRepository = refundRepository;
        _escrowService = escrowService;
        _commissionService = commissionService;
        _logger = logger;
        _refundSettings = refundSettings.Value;
    }

    /// <inheritdoc />
    public async Task<ProcessRefundResult> ProcessFullRefundAsync(ProcessFullRefundCommand command)
    {
        var errors = ValidateFullRefundCommand(command);
        if (errors.Count > 0)
        {
            return ProcessRefundResult.Failure(errors);
        }

        // Get escrow entries to calculate total refund amount
        var escrowResult = await _escrowService.GetEscrowEntriesByOrderIdAsync(command.OrderId);
        if (!escrowResult.Succeeded || escrowResult.Entries.Count == 0)
        {
            return ProcessRefundResult.Failure("No escrow entries found for the order.");
        }

        // Calculate total refund amount from remaining escrow
        var totalRefundAmount = escrowResult.Entries.Sum(e => e.Amount - e.RefundedAmount);
        if (totalRefundAmount <= 0)
        {
            return ProcessRefundResult.Failure("No funds available for refund.");
        }

        var now = DateTimeOffset.UtcNow;

        // Create refund record
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = command.PaymentTransactionId,
            OrderId = command.OrderId,
            SellerId = null, // Full refund covers all sellers
            Amount = totalRefundAmount,
            Currency = escrowResult.Entries.First().Currency,
            Type = RefundType.Full,
            Status = RefundStatus.Processing,
            Reason = command.Reason,
            InitiatedByUserId = command.InitiatedByUserId,
            InitiatedByRole = command.InitiatedByRole,
            CreatedAt = now,
            LastUpdatedAt = now,
            AuditNote = command.AuditNote ?? $"Full refund initiated by {command.InitiatedByRole}"
        };

        await _refundRepository.AddAsync(refund);

        _logger.LogInformation(
            "Full refund initiated: RefundId={RefundId}, OrderId={OrderId}, Amount={Amount}",
            refund.Id, command.OrderId, totalRefundAmount);

        // Process escrow refunds for each seller
        decimal totalCommissionRefunded = 0m;
        decimal totalEscrowRefunded = 0m;

        try
        {
            foreach (var entry in escrowResult.Entries.Where(e => e.Status == EscrowStatus.Held || e.Status == EscrowStatus.PartiallyRefunded))
            {
                var remainingAmount = entry.Amount - entry.RefundedAmount;
                if (remainingAmount <= 0)
                {
                    continue;
                }

                // Refund escrow
                var escrowRefundResult = await _escrowService.RefundEscrowAsync(new RefundEscrowCommand
                {
                    OrderId = command.OrderId,
                    SellerId = entry.SellerId,
                    AuditNote = $"Full refund - RefundId: {refund.Id}"
                });

                if (!escrowRefundResult.Succeeded)
                {
                    // Log the error but continue processing other sellers
                    LogProviderError(refund.Id, $"Failed to refund escrow for seller {entry.SellerId}: {string.Join(", ", escrowRefundResult.Errors)}");
                }
                else
                {
                    totalEscrowRefunded += remainingAmount;
                }

                // Recalculate commission for the refund
                var commissionResult = await _commissionService.RecalculatePartialRefundAsync(new RecalculatePartialRefundCommand
                {
                    OrderId = command.OrderId,
                    SellerId = entry.SellerId,
                    RefundAmount = remainingAmount
                });

                if (commissionResult.Succeeded && commissionResult.UpdatedRecord != null)
                {
                    totalCommissionRefunded += commissionResult.UpdatedRecord.RefundedCommissionAmount;
                }
            }

            // Update refund with completion details
            refund.Status = RefundStatus.Completed;
            refund.CompletedAt = DateTimeOffset.UtcNow;
            refund.CommissionRefunded = totalCommissionRefunded;
            refund.EscrowRefunded = totalEscrowRefunded;
            refund.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _refundRepository.UpdateAsync(refund);

            _logger.LogInformation(
                "Full refund completed: RefundId={RefundId}, EscrowRefunded={EscrowRefunded}, CommissionRefunded={CommissionRefunded}",
                refund.Id, totalEscrowRefunded, totalCommissionRefunded);

            return ProcessRefundResult.Success(refund);
        }
        catch (Exception ex)
        {
            // Log provider error
            LogProviderError(refund.Id, ex.Message);

            refund.Status = RefundStatus.Failed;
            refund.ErrorMessage = ex.Message;
            refund.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _refundRepository.UpdateAsync(refund);

            return ProcessRefundResult.ProviderError(ex.Message, refund);
        }
    }

    /// <inheritdoc />
    public async Task<ProcessRefundResult> ProcessPartialRefundAsync(ProcessPartialRefundCommand command)
    {
        var errors = ValidatePartialRefundCommand(command);
        if (errors.Count > 0)
        {
            return ProcessRefundResult.Failure(errors);
        }

        // Check if seller is specified for validation
        Guid sellerId;
        if (command.SellerId.HasValue)
        {
            sellerId = command.SellerId.Value;
        }
        else
        {
            // For partial refunds without seller, we need to find which seller's escrow to use
            var escrowResult = await _escrowService.GetEscrowEntriesByOrderIdAsync(command.OrderId);
            if (!escrowResult.Succeeded || escrowResult.Entries.Count == 0)
            {
                return ProcessRefundResult.Failure("No escrow entries found for the order.");
            }

            // Use the first seller with available balance (this is a simplified approach)
            var availableEntry = escrowResult.Entries
                .Where(e => e.Status == EscrowStatus.Held || e.Status == EscrowStatus.PartiallyRefunded)
                .FirstOrDefault(e => e.Amount - e.RefundedAmount >= command.Amount);

            if (availableEntry == null)
            {
                return ProcessRefundResult.Failure("No seller has sufficient escrow balance for this refund amount.");
            }

            sellerId = availableEntry.SellerId;
        }

        // Validate that the seller has enough balance
        var sellerEscrowResult = await _escrowService.GetEscrowEntriesByOrderIdAsync(command.OrderId);
        var sellerEntry = sellerEscrowResult.Entries.FirstOrDefault(e => e.SellerId == sellerId);

        if (sellerEntry == null)
        {
            return ProcessRefundResult.Failure("No escrow entry found for the specified seller.");
        }

        var remainingBalance = sellerEntry.Amount - sellerEntry.RefundedAmount;
        if (command.Amount > remainingBalance)
        {
            return ProcessRefundResult.Failure(
                $"Refund amount ({command.Amount:F2}) exceeds available balance ({remainingBalance:F2}).");
        }

        var now = DateTimeOffset.UtcNow;

        // Create refund record
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = command.PaymentTransactionId,
            OrderId = command.OrderId,
            SellerId = sellerId,
            Amount = command.Amount,
            Currency = sellerEntry.Currency,
            Type = RefundType.Partial,
            Status = RefundStatus.Processing,
            Reason = command.Reason,
            InitiatedByUserId = command.InitiatedByUserId,
            InitiatedByRole = command.InitiatedByRole,
            CreatedAt = now,
            LastUpdatedAt = now,
            AuditNote = command.AuditNote ?? $"Partial refund initiated by {command.InitiatedByRole}"
        };

        await _refundRepository.AddAsync(refund);

        _logger.LogInformation(
            "Partial refund initiated: RefundId={RefundId}, OrderId={OrderId}, SellerId={SellerId}, Amount={Amount}",
            refund.Id, command.OrderId, sellerId, command.Amount);

        try
        {
            // Process partial escrow refund
            var escrowRefundResult = await _escrowService.PartialRefundEscrowAsync(new PartialRefundEscrowCommand
            {
                OrderId = command.OrderId,
                SellerId = sellerId,
                Amount = command.Amount,
                AuditNote = $"Partial refund - RefundId: {refund.Id}"
            });

            if (!escrowRefundResult.Succeeded)
            {
                var errorMessage = string.Join(", ", escrowRefundResult.Errors);
                LogProviderError(refund.Id, $"Failed to process partial escrow refund: {errorMessage}");

                refund.Status = RefundStatus.Failed;
                refund.ErrorMessage = errorMessage;
                refund.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _refundRepository.UpdateAsync(refund);

                return ProcessRefundResult.ProviderError(errorMessage, refund);
            }

            // Recalculate commission
            var commissionResult = await _commissionService.RecalculatePartialRefundAsync(new RecalculatePartialRefundCommand
            {
                OrderId = command.OrderId,
                SellerId = sellerId,
                RefundAmount = command.Amount
            });

            decimal commissionRefunded = 0m;
            if (commissionResult.Succeeded && commissionResult.UpdatedRecord != null)
            {
                commissionRefunded = commissionResult.UpdatedRecord.RefundedCommissionAmount;
            }

            // Update refund with completion details
            refund.Status = RefundStatus.Completed;
            refund.CompletedAt = DateTimeOffset.UtcNow;
            refund.CommissionRefunded = commissionRefunded;
            refund.EscrowRefunded = command.Amount;
            refund.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _refundRepository.UpdateAsync(refund);

            _logger.LogInformation(
                "Partial refund completed: RefundId={RefundId}, EscrowRefunded={EscrowRefunded}, CommissionRefunded={CommissionRefunded}",
                refund.Id, command.Amount, commissionRefunded);

            return ProcessRefundResult.Success(refund);
        }
        catch (Exception ex)
        {
            LogProviderError(refund.Id, ex.Message);

            refund.Status = RefundStatus.Failed;
            refund.ErrorMessage = ex.Message;
            refund.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _refundRepository.UpdateAsync(refund);

            return ProcessRefundResult.ProviderError(ex.Message, refund);
        }
    }

    /// <inheritdoc />
    public async Task<GetRefundResult> GetRefundAsync(Guid refundId)
    {
        if (refundId == Guid.Empty)
        {
            return GetRefundResult.Failure("Refund ID is required.");
        }

        var refund = await _refundRepository.GetByIdAsync(refundId);
        if (refund == null)
        {
            return GetRefundResult.Failure("Refund not found.");
        }

        return GetRefundResult.Success(refund);
    }

    /// <inheritdoc />
    public async Task<GetRefundsResult> GetRefundsByOrderIdAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            return GetRefundsResult.Failure("Order ID is required.");
        }

        var refunds = await _refundRepository.GetByOrderIdAsync(orderId);
        var totalRefunded = refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount);

        return GetRefundsResult.Success(refunds, totalRefunded);
    }

    /// <inheritdoc />
    public async Task<RefundEligibilityResult> CheckSellerRefundEligibilityAsync(CheckRefundEligibilityCommand command)
    {
        var errors = ValidateCheckRefundEligibilityCommand(command);
        if (errors.Count > 0)
        {
            return RefundEligibilityResult.Failure(errors);
        }

        // Get seller's escrow entry
        var escrowResult = await _escrowService.GetEscrowEntriesByOrderIdAsync(command.OrderId);
        if (!escrowResult.Succeeded)
        {
            return RefundEligibilityResult.Failure("Unable to retrieve escrow information.");
        }

        var sellerEntry = escrowResult.Entries.FirstOrDefault(e => e.SellerId == command.SellerId);
        if (sellerEntry == null)
        {
            return RefundEligibilityResult.NotEligible("No escrow entry found for the seller on this order.");
        }

        // Check if escrow is in a refundable state
        if (sellerEntry.Status == EscrowStatus.Released)
        {
            return RefundEligibilityResult.NotEligible("Escrow has already been released to the seller.");
        }

        if (sellerEntry.Status == EscrowStatus.Refunded)
        {
            return RefundEligibilityResult.NotEligible("Escrow has already been fully refunded.");
        }

        // Check refund window (based on entry creation time)
        var daysSincePayment = (DateTimeOffset.UtcNow - sellerEntry.CreatedAt).TotalDays;
        if (daysSincePayment > _refundSettings.SellerRefundWindowDays)
        {
            return RefundEligibilityResult.NotEligible(
                $"Refund window has expired. Sellers can only initiate refunds within {_refundSettings.SellerRefundWindowDays} days of payment.");
        }

        // Check if seller partial refunds are allowed
        if (!_refundSettings.AllowSellerPartialRefunds)
        {
            return RefundEligibilityResult.NotEligible("Seller-initiated refunds are not allowed for this marketplace.");
        }

        // Calculate max refundable amount
        var remainingEscrow = sellerEntry.Amount - sellerEntry.RefundedAmount;
        var maxRefundableByPercentage = sellerEntry.Amount * (_refundSettings.MaxSellerRefundPercentage / 100m);
        var alreadyRefunded = await _refundRepository.GetTotalRefundedByOrderIdAndSellerIdAsync(command.OrderId, command.SellerId);
        var maxRefundable = Math.Min(remainingEscrow, maxRefundableByPercentage - alreadyRefunded);

        if (maxRefundable <= 0)
        {
            return RefundEligibilityResult.NotEligible("Maximum refund limit has been reached.");
        }

        // Check if requested amount is within limits
        if (command.Amount > maxRefundable)
        {
            return RefundEligibilityResult.NotEligible(
                $"Requested amount ({command.Amount:F2}) exceeds maximum refundable amount ({maxRefundable:F2}).");
        }

        return RefundEligibilityResult.Eligible(maxRefundable);
    }

    private void LogProviderError(Guid refundId, string errorMessage)
    {
        if (_refundSettings.LogProviderErrors)
        {
            _logger.LogError(
                "Provider error during refund processing: RefundId={RefundId}, Error={Error}",
                refundId, errorMessage);
        }
    }

    private static List<string> ValidateFullRefundCommand(ProcessFullRefundCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
        {
            errors.Add("Order ID is required.");
        }

        if (command.PaymentTransactionId == Guid.Empty)
        {
            errors.Add("Payment transaction ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Refund reason is required.");
        }

        if (string.IsNullOrWhiteSpace(command.InitiatedByUserId))
        {
            errors.Add("Initiating user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.InitiatedByRole))
        {
            errors.Add("Initiating user role is required.");
        }

        return errors;
    }

    private static List<string> ValidatePartialRefundCommand(ProcessPartialRefundCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
        {
            errors.Add("Order ID is required.");
        }

        if (command.PaymentTransactionId == Guid.Empty)
        {
            errors.Add("Payment transaction ID is required.");
        }

        if (command.Amount <= 0)
        {
            errors.Add("Refund amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Refund reason is required.");
        }

        if (string.IsNullOrWhiteSpace(command.InitiatedByUserId))
        {
            errors.Add("Initiating user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.InitiatedByRole))
        {
            errors.Add("Initiating user role is required.");
        }

        return errors;
    }

    private static List<string> ValidateCheckRefundEligibilityCommand(CheckRefundEligibilityCommand command)
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

        if (command.Amount <= 0)
        {
            errors.Add("Refund amount must be greater than zero.");
        }

        return errors;
    }
}
