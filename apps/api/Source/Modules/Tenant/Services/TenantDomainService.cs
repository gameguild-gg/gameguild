using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Modules.Tenant.Models;
using UserModel = GameGuild.Modules.User.Models.User;


namespace GameGuild.Modules.Tenant.Services;

/// <summary>
/// Implementation of tenant domain service for managing domain-based user group assignments
/// </summary>
public class TenantDomainService : ITenantDomainService {
  private readonly ApplicationDbContext _context;

  public TenantDomainService(ApplicationDbContext context) { _context = context; }

  #region Domain Management

  public async Task<TenantDomain> CreateDomainAsync(TenantDomain domain) {
    domain.Id = Guid.NewGuid();
    domain.CreatedAt = DateTime.UtcNow;
    domain.UpdatedAt = DateTime.UtcNow;

    // If this is marked as main domain, unset other main domains for the tenant
    if (domain.IsMainDomain) await UnsetMainDomainsForTenantAsync(domain.TenantId);

    _context.TenantDomains.Add(domain);
    await _context.SaveChangesAsync();

    return await GetDomainByIdAsync(domain.Id) ?? domain;
  }

  public async Task<TenantDomain?> GetDomainByIdAsync(Guid id) {
    return await _context.TenantDomains.Include(d => d.Tenant)
                         .Include(d => d.UserGroup)
                         .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);
  }

  public async Task<IEnumerable<TenantDomain>> GetDomainsByTenantAsync(Guid tenantId) {
    return await _context.TenantDomains.Where(d => d.TenantId == tenantId && d.DeletedAt == null)
                         .Include(d => d.UserGroup)
                         .OrderBy(d => d.IsMainDomain ? 0 : 1)
                         .ThenBy(d => d.TopLevelDomain)
                         .ToListAsync();
  }

  public async Task<TenantDomain?> GetDomainByFullDomainAsync(string fullDomain) {
    var normalizedDomain = fullDomain.ToLowerInvariant();

    return await _context.TenantDomains.Include(d => d.Tenant)
                         .Include(d => d.UserGroup)
                         .FirstOrDefaultAsync(d => d.DeletedAt == null &&
                                                   ((d.Subdomain == null && d.TopLevelDomain == normalizedDomain) ||
                                                    (d.Subdomain != null &&
                                                     $"{d.Subdomain}.{d.TopLevelDomain}" ==
                                                     normalizedDomain))
                         );
  }

  public async Task<TenantDomain> UpdateDomainAsync(TenantDomain domain) {
    var existingDomain = await _context.TenantDomains.FindAsync(domain.Id);

    if (existingDomain == null) throw new InvalidOperationException($"Domain with ID {domain.Id} not found.");

    // If changing to main domain, unset other main domains for the tenant
    if (domain.IsMainDomain && !existingDomain.IsMainDomain) await UnsetMainDomainsForTenantAsync(domain.TenantId);

    existingDomain.TopLevelDomain = domain.TopLevelDomain;
    existingDomain.Subdomain = domain.Subdomain;
    existingDomain.IsMainDomain = domain.IsMainDomain;
    existingDomain.IsSecondaryDomain = domain.IsSecondaryDomain;
    existingDomain.UserGroupId = domain.UserGroupId;
    existingDomain.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return await GetDomainByIdAsync(existingDomain.Id) ?? existingDomain;
  }

  public async Task<bool> DeleteDomainAsync(Guid id) {
    var domain = await _context.TenantDomains.FindAsync(id);

    if (domain == null) return false;

    domain.DeletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> SetMainDomainAsync(Guid tenantId, Guid domainId) {
    var domain =
      await _context.TenantDomains.FirstOrDefaultAsync(d =>
                                                         d.Id == domainId && d.TenantId == tenantId && d.DeletedAt == null
      );

    if (domain == null) return false;

    await UnsetMainDomainsForTenantAsync(tenantId);

    domain.IsMainDomain = true;
    domain.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return true;
  }

  private async Task UnsetMainDomainsForTenantAsync(Guid tenantId) {
    var mainDomains = await _context.TenantDomains
                                    .Where(d => d.TenantId == tenantId && d.IsMainDomain && d.DeletedAt == null)
                                    .ToListAsync();

    foreach (var mainDomain in mainDomains) {
      mainDomain.IsMainDomain = false;
      mainDomain.UpdatedAt = DateTime.UtcNow;
    }
  }

  #endregion

  #region User Group Management

  public async Task<TenantUserGroup> CreateUserGroupAsync(TenantUserGroup userGroup) {
    userGroup.Id = Guid.NewGuid();
    userGroup.CreatedAt = DateTime.UtcNow;
    userGroup.UpdatedAt = DateTime.UtcNow;

    _context.TenantUserGroups.Add(userGroup);
    await _context.SaveChangesAsync();

    return await GetUserGroupByIdAsync(userGroup.Id) ?? userGroup;
  }

  public async Task<TenantUserGroup?> GetUserGroupByIdAsync(Guid id) {
    return await _context.TenantUserGroups.Include(g => g.Tenant)
                         .Include(g => g.ParentGroup)
                         .Include(g => g.SubGroups)
                         .Include(g => g.Domains)
                         .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);
  }

  public async Task<IEnumerable<TenantUserGroup>> GetUserGroupsByTenantAsync(Guid tenantId) {
    return await _context.TenantUserGroups.Where(g => g.TenantId == tenantId && g.DeletedAt == null)
                         .Include(g => g.ParentGroup)
                         .Include(g => g.SubGroups)
                         .OrderBy(g => g.Name)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TenantUserGroup>> GetRootUserGroupsByTenantAsync(Guid tenantId) {
    return await _context.TenantUserGroups
                         .Where(g => g.TenantId == tenantId && g.ParentGroupId == null && g.DeletedAt == null)
                         .Include(g => g.SubGroups)
                         .OrderBy(g => g.Name)
                         .ToListAsync();
  }

  public async Task<TenantUserGroup> UpdateUserGroupAsync(TenantUserGroup userGroup) {
    var existingGroup = await _context.TenantUserGroups.FindAsync(userGroup.Id);

    if (existingGroup == null) throw new InvalidOperationException($"User group with ID {userGroup.Id} not found.");

    existingGroup.Name = userGroup.Name;
    existingGroup.Description = userGroup.Description;
    existingGroup.ParentGroupId = userGroup.ParentGroupId;
    existingGroup.IsActive = userGroup.IsActive;
    existingGroup.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return await GetUserGroupByIdAsync(existingGroup.Id) ?? existingGroup;
  }

  public async Task<bool> DeleteUserGroupAsync(Guid id) {
    var userGroup = await _context.TenantUserGroups.FindAsync(id);

    if (userGroup == null) return false;

    userGroup.DeletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return true;
  }

  #endregion

  #region User Group Membership Management

  public async Task<TenantUserGroupMembership> AddUserToGroupAsync(
    Guid userId, Guid userGroupId,
    bool isAutoAssigned = false
  ) {
    // Check if membership already exists
    var existingMembership =
      await _context.TenantUserGroupMemberships.FirstOrDefaultAsync(m =>
                                                                      m.UserId == userId && m.UserGroupId == userGroupId
      );

    if (existingMembership != null) return existingMembership;

    var membership = new TenantUserGroupMembership(userId, userGroupId, isAutoAssigned) { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    _context.TenantUserGroupMemberships.Add(membership);
    await _context.SaveChangesAsync();

    return await _context.TenantUserGroupMemberships.Include(m => m.User)
                         .Include(m => m.UserGroup)
                         .FirstAsync(m => m.Id == membership.Id);
  }

  public async Task<bool> RemoveUserFromGroupAsync(Guid userId, Guid userGroupId) {
    var membership =
      await _context.TenantUserGroupMemberships.FirstOrDefaultAsync(m =>
                                                                      m.UserId == userId && m.UserGroupId == userGroupId
      );

    if (membership == null) return false;

    _context.TenantUserGroupMemberships.Remove(membership);
    await _context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<TenantUserGroupMembership>> GetUserGroupMembershipsAsync(Guid userId) {
    return await _context.TenantUserGroupMemberships.Where(m => m.UserId == userId)
                         .Include(m => m.UserGroup)
                         .ThenInclude(g => g.Tenant)
                         .OrderBy(m => m.UserGroup.Name)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TenantUserGroupMembership>> GetGroupMembersAsync(Guid userGroupId) {
    return await _context.TenantUserGroupMemberships.Where(m => m.UserGroupId == userGroupId)
                         .Include(m => m.User)
                         .OrderBy(m => m.User.Name)
                         .ToListAsync();
  }

  public async Task<bool> IsUserInGroupAsync(Guid userId, Guid userGroupId) { return await _context.TenantUserGroupMemberships.AnyAsync(m => m.UserId == userId && m.UserGroupId == userGroupId); }

  #endregion

  #region Domain-based Auto-assignment

  public async Task<IEnumerable<TenantUserGroupMembership>> AutoAssignUserToGroupsAsync(string userEmail) {
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

    if (user == null) return Enumerable.Empty<TenantUserGroupMembership>();

    return await AutoAssignUserToGroupsAsync(user.Id);
  }

  public async Task<IEnumerable<TenantUserGroupMembership>> AutoAssignUserToGroupsAsync(Guid userId) {
    var user = await _context.Users.FindAsync(userId);

    if (user == null) return Enumerable.Empty<TenantUserGroupMembership>();

    var matchingDomain = await FindMatchingDomainAsync(user.Email);

    if (matchingDomain?.UserGroupId == null) return Enumerable.Empty<TenantUserGroupMembership>();

    var memberships = new List<TenantUserGroupMembership>();

    // Add user to the matched group
    var membership = await AddUserToGroupAsync(userId, matchingDomain.UserGroupId.Value, true);
    memberships.Add(membership);

    return memberships;
  }

  public async Task<TenantDomain?> FindMatchingDomainAsync(string email) {
    if (string.IsNullOrEmpty(email) || !email.Contains('@')) return null;

    var emailDomain = email.Split('@')[1].ToLowerInvariant();

    return await _context.TenantDomains.Include(d => d.UserGroup)
                         .Include(d => d.Tenant)
                         .FirstOrDefaultAsync(d =>
                                                d.DeletedAt == null &&
                                                d.UserGroupId != null &&
                                                ((d.Subdomain == null && d.TopLevelDomain == emailDomain) ||
                                                 (d.Subdomain != null &&
                                                  $"{d.Subdomain}.{d.TopLevelDomain}" ==
                                                  emailDomain))
                         );
  }

  public async Task<int> AutoAssignAllUsersAsync() {
    var users = await _context.Users.Where(u => u.DeletedAt == null).ToListAsync();

    var assignedCount = 0;

    foreach (var user in users) {
      var memberships = await AutoAssignUserToGroupsAsync(user.Id);
      assignedCount += memberships.Count();
    }

    return assignedCount;
  }

  public async Task<int> AutoAssignTenantUsersAsync(Guid tenantId) {
    var tenantDomains = await GetDomainsByTenantAsync(tenantId);
    var domainStrings = tenantDomains.Select(d => d.FullDomainName).ToList();

    if (!domainStrings.Any()) return 0;

    var users = await _context.Users
                              .Where(u => u.DeletedAt == null && domainStrings.Any(domain => u.Email.ToLower().EndsWith("@" + domain)))
                              .ToListAsync();

    var assignedCount = 0;

    foreach (var user in users) {
      var memberships = await AutoAssignUserToGroupsAsync(user.Id);
      assignedCount += memberships.Count();
    }

    return assignedCount;
  }

  #endregion

  #region Query Methods

  public async Task<IEnumerable<UserModel>> GetUsersInGroupAsync(Guid userGroupId, bool includeSubGroups = false) {
    var groupIds = new List<Guid> { userGroupId };

    if (includeSubGroups) {
      var group = await GetUserGroupByIdAsync(userGroupId);

      if (group != null) {
        var descendants = group.GetAllDescendants();
        groupIds.AddRange(descendants.Select(g => g.Id));
      }
    }

    return await _context.TenantUserGroupMemberships.Where(m => groupIds.Contains(m.UserGroupId))
                         .Select(m => m.User)
                         .Distinct()
                         .OrderBy(u => u.Name)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TenantUserGroup>> GetUserGroupsForUserAsync(Guid userId) {
    return await _context.TenantUserGroupMemberships.Where(m => m.UserId == userId)
                         .Select(m => m.UserGroup)
                         .Include(g => g.Tenant)
                         .Include(g => g.ParentGroup)
                         .OrderBy(g => g.Tenant.Name)
                         .ThenBy(g => g.Name)
                         .ToListAsync();
  }

  public async Task<bool> ValidateDomainOwnershipAsync(Guid tenantId, string domain) {
    // This is a placeholder for domain ownership validation
    // In a real implementation, you might check DNS records, verify through email, etc.
    return true;
  }

  #endregion
}
