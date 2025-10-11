using GameGuild;
using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.Models;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Subscriptions.Infrastructure;

/// <summary>
/// Repository implementation for subscription data access
/// </summary>
public class SubscriptionRepository : ISubscriptionRepository {
  private readonly ApplicationDbContext _context;

  public SubscriptionRepository(ApplicationDbContext context) {
    _context = context;
  }

  public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
  }

  public async Task<Subscription?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .FirstOrDefaultAsync(s => s.ExternalId == externalId, cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Where(s => s.TenantId == tenantId)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync(cancellationToken);
  }

  public async Task<Subscription?> GetActiveTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Tenant)
        .Where(s => s.PlanId == planId)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.Status == status)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetByCreatedUserIdAsync(Guid userId, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.CreatedByUserId == userId)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetExpiringSoonAsync(int days, CancellationToken cancellationToken = default) {
    var cutoffDate = DateTime.UtcNow.AddDays(days);
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.Status == SubscriptionStatus.Active &&
                    s.NextBillingDate.HasValue &&
                    s.NextBillingDate <= cutoffDate)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetDueForRenewalAsync(int days, CancellationToken cancellationToken = default) {
    var cutoffDate = DateTime.UtcNow.AddDays(days);
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.Status == SubscriptionStatus.Active &&
                    s.AutoRenew &&
                    s.NextBillingDate.HasValue &&
                    s.NextBillingDate <= cutoffDate)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetTrialsExpiringSoonAsync(int days, CancellationToken cancellationToken = default) {
    var cutoffDate = DateTime.UtcNow.AddDays(days);
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.Status == SubscriptionStatus.Trialing &&
                    s.TrialEndDate.HasValue &&
                    s.TrialEndDate <= cutoffDate)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetSuspendedAsync(CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.Status == SubscriptionStatus.Suspended)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetByBillingCycleAsync(BillingCycle billingCycle, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.BillingCycle == billingCycle)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Subscription>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .Where(s => s.StartDate >= startDate && s.StartDate <= endDate)
        .ToListAsync(cancellationToken);
  }

  public async Task<Dictionary<SubscriptionStatus, int>> GetCountByStatusAsync(CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .GroupBy(s => s.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
  }

  public async Task<decimal> GetRevenueForPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .Where(s => s.StartDate >= startDate && s.StartDate <= endDate &&
                    s.Status != SubscriptionStatus.Cancelled)
        .SumAsync(s => s.Amount.Amount, cancellationToken);
  }

  public async Task<Subscription> AddAsync(Subscription subscription, CancellationToken cancellationToken = default) {
    var entry = await _context.Subscriptions.AddAsync(subscription, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);
    return entry.Entity;
  }

  public async Task<Subscription> UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default) {
    _context.Subscriptions.Update(subscription);
    await _context.SaveChangesAsync(cancellationToken);
    return subscription;
  }

  public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) {
    var subscription = await GetByIdAsync(id, cancellationToken);
    if (subscription != null) {
      subscription.DeletedAt = DateTime.UtcNow;
      _context.Subscriptions.Update(subscription);
      await _context.SaveChangesAsync(cancellationToken);
    }
  }

  public async Task<bool> HasActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default) {
    return await _context.Subscriptions
        .AnyAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);
  }

  public async Task<PagedResult<Subscription>> GetPagedAsync(
      int page,
      int pageSize,
      SubscriptionStatus? status = null,
      Guid? tenantId = null,
      Guid? planId = null,
      CancellationToken cancellationToken = default) {
    var query = _context.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .AsQueryable();

    if (status.HasValue)
      query = query.Where(s => s.Status == status.Value);

    if (tenantId.HasValue)
      query = query.Where(s => s.TenantId == tenantId.Value);

    if (planId.HasValue)
      query = query.Where(s => s.PlanId == planId.Value);

    var totalCount = await query.CountAsync(cancellationToken);
    var skip = (page - 1) * pageSize;
    var items = await query
        .OrderByDescending(s => s.CreatedAt)
        .Skip(skip)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return new PagedResult<Subscription>(items, totalCount, skip, pageSize);
  }
}
