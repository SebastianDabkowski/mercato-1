using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for commission rule data access operations.
/// </summary>
public class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionRuleRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public CommissionRuleRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CommissionRule?> GetByIdAsync(Guid id)
    {
        return await _dbContext.CommissionRules.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommissionRule>> GetActiveRulesAsync()
    {
        return await _dbContext.CommissionRules
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CommissionRule?> GetBestMatchingRuleAsync(Guid? sellerId, string? categoryId)
    {
        var activeRules = await _dbContext.CommissionRules
            .Where(r => r.IsActive)
            .ToListAsync();

        CommissionRule? bestMatch = null;
        var bestMatchScore = -1;

        foreach (var rule in activeRules)
        {
            if (!IsRuleApplicable(rule, sellerId, categoryId))
            {
                continue;
            }

            var matchScore = CalculateMatchScore(rule);
            if (bestMatch == null || matchScore > bestMatchScore || 
                (matchScore == bestMatchScore && rule.Priority > bestMatch.Priority))
            {
                bestMatch = rule;
                bestMatchScore = matchScore;
            }
        }

        return bestMatch;
    }

    /// <inheritdoc />
    public async Task<CommissionRule> AddAsync(CommissionRule rule)
    {
        await _dbContext.CommissionRules.AddAsync(rule);
        await _dbContext.SaveChangesAsync();
        return rule;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CommissionRule rule)
    {
        _dbContext.CommissionRules.Update(rule);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Determines if a rule is applicable to the given seller and category.
    /// A rule is applicable if:
    /// - Its SellerId is null (applies to all sellers), OR it matches the given sellerId
    /// - Its CategoryId is null (applies to all categories), OR it matches the given categoryId
    /// </summary>
    private static bool IsRuleApplicable(CommissionRule rule, Guid? sellerId, string? categoryId)
    {
        var sellerMatches = rule.SellerId == null || 
            (sellerId.HasValue && rule.SellerId == sellerId);
        
        var categoryMatches = rule.CategoryId == null || 
            (!string.IsNullOrEmpty(categoryId) && string.Equals(rule.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));

        return sellerMatches && categoryMatches;
    }

    /// <summary>
    /// Calculates the specificity score for a rule.
    /// Higher scores indicate more specific rules.
    /// </summary>
    private static int CalculateMatchScore(CommissionRule rule)
    {
        // Score based on specificity:
        // 3 = Seller + Category (most specific)
        // 2 = Seller only
        // 1 = Category only
        // 0 = Global default (both null)
        if (rule.SellerId != null && rule.CategoryId != null)
            return 3;
        if (rule.SellerId != null)
            return 2;
        if (rule.CategoryId != null)
            return 1;
        return 0;
    }
}
