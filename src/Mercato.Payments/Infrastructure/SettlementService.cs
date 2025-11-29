using System.Globalization;
using System.Text;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Payments.Infrastructure;

/// <summary>
/// Service implementation for settlement operations.
/// </summary>
public class SettlementService : ISettlementService
{
    private readonly ISettlementRepository _settlementRepository;
    private readonly ICommissionRecordRepository _commissionRecordRepository;
    private readonly IEscrowRepository _escrowRepository;
    private readonly ILogger<SettlementService> _logger;
    private readonly SettlementSettings _settlementSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettlementService"/> class.
    /// </summary>
    /// <param name="settlementRepository">The settlement repository.</param>
    /// <param name="commissionRecordRepository">The commission record repository.</param>
    /// <param name="escrowRepository">The escrow repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="settlementSettings">The settlement settings.</param>
    public SettlementService(
        ISettlementRepository settlementRepository,
        ICommissionRecordRepository commissionRecordRepository,
        IEscrowRepository escrowRepository,
        ILogger<SettlementService> logger,
        IOptions<SettlementSettings> settlementSettings)
    {
        _settlementRepository = settlementRepository;
        _commissionRecordRepository = commissionRecordRepository;
        _escrowRepository = escrowRepository;
        _logger = logger;
        _settlementSettings = settlementSettings.Value;
    }

    /// <inheritdoc />
    public async Task<GenerateSettlementResult> GenerateSettlementAsync(GenerateSettlementCommand command)
    {
        var errors = ValidateGenerateSettlementCommand(command);
        if (errors.Count > 0)
        {
            return GenerateSettlementResult.Failure(errors);
        }

        // Check if a settlement already exists for this period
        var existingSettlement = await _settlementRepository.GetBySellerAndPeriodAsync(
            command.SellerId, command.Year, command.Month);

        if (existingSettlement != null)
        {
            return GenerateSettlementResult.Failure(
                $"A settlement already exists for seller {command.SellerId} for {command.Year}/{command.Month}. Use regenerate instead.");
        }

        var settlement = await CreateSettlementAsync(command.SellerId, command.Year, command.Month, command.AuditNote);

        _logger.LogInformation(
            "Generated settlement {SettlementId} for seller {SellerId} for period {Year}/{Month}. " +
            "Gross: {GrossSales}, Net Payable: {NetPayable}, Orders: {OrderCount}",
            settlement.Id,
            settlement.SellerId,
            settlement.Year,
            settlement.Month,
            settlement.GrossSales,
            settlement.NetPayable,
            settlement.OrderCount);

        return GenerateSettlementResult.Success(settlement);
    }

    /// <inheritdoc />
    public async Task<RegenerateSettlementResult> RegenerateSettlementAsync(RegenerateSettlementCommand command)
    {
        if (command.SettlementId == Guid.Empty)
        {
            return RegenerateSettlementResult.Failure("Settlement ID is required.");
        }

        var existingSettlement = await _settlementRepository.GetByIdWithLineItemsAsync(command.SettlementId);
        if (existingSettlement == null)
        {
            return RegenerateSettlementResult.Failure("Settlement not found.");
        }

        if (existingSettlement.Status != SettlementStatus.Draft)
        {
            return RegenerateSettlementResult.Failure(
                $"Cannot regenerate a settlement with status '{existingSettlement.Status}'. Only draft settlements can be regenerated.");
        }

        var previousVersion = existingSettlement.Version;

        // Delete existing line items
        await _settlementRepository.DeleteLineItemsAsync(command.SettlementId);

        // Regenerate the settlement data
        var regeneratedSettlement = await RecalculateSettlementAsync(
            existingSettlement,
            command.Reason);

        _logger.LogInformation(
            "Regenerated settlement {SettlementId} for seller {SellerId} for period {Year}/{Month}. " +
            "Version: {PreviousVersion} -> {NewVersion}. Reason: {Reason}",
            regeneratedSettlement.Id,
            regeneratedSettlement.SellerId,
            regeneratedSettlement.Year,
            regeneratedSettlement.Month,
            previousVersion,
            regeneratedSettlement.Version,
            command.Reason ?? "Not specified");

        return RegenerateSettlementResult.Success(regeneratedSettlement, previousVersion);
    }

