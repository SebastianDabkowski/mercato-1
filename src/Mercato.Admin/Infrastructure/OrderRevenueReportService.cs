using System.Text;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for generating order and revenue reports with CSV export.
/// </summary>
public class OrderRevenueReportService : IOrderRevenueReportService
{
    /// <summary>
    /// Maximum number of rows that can be exported to CSV.
    /// </summary>
    private const int MaxExportRows = 10000;

    /// <summary>
    /// UTF-8 BOM for Excel compatibility.
    /// </summary>
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private readonly IOrderRevenueReportRepository _repository;
    private readonly ILogger<OrderRevenueReportService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderRevenueReportService"/> class.
    /// </summary>
    /// <param name="repository">The order revenue report repository.</param>
    /// <param name="logger">The logger.</param>
    public OrderRevenueReportService(
        IOrderRevenueReportRepository repository,
        ILogger<OrderRevenueReportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GetOrderRevenueReportResult> GetReportAsync(OrderRevenueReportFilterQuery query)
    {
        try
        {
            var validationErrors = ValidateQuery(query);
            if (validationErrors.Count > 0)
            {
                return GetOrderRevenueReportResult.Failure(validationErrors);
            }

            var (rows, totalCount, totalOrderValue, totalCommission, totalPayoutAmount) =
                await _repository.GetReportDataAsync(
                    query.FromDate,
                    query.ToDate,
                    query.SellerId,
                    query.OrderStatuses.Count > 0 ? query.OrderStatuses : null,
                    query.PaymentStatuses.Count > 0 ? query.PaymentStatuses : null,
                    query.Page,
                    query.PageSize);

            return GetOrderRevenueReportResult.Success(
                rows,
                totalCount,
                query.Page,
                query.PageSize,
                totalOrderValue,
                totalCommission,
                totalPayoutAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order revenue report");
            return GetOrderRevenueReportResult.Failure("An error occurred while retrieving the report.");
        }
    }

    /// <inheritdoc />
    public async Task<ExportOrderRevenueReportResult> ExportToCsvAsync(OrderRevenueReportFilterQuery query)
    {
        try
        {
            var validationErrors = ValidateQuery(query);
            if (validationErrors.Count > 0)
            {
                return ExportOrderRevenueReportResult.Failure(validationErrors);
            }

            // First, get the total count to check if it exceeds the limit
            var (rows, totalCount, _, _, _) = await _repository.GetReportDataAsync(
                query.FromDate,
                query.ToDate,
                query.SellerId,
                query.OrderStatuses.Count > 0 ? query.OrderStatuses : null,
                query.PaymentStatuses.Count > 0 ? query.PaymentStatuses : null,
                1,
                1);

            if (totalCount > MaxExportRows)
            {
                return ExportOrderRevenueReportResult.Failure(
                    $"Export exceeds maximum allowed rows ({MaxExportRows}). Please narrow your filter criteria or consider requesting a background report generation.");
            }

            // Get all rows for export (no pagination)
            (rows, totalCount, _, _, _) = await _repository.GetReportDataAsync(
                query.FromDate,
                query.ToDate,
                query.SellerId,
                query.OrderStatuses.Count > 0 ? query.OrderStatuses : null,
                query.PaymentStatuses.Count > 0 ? query.PaymentStatuses : null,
                1,
                MaxExportRows);

            var csvContent = GenerateCsv(rows);
            var fileName = GenerateFileName(query);

            return ExportOrderRevenueReportResult.Success(csvContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting order revenue report to CSV");
            return ExportOrderRevenueReportResult.Failure("An error occurred while exporting the report.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid SellerId, string SellerName)>> GetDistinctSellersAsync()
    {
        try
        {
            return await _repository.GetDistinctSellersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving distinct sellers for report filter");
            return [];
        }
    }

    private static List<string> ValidateQuery(OrderRevenueReportFilterQuery query)
    {
        var errors = new List<string>();

        if (query.Page < 1)
        {
            errors.Add("Page must be greater than or equal to 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            errors.Add("From date cannot be after to date.");
        }

        return errors;
    }

    private static byte[] GenerateCsv(IReadOnlyList<OrderRevenueReportRow> rows)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Order ID,Order Number,Order Date,Buyer Email,Seller Name,Seller ID,Order Status,Payment Status,Order Value,Commission,Payout Amount");

        // Data rows
        foreach (var row in rows)
        {
            sb.Append(EscapeCsvField(row.OrderId.ToString()));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.OrderNumber));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.OrderDate.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.BuyerEmail));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.SellerName));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.SellerId.ToString()));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.OrderStatus.ToString()));
            sb.Append(',');
            sb.Append(EscapeCsvField(row.PaymentStatus.ToString()));
            sb.Append(',');
            sb.Append(row.OrderValue.ToString("F2"));
            sb.Append(',');
            sb.Append(row.Commission.ToString("F2"));
            sb.Append(',');
            sb.Append(row.PayoutAmount.ToString("F2"));
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

    private static string GenerateFileName(OrderRevenueReportFilterQuery query)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var parts = new List<string> { "OrderRevenueReport" };

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
