using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for auditing admin access to sensitive data.
/// </summary>
public class SensitiveAccessAuditService : ISensitiveAccessAuditService
{
    private readonly IAdminAuditRepository _auditRepository;
    private readonly ILogger<SensitiveAccessAuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveAccessAuditService"/> class.
    /// </summary>
    /// <param name="auditRepository">The admin audit repository.</param>
    /// <param name="logger">The logger.</param>
    public SensitiveAccessAuditService(
        IAdminAuditRepository auditRepository,
        ILogger<SensitiveAccessAuditService> logger)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task LogCustomerProfileAccessAsync(
        string adminUserId,
        string customerId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await LogSensitiveAccessAsync(
            adminUserId: adminUserId,
            action: "ViewCustomerProfile",
            entityType: "Customer",
            entityId: customerId,
            details: $"Admin accessed customer profile for customer {customerId}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LogPayoutDetailsAccessAsync(
        string adminUserId,
        string sellerId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await LogSensitiveAccessAsync(
            adminUserId: adminUserId,
            action: "ViewPayoutDetails",
            entityType: "Seller",
            entityId: sellerId,
            details: $"Admin accessed payout details for seller {sellerId}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LogKycDocumentAccessAsync(
        string adminUserId,
        Guid submissionId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await LogSensitiveAccessAsync(
            adminUserId: adminUserId,
            action: "ViewKycDocument",
            entityType: "KycSubmission",
            entityId: submissionId.ToString(),
            details: $"Admin accessed KYC document for submission {submissionId}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LogStoreDetailsAccessAsync(
        string adminUserId,
        Guid storeId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await LogSensitiveAccessAsync(
            adminUserId: adminUserId,
            action: "ViewStoreDetails",
            entityType: "Store",
            entityId: storeId.ToString(),
            details: $"Admin accessed store details for store {storeId}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);
    }

    private async Task LogSensitiveAccessAsync(
        string adminUserId,
        string action,
        string entityType,
        string entityId,
        string details,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var auditLog = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow,
            IpAddress = ipAddress
        };

        try
        {
            await _auditRepository.AddAsync(auditLog);

            _logger.LogInformation(
                "Sensitive access logged: Admin {AdminUserId} performed {Action} on {EntityType} {EntityId}",
                adminUserId,
                action,
                entityType,
                entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to log sensitive access: Admin {AdminUserId} performed {Action} on {EntityType} {EntityId}",
                adminUserId,
                action,
                entityType,
                entityId);
            throw;
        }
    }
}