    /// <inheritdoc />
    public async Task<FinalizeSettlementResult> FinalizeSettlementAsync(Guid settlementId)
    {
        if (settlementId == Guid.Empty)
        {
            return FinalizeSettlementResult.Failure("Settlement ID is required.");
        }

        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        if (settlement == null)
        {
            return FinalizeSettlementResult.Failure("Settlement not found.");
        }

        if (settlement.Status == SettlementStatus.Finalized)
        {
            return FinalizeSettlementResult.Failure("Settlement is already finalized.");
        }

        if (settlement.Status == SettlementStatus.Exported)
        {
            return FinalizeSettlementResult.Failure("Settlement has already been exported.");
        }

        settlement.Status = SettlementStatus.Finalized;
        settlement.FinalizedAt = DateTimeOffset.UtcNow;

        await _settlementRepository.UpdateAsync(settlement);

        _logger.LogInformation(
            "Finalized settlement {SettlementId} for seller {SellerId} for period {Year}/{Month}",
            settlement.Id,
            settlement.SellerId,
            settlement.Year,
            settlement.Month);

        return FinalizeSettlementResult.Success(settlement);
    }

    /// <inheritdoc />
    public async Task<ExportSettlementResult> ExportSettlementAsync(Guid settlementId)
    {
        if (settlementId == Guid.Empty)
        {
            return ExportSettlementResult.Failure("Settlement ID is required.");
        }

        var settlement = await _settlementRepository.GetByIdWithLineItemsAsync(settlementId);
        if (settlement == null)
        {
            return ExportSettlementResult.Failure("Settlement not found.");
        }

        // Generate CSV
        var csvData = GenerateCsv(settlement);
        var fileName = $"settlement_{settlement.SellerId}_{settlement.Year}_{settlement.Month:D2}_v{settlement.Version}.csv";

        // Update status to Exported if not already
        if (settlement.Status != SettlementStatus.Exported)
        {
            settlement.Status = SettlementStatus.Exported;
            settlement.ExportedAt = DateTimeOffset.UtcNow;
            await _settlementRepository.UpdateAsync(settlement);
        }

        _logger.LogInformation(
            "Exported settlement {SettlementId} for seller {SellerId} for period {Year}/{Month}",
            settlement.Id,
            settlement.SellerId,
            settlement.Year,
            settlement.Month);

        return ExportSettlementResult.Success(settlement, csvData, fileName);
    }

    /// <inheritdoc />
    public async Task<GetSettlementResult> GetSettlementAsync(Guid settlementId)
    {
        if (settlementId == Guid.Empty)
        {
            return GetSettlementResult.Failure("Settlement ID is required.");
        }

        var settlement = await _settlementRepository.GetByIdWithLineItemsAsync(settlementId);
        if (settlement == null)
        {
            return GetSettlementResult.Failure("Settlement not found.");
        }

        return GetSettlementResult.Success(settlement);
    }

    /// <inheritdoc />
    public async Task<GetSettlementsResult> GetSettlementsAsync(GetSettlementsQuery query)
    {
        var errors = ValidateGetSettlementsQuery(query);
        if (errors.Count > 0)
        {
            return GetSettlementsResult.Failure(errors);
        }

        var settlements = await _settlementRepository.GetFilteredAsync(
            query.SellerId,
            query.Year,
            query.Month,
            query.Status);

        return GetSettlementsResult.Success(settlements);
    }

