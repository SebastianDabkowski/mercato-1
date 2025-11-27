using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for KYC submission data access.
/// </summary>
public class KycRepository : IKycRepository
{
    private readonly SellerDbContext _context;

    public KycRepository(SellerDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<KycSubmission?> GetByIdAsync(Guid id)
    {
        return await _context.KycSubmissions.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KycSubmission>> GetBySellerIdAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _context.KycSubmissions
            .Where(s => s.SellerId == sellerId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<KycSubmission> AddAsync(KycSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        _context.KycSubmissions.Add(submission);
        await _context.SaveChangesAsync();
        return submission;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(KycSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        _context.KycSubmissions.Update(submission);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task AddAuditLogAsync(KycAuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        _context.KycAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KycAuditLog>> GetAuditLogsAsync(Guid kycSubmissionId)
    {
        return await _context.KycAuditLogs
            .Where(l => l.KycSubmissionId == kycSubmissionId)
            .OrderByDescending(l => l.PerformedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KycSubmission>> GetAllAsync()
    {
        return await _context.KycSubmissions
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KycSubmission>> GetByStatusAsync(KycStatus status)
    {
        return await _context.KycSubmissions
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }
}
