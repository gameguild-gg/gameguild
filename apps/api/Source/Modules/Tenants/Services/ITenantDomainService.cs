using GameGuild.Modules.Tenants.Entities;
using UserModel = GameGuild.Modules.Users.User;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Service for managing tenant domain-based user group assignments
/// </summary>
public interface ITenantDomainService {
  // Domain Management
  Task<TenantDomain> CreateDomainAsync(TenantDomain domain);

  Task<TenantDomain?> GetDomainByIdAsync(Guid id);

  Task<IEnumerable<TenantDomain>> GetDomainsByTenantAsync(Guid tenantId);

  Task<IEnumerable<TenantDomain>> GetAllDomainsAsync();

  Task<TenantDomain?> GetDomainByFullDomainAsync(string fullDomain);

  Task<TenantDomain> UpdateDomainAsync(TenantDomain domain);

  Task<bool> DeleteDomainAsync(Guid id);

  Task<bool> SetMainDomainAsync(Guid tenantId, Guid domainId);

  // User Group Management
  Task<TenantUserGroup> CreateUserGroupAsync(TenantUserGroup userGroup);

  Task<TenantUserGroup?> GetUserGroupByIdAsync(Guid id);

  Task<IEnumerable<TenantUserGroup>> GetUserGroupsByTenantAsync(Guid tenantId);

  Task<IEnumerable<TenantUserGroup>> GetRootUserGroupsByTenantAsync(Guid tenantId);

  Task<TenantUserGroup> UpdateUserGroupAsync(TenantUserGroup userGroup);

  Task<bool> DeleteUserGroupAsync(Guid id);

  // User Group Membership Management
  Task<TenantUserGroupMembership> AddUserToGroupAsync(Guid userId, Guid userGroupId, bool isAutoAssigned = false);

  Task<bool> RemoveUserFromGroupAsync(Guid userId, Guid userGroupId);

  Task<IEnumerable<TenantUserGroupMembership>> GetUserGroupMembershipsAsync(Guid userId);

  Task<IEnumerable<TenantUserGroupMembership>> GetGroupMembersAsync(Guid userGroupId);

  Task<bool> IsUserInGroupAsync(Guid userId, Guid userGroupId);

  // Domain-based Auto-assignment
  Task<IEnumerable<TenantUserGroupMembership>> AutoAssignUserToGroupsAsync(string userEmail);

  Task<IEnumerable<TenantUserGroupMembership>> AutoAssignUserToGroupsAsync(Guid userId);

  Task<TenantDomain?> FindMatchingDomainAsync(string email);

  // Bulk Operations
  Task<int> AutoAssignAllUsersAsync();

  Task<int> AutoAssignTenantUsersAsync(Guid tenantId);

  // Query Methods
  Task<IEnumerable<UserModel>> GetUsersInGroupAsync(Guid userGroupId, bool includeSubGroups = false);

  Task<IEnumerable<TenantUserGroup>> GetUserGroupsForUserAsync(Guid userId);

  Task<bool> ValidateDomainOwnershipAsync(Guid tenantId, string domain);
}