    private async Task<Settlement> CreateSettlementAsync(Guid sellerId, int year, int month, string? auditNote)
    {
        var now = DateTimeOffset.UtcNow;

        // Get period boundaries
        var periodStart = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // Get commission records for this seller in the period
        // TODO: Clarify performance requirements. Consider adding date range filtering to repository
        // to avoid loading all historical records for sellers with large transaction volumes.
        var allCommissionRecords = await _commissionRecordRepository.GetBySellerIdAsync(sellerId);
        var periodCommissionRecords = allCommissionRecords
            .Where(cr => cr.CalculatedAt >= periodStart && cr.CalculatedAt <= periodEnd)
            .ToList();

        // Get escrow entries released in this period for additional data
        // Note: Used for potential future enhancements to cross-reference settlement data
        var allEscrowEntries = await _escrowRepository.GetBySellerIdAsync(sellerId);
        var releasedEscrowEntries = allEscrowEntries
            .Where(e => e.ReleasedAt.HasValue && e.ReleasedAt >= periodStart && e.ReleasedAt <= periodEnd)
            .ToList();

        // Calculate totals
        var grossSales = periodCommissionRecords.Sum(cr => cr.OrderAmount);
        var totalRefunds = periodCommissionRecords.Sum(cr => cr.RefundedAmount);
        var netSales = grossSales - totalRefunds;
        var totalCommission = periodCommissionRecords.Sum(cr => cr.NetCommissionAmount);

        // Check for adjustments from previous months (refunds on orders from prior periods)
        var previousMonthAdjustments = CalculatePreviousMonthAdjustments(
            allCommissionRecords, periodStart, periodEnd);

        var netPayable = netSales - totalCommission + previousMonthAdjustments;

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Year = year,
            Month = month,
            Currency = _settlementSettings.DefaultCurrency,
            GrossSales = grossSales,
            TotalRefunds = totalRefunds,
            NetSales = netSales,
            TotalCommission = totalCommission,
            PreviousMonthAdjustments = previousMonthAdjustments,
            NetPayable = netPayable,
            OrderCount = periodCommissionRecords.Select(cr => cr.OrderId).Distinct().Count(),
            Status = SettlementStatus.Draft,
            GeneratedAt = now,
            Version = 1,
            AuditNotes = auditNote ?? $"Generated on {now:yyyy-MM-dd HH:mm:ss} UTC"
        };

        await _settlementRepository.AddAsync(settlement);

        // Create line items for each order
        await CreateLineItemsAsync(settlement, periodCommissionRecords, allCommissionRecords, periodStart);

