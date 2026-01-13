using GameGuild.Abstractions;
using GameGuild.Models;
using GameGuild.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Repository implementation for Subscription entity using the shared application context
/// </summary>
public class SubscriptionRepository(IApplicationDbContext context) : ISubscriptionRepository
{
    private DbSet<Subscription> Subscriptions => context.Set<Subscription>();

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.ExternalId == externalId && s.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).Where(s => s.TenantId == tenantId && s.DeletedAt == null).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription?> GetActiveTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active && s.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).Where(s => s.PlanId == planId && s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).Where(s => s.Status == status && s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetByCreatedUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).Where(s => s.CreatedByUserId == userId && s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetExpiringSoonAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(days);

        return await Subscriptions.Include(s => s.Plan)
            .Where(s => s.EndDate.HasValue && s.EndDate <= cutoffDate && s.Status == SubscriptionStatus.Active && s.DeletedAt == null)
            .OrderBy(s => s.EndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetDueForRenewalAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(days);

        return await Subscriptions.Include(s => s.Plan)
            .Where(s => s.NextBillingDate <= cutoffDate && s.Status == SubscriptionStatus.Active && s.DeletedAt == null)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetTrialsExpiringSoonAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(days);

        return await Subscriptions.Include(s => s.Plan)
            .Where(s => s.TrialEndDate.HasValue && s.TrialEndDate <= cutoffDate && s.Status == SubscriptionStatus.Trialing && s.DeletedAt == null)
            .OrderBy(s => s.TrialEndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetSuspendedAsync(CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Suspended && s.DeletedAt == null)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetByBillingCycleAsync(BillingCycle billingCycle, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan).Where(s => s.BillingCycle == billingCycle && s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Include(s => s.Plan)
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate && s.DeletedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Dictionary<SubscriptionStatus, int>> GetCountByStatusAsync(CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Where(s => s.DeletedAt == null).GroupBy(s => s.Status).ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<decimal> GetRevenueForPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate && s.Status == SubscriptionStatus.Active && s.DeletedAt == null)
            .SumAsync(s => s.Amount.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Subscription> AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        var entry = await Subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entry.Entity;
    }

    public async Task<Subscription> UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        Subscriptions.Update(subscription);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return subscription;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);

        if (subscription != null)
        {
            subscription.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Subscriptions.AnyAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active && s.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<Subscription>> GetPagedAsync(int page, int pageSize, SubscriptionStatus? status = null, Guid? tenantId = null, Guid? planId = null, CancellationToken cancellationToken = default)
    {
        var query = Subscriptions.Include(s => s.Plan).Where(s => s.DeletedAt == null);

        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        if (tenantId.HasValue) query = query.Where(s => s.TenantId == tenantId.Value);

        if (planId.HasValue) query = query.Where(s => s.PlanId == planId.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var skip = (page - 1) * pageSize;
        var items = await query.OrderByDescending(s => s.CreatedAt).Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResult<Subscription>(items, totalCount, skip, pageSize);
    }
}
