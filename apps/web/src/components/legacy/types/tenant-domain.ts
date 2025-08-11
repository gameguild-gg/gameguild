// Tenant Domain Types
export interface TenantDomain {
  id: string;
  tenantId: string;
  topLevelDomain: string;
  subdomain?: string;
  isMainDomain: boolean;
  isSecondaryDomain: boolean;
  userGroupId?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTenantDomainRequest {
  tenantId: string;
  topLevelDomain: string;
  subdomain?: string;
  isMainDomain: boolean;
  isSecondaryDomain: boolean;
}

export interface UpdateTenantDomainRequest {
  isMainDomain?: boolean;
  isSecondaryDomain?: boolean;
}

// Tenant User Group Types
export interface TenantUserGroup {
  id: string;
  tenantId: string;
  name: string;
  description?: string;
  isDefault: boolean;
  parentGroupId?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTenantUserGroupRequest {
  tenantId: string;
  name: string;
  description?: string;
  isDefault: boolean;
  parentGroupId?: string;
}

export interface UpdateTenantUserGroupRequest {
  name?: string;
  description?: string;
  isDefault?: boolean;
  parentGroupId?: string;
}

// Tenant User Group Membership Types
export interface TenantUserGroupMembership {
  id: string;
  userId: string;
  groupId: string;
  isAutoAssigned: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface AddUserToGroupRequest {
  userId: string;
  userGroupId: string;
  isAutoAssigned: boolean;
}

export interface AutoAssignUserRequest {
  userId: string;
  email: string;
}