        return settlement;
    }

    private async Task<Settlement> RecalculateSettlementAsync(Settlement existingSettlement, string? reason)
    {
        var now = DateTimeOffset.UtcNow;

        // Get period boundaries
        var periodStart = new DateTimeOffset(existingSettlement.Year, existingSettlement.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // Get commission records for this seller in the period
        // TODO: Clarify performance requirements. Consider adding date range filtering to repository.
        var allCommissionRecords = await _commissionRecordRepository.GetBySellerIdAsync(existingSettlement.SellerId);
        var periodCommissionRecords = allCommissionRecords
            .Where(cr => cr.CalculatedAt >= periodStart && cr.CalculatedAt <= periodEnd)
            .ToList();

        // Calculate totals
        var grossSales = periodCommissionRecords.Sum(cr => cr.OrderAmount);
        var totalRefunds = periodCommissionRecords.Sum(cr => cr.RefundedAmount);
        var netSales = grossSales - totalRefunds;
        var totalCommission = periodCommissionRecords.Sum(cr => cr.NetCommissionAmount);

        // Check for adjustments from previous months
        var previousMonthAdjustments = CalculatePreviousMonthAdjustments(
            allCommissionRecords, periodStart, periodEnd);

        var netPayable = netSales - totalCommission + previousMonthAdjustments;

        // Update settlement with new values
        existingSettlement.GrossSales = grossSales;
        existingSettlement.TotalRefunds = totalRefunds;
        existingSettlement.NetSales = netSales;
        existingSettlement.TotalCommission = totalCommission;
        existingSettlement.PreviousMonthAdjustments = previousMonthAdjustments;
        existingSettlement.NetPayable = netPayable;
        existingSettlement.OrderCount = periodCommissionRecords.Select(cr => cr.OrderId).Distinct().Count();
        existingSettlement.RegeneratedAt = now;
        existingSettlement.Version++;
        existingSettlement.AuditNotes = AppendAuditNote(existingSettlement.AuditNotes, reason, now);

        await _settlementRepository.UpdateAsync(existingSettlement);

        // Create new line items
        await CreateLineItemsAsync(existingSettlement, periodCommissionRecords, allCommissionRecords, periodStart);

        return existingSettlement;
    }

    private async Task CreateLineItemsAsync(
        Settlement settlement,
        List<CommissionRecord> periodCommissionRecords,
        IReadOnlyList<CommissionRecord> allCommissionRecords,
        DateTimeOffset periodStart)
    {
        // Group by order to create line items
        var orderGroups = periodCommissionRecords.GroupBy(cr => cr.OrderId);

        foreach (var orderGroup in orderGroups)
        {
            var orderId = orderGroup.Key;
            var orderRecords = orderGroup.ToList();
            var firstRecord = orderRecords.First();

            var lineItem = new SettlementLineItem
            {
                Id = Guid.NewGuid(),
                SettlementId = settlement.Id,
                OrderId = orderId,
                OrderNumber = $"ORD-{orderId.ToString()[..8].ToUpperInvariant()}",
                OrderDate = firstRecord.CalculatedAt,
                GrossAmount = orderRecords.Sum(cr => cr.OrderAmount),
                RefundAmount = orderRecords.Sum(cr => cr.RefundedAmount),
                NetAmount = orderRecords.Sum(cr => cr.OrderAmount - cr.RefundedAmount),
                CommissionAmount = orderRecords.Sum(cr => cr.NetCommissionAmount),
                IsAdjustment = false
            };

            await _settlementRepository.AddLineItemAsync(lineItem);
        }

        // Add adjustment line items for refunds from previous months
        var adjustmentRecords = allCommissionRecords
            .Where(cr => cr.CalculatedAt < periodStart &&
                        cr.LastRefundRecalculatedAt.HasValue &&
                        cr.LastRefundRecalculatedAt >= periodStart)
            .ToList();

        foreach (var adjustmentRecord in adjustmentRecords)
        {
            var originalPeriod = new DateTime(adjustmentRecord.CalculatedAt.Year, adjustmentRecord.CalculatedAt.Month, 1);

            var adjustmentLineItem = new SettlementLineItem
            {
                Id = Guid.NewGuid(),
                SettlementId = settlement.Id,
                OrderId = adjustmentRecord.OrderId,
                OrderNumber = $"ADJ-{adjustmentRecord.OrderId.ToString()[..8].ToUpperInvariant()}",
                OrderDate = adjustmentRecord.LastRefundRecalculatedAt ?? DateTimeOffset.UtcNow,
                GrossAmount = 0,
                RefundAmount = adjustmentRecord.RefundedAmount,
                NetAmount = -adjustmentRecord.RefundedAmount,
                CommissionAmount = -adjustmentRecord.RefundedCommissionAmount,
                IsAdjustment = true,
                OriginalMonth = originalPeriod.Month,
                OriginalYear = originalPeriod.Year,
                AdjustmentNotes = $"Refund adjustment from {originalPeriod:MMM yyyy}"
            };

            await _settlementRepository.AddLineItemAsync(adjustmentLineItem);
        }
    }

    private static decimal CalculatePreviousMonthAdjustments(
        IReadOnlyList<CommissionRecord> allCommissionRecords,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        // Find refunds that were processed in this period for orders from previous months
        var adjustments = allCommissionRecords
            .Where(cr => cr.CalculatedAt < periodStart &&
                        cr.LastRefundRecalculatedAt.HasValue &&
                        cr.LastRefundRecalculatedAt >= periodStart &&
                        cr.LastRefundRecalculatedAt <= periodEnd)
            .Sum(cr => cr.RefundedCommissionAmount - cr.RefundedAmount);

        return adjustments;
    }

    private static string GenerateCsv(Settlement settlement)
    {
        var sb = new StringBuilder();

        // Header section with settlement summary
        sb.AppendLine("Settlement Report");
        sb.AppendLine($"Seller ID,{settlement.SellerId}");
        sb.AppendLine($"Period,{settlement.Year}-{settlement.Month:D2}");
        sb.AppendLine($"Currency,{settlement.Currency}");
        sb.AppendLine($"Status,{settlement.Status}");
        sb.AppendLine($"Generated At,{settlement.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Version,{settlement.Version}");
        sb.AppendLine();

        // Summary section
        sb.AppendLine("Summary");
        sb.AppendLine($"Gross Sales,{settlement.GrossSales.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Total Refunds,{settlement.TotalRefunds.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net Sales,{settlement.NetSales.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Total Commission,{settlement.TotalCommission.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Previous Month Adjustments,{settlement.PreviousMonthAdjustments.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net Payable,{settlement.NetPayable.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Order Count,{settlement.OrderCount}");
        sb.AppendLine();

        // Line items section
        sb.AppendLine("Line Items");
        sb.AppendLine("Order Number,Order Date,Gross Amount,Refund Amount,Net Amount,Commission Amount,Is Adjustment,Original Period,Notes");

        foreach (var lineItem in settlement.LineItems.OrderBy(li => li.OrderDate))
        {
            var originalPeriod = lineItem.IsAdjustment && lineItem.OriginalYear.HasValue && lineItem.OriginalMonth.HasValue
                ? $"{lineItem.OriginalYear}-{lineItem.OriginalMonth:D2}"
                : string.Empty;

            sb.AppendLine(string.Join(",",
                EscapeCsvField(lineItem.OrderNumber),
                lineItem.OrderDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                lineItem.GrossAmount.ToString("F2", CultureInfo.InvariantCulture),
                lineItem.RefundAmount.ToString("F2", CultureInfo.InvariantCulture),
                lineItem.NetAmount.ToString("F2", CultureInfo.InvariantCulture),
                lineItem.CommissionAmount.ToString("F2", CultureInfo.InvariantCulture),
                lineItem.IsAdjustment ? "Yes" : "No",
                originalPeriod,
                EscapeCsvField(lineItem.AdjustmentNotes ?? string.Empty)));
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static string AppendAuditNote(string? existingNotes, string? newReason, DateTimeOffset timestamp)
    {
        var newNote = $"Regenerated on {timestamp:yyyy-MM-dd HH:mm:ss} UTC" +
            (string.IsNullOrWhiteSpace(newReason) ? "" : $": {newReason}");

        if (string.IsNullOrWhiteSpace(existingNotes))
        {
            return newNote;
        }

        return $"{existingNotes}\n{newNote}";
    }

    private static List<string> ValidateGenerateSettlementCommand(GenerateSettlementCommand command)
    {
        var errors = new List<string>();

        if (command.SellerId == Guid.Empty)
        {
            errors.Add("Seller ID is required.");
        }

        if (command.Year < 2000 || command.Year > 2100)
        {
            errors.Add("Year must be between 2000 and 2100.");
        }

        if (command.Month < 1 || command.Month > 12)
        {
            errors.Add("Month must be between 1 and 12.");
        }

        return errors;
    }

    private static List<string> ValidateGetSettlementsQuery(GetSettlementsQuery query)
    {
        var errors = new List<string>();

        if (query.Year.HasValue && (query.Year < 2000 || query.Year > 2100))
        {
            errors.Add("Year must be between 2000 and 2100.");
        }

        if (query.Month.HasValue && (query.Month < 1 || query.Month > 12))
        {
            errors.Add("Month must be between 1 and 12.");
        }

        return errors;
    }
}
