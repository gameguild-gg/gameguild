using System.Linq.Expressions;
using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Models;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Subscriptions.Infrastructure;

/// <summary> Repository implementation for subscription data access </summary>
public class SubscriptionRepository : ISubscriptionRepository {
  private readonly ApplicationDbContext _context;

  public SubscriptionRepository(ApplicationDbContext context) { _context = context; }

  public async Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
  }

  public async Task<IEnumerable<UserSubscription>> GetAllAsync(CancellationToken cancellationToken = default) { return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).ToListAsync(cancellationToken); }

  public async Task<(IEnumerable<UserSubscription> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) {
    var query = _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan);

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    return (items, totalCount);
  }

  public async Task<IEnumerable<UserSubscription>> FindAsync(Expression<Func<UserSubscription, bool>> predicate, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(predicate).ToListAsync(cancellationToken);
  }

  public async Task<UserSubscription?> FirstOrDefaultAsync(Expression<Func<UserSubscription, bool>> predicate, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).FirstOrDefaultAsync(predicate, cancellationToken);
  }

  public async Task<bool> AnyAsync(Expression<Func<UserSubscription, bool>> predicate, CancellationToken cancellationToken = default) { return await _context.UserSubscriptions.AnyAsync(predicate, cancellationToken); }

  public async Task<int> CountAsync(Expression<Func<UserSubscription, bool>>? predicate = null, CancellationToken cancellationToken = default) {
    var query = _context.UserSubscriptions.AsQueryable();
    if (predicate != null) query = query.Where(predicate);

    return await query.CountAsync(cancellationToken);
  }

  public async Task<UserSubscription> AddAsync(UserSubscription entity, CancellationToken cancellationToken = default) {
    var result = await _context.UserSubscriptions.AddAsync(entity, cancellationToken);

    return result.Entity;
  }

  public async Task AddRangeAsync(IEnumerable<UserSubscription> entities, CancellationToken cancellationToken = default) { await _context.UserSubscriptions.AddRangeAsync(entities, cancellationToken); }

  public async Task<UserSubscription> UpdateAsync(UserSubscription entity, CancellationToken cancellationToken = default) {
    _context.UserSubscriptions.Update(entity);

    return await Task.FromResult(entity);
  }

  public async Task UpdateRangeAsync(IEnumerable<UserSubscription> entities, CancellationToken cancellationToken = default) {
    _context.UserSubscriptions.UpdateRange(entities);
    await Task.CompletedTask;
  }

  public async Task RemoveAsync(UserSubscription entity, CancellationToken cancellationToken = default) {
    _context.UserSubscriptions.Remove(entity);
    await Task.CompletedTask;
  }

  public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default) {
    var entity = await GetByIdAsync(id, cancellationToken);
    if (entity != null) _context.UserSubscriptions.Remove(entity);
  }

  public async Task RemoveRangeAsync(IEnumerable<UserSubscription> entities, CancellationToken cancellationToken = default) {
    _context.UserSubscriptions.RemoveRange(entities);
    await Task.CompletedTask;
  }

  public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) {
    var entity = await GetByIdAsync(id, cancellationToken);

    if (entity != null) {
      // Assuming UserSubscription has soft delete functionality
      entity.DeletedAt = DateTime.UtcNow;
      _context.UserSubscriptions.Update(entity);
    }
  }

  public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default) {
    var entity = await _context.UserSubscriptions.IgnoreQueryFilters() // To include soft deleted items
                               .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (entity != null) {
      entity.DeletedAt = null;
      _context.UserSubscriptions.Update(entity);
    }
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { return await _context.SaveChangesAsync(cancellationToken); }

  // Subscription-specific methods

  public async Task<IEnumerable<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
  }

  public async Task<UserSubscription?> GetActiveUserSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active, cancellationToken);
  }

  public async Task<IEnumerable<UserSubscription>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(s => s.SubscriptionPlanId == planId).ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<UserSubscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(s => s.Status == status).ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<UserSubscription>> GetExpiringSoonAsync(int withinDays, CancellationToken cancellationToken = default) {
    var cutoffDate = DateTime.UtcNow.AddDays(withinDays);

    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(s => s.Status == SubscriptionStatus.Active && s.NextBillingAt.HasValue && s.NextBillingAt <= cutoffDate).ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<UserSubscription>> GetPendingBillingAsync(CancellationToken cancellationToken = default) {
    var now = DateTime.UtcNow;

    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(s => s.Status == SubscriptionStatus.Active && s.NextBillingAt.HasValue && s.NextBillingAt <= now).ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<UserSubscription>> GetTrialsEndingSoonAsync(int withinDays, CancellationToken cancellationToken = default) {
    var cutoffDate = DateTime.UtcNow.AddDays(withinDays);

    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).Where(s => s.Status == SubscriptionStatus.Trialing && s.TrialEndsAt.HasValue && s.TrialEndsAt <= cutoffDate).ToListAsync(cancellationToken);
  }

  public async Task<UserSubscription?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.Include(s => s.User).Include(s => s.SubscriptionPlan).FirstOrDefaultAsync(s => s.ExternalSubscriptionId == externalId, cancellationToken);
  }

  public async Task<int> GetCountByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default) { return await _context.UserSubscriptions.CountAsync(s => s.Status == status, cancellationToken); }

  public async Task<int> GetActiveSubscriptionCountAsync(Guid userId, CancellationToken cancellationToken = default) {
    return await _context.UserSubscriptions.CountAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active, cancellationToken);
  }

  public UserSubscription Add(UserSubscription entity) { return _context.UserSubscriptions.Add(entity).Entity; }

  public UserSubscription Update(UserSubscription entity) { return _context.UserSubscriptions.Update(entity).Entity; }

  public void Remove(UserSubscription entity) { _context.UserSubscriptions.Remove(entity); }

  public async Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    var entity = await GetByIdAsync(id, cancellationToken);
    if (entity != null) Remove(entity);
  }
}
