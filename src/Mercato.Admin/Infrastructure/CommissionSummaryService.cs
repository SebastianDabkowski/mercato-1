using System.Text;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for generating commission summary reports with CSV export.
/// </summary>
public class CommissionSummaryService : ICommissionSummaryService
{
    /// <summary>
    /// UTF-8 BOM for Excel compatibility.
    /// </summary>
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Maximum allowed page size for pagination.
    /// </summary>
    private const int MaxPageSize = 100;

    private readonly ICommissionSummaryRepository _repository;
    private readonly ILogger<CommissionSummaryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionSummaryService"/> class.
    /// </summary>
    /// <param name="repository">The commission summary repository.</param>
    /// <param name="logger">The logger.</param>
    public CommissionSummaryService(
        ICommissionSummaryRepository repository,
        ILogger<CommissionSummaryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GetCommissionSummaryResult> GetSummaryAsync(CommissionSummaryFilterQuery query)
    {
        try
        {
            var validationErrors = ValidateQuery(query);
            if (validationErrors.Count > 0)
            {
                return GetCommissionSummaryResult.Failure(validationErrors);
            }

            var (rows, totalGMV, totalCommission, totalNetPayout, totalOrderCount) =
                await _repository.GetSummaryDataAsync(query.FromDate, query.ToDate);

            return GetCommissionSummaryResult.Success(
                rows,
                totalGMV,
                totalCommission,
                totalNetPayout,
                totalOrderCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving commission summary report");
            return GetCommissionSummaryResult.Failure("An error occurred while retrieving the commission summary.");
        }
    }

    /// <inheritdoc />
    public async Task<GetSellerOrdersResult> GetSellerOrdersAsync(
        Guid sellerId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize)
    {
        try
        {
            var validationErrors = ValidateSellerOrdersQuery(sellerId, fromDate, toDate, page, pageSize);
            if (validationErrors.Count > 0)
            {
                return GetSellerOrdersResult.Failure(validationErrors);
            }

            var (rows, totalCount, sellerIdResult, sellerName) =
                await _repository.GetSellerOrdersAsync(sellerId, fromDate, toDate, page, pageSize);

            return GetSellerOrdersResult.Success(
                rows,
                totalCount,
                page,
                pageSize,
                sellerIdResult,
                sellerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seller orders for seller {SellerId}", sellerId);
            return GetSellerOrdersResult.Failure("An error occurred while retrieving the seller orders.");
        }
    }

    /// <inheritdoc />
    public async Task<ExportCommissionSummaryResult> ExportToCsvAsync(CommissionSummaryFilterQuery query)
    {
        try
        {
            var validationErrors = ValidateQuery(query);
            if (validationErrors.Count > 0)
            {
                return ExportCommissionSummaryResult.Failure(validationErrors);
            }

            var (rows, _, _, _, _) = await _repository.GetSummaryDataAsync(query.FromDate, query.ToDate);

            var csvContent = GenerateCsv(rows);
            var fileName = GenerateFileName(query);

            return ExportCommissionSummaryResult.Success(csvContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting commission summary to CSV");
            return ExportCommissionSummaryResult.Failure("An error occurred while exporting the commission summary.");
        }
    }

    private static List<string> ValidateQuery(CommissionSummaryFilterQuery query)
    {
        var errors = new List<string>();

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            errors.Add("From date cannot be after to date.");
        }

        return errors;
    }

    private static List<string> ValidateSellerOrdersQuery(
        Guid sellerId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize)
    {
        var errors = new List<string>();

        if (sellerId == Guid.Empty)
        {
            errors.Add("Seller ID is required.");
        }

        if (page < 1)
        {
            errors.Add("Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            errors.Add($"Page size must be between 1 and {MaxPageSize}.");
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            errors.Add("From date cannot be after to date.");
        }

        return errors;
    }

    private static byte[] GenerateCsv(IReadOnlyList<SellerCommissionSummaryRow> rows)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Seller ID,Seller Name,Total GMV,Total Commission,Total Net Payout,Order Count");

        // Data rows
        foreach (var row in rows)
        {
            sb.Append(EscapeCsvField(row.SellerId.ToString()));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.SellerName));
            sb.Append(',');
            sb.Append(row.TotalGMV.ToString("F2"));
            sb.Append(',');
            sb.Append(row.TotalCommission.ToString("F2"));
            sb.Append(',');
            sb.Append(row.TotalNetPayout.ToString("F2"));
            sb.Append(',');
            sb.Append(row.OrderCount);
            sb.AppendLine();
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());

        // Prepend UTF-8 BOM for Excel compatibility
        var result = new byte[Utf8Bom.Length + csvBytes.Length];
        Utf8Bom.CopyTo(result, 0);
        csvBytes.CopyTo(result, Utf8Bom.Length);

        return result;
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // Check if field contains special characters that require escaping
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape double quotes by doubling them
            var escapedField = field.Replace("\"", "\"\"");
            return $"\"{escapedField}\"";
        }

        return field;
    }

    private static string GenerateFileName(CommissionSummaryFilterQuery query)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var parts = new List<string> { "CommissionSummary" };

        if (query.FromDate.HasValue)
        {
            parts.Add($"from_{query.FromDate.Value:yyyyMMdd}");
        }

        if (query.ToDate.HasValue)
        {
            parts.Add($"to_{query.ToDate.Value:yyyyMMdd}");
        }

        parts.Add(timestamp);

        return string.Join("_", parts) + ".csv";
    }
}
