import {
  TenantDomain,
  CreateTenantDomainRequest,
  UpdateTenantDomainRequest,
  TenantUserGroup,
  CreateTenantUserGroupRequest,
  UpdateTenantUserGroupRequest,
  TenantUserGroupMembership,
  AddUserToGroupRequest,
  AutoAssignUserRequest,
} from '@/types/tenant-domain';

class TenantDomainApiClient {
  private baseUrl: string;
  private accessToken?: string;

  constructor(baseUrl: string, accessToken?: string) {
    this.baseUrl = baseUrl;
    this.accessToken = accessToken;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    // Add existing headers from options
    if (options.headers) {
      Object.assign(headers, options.headers);
    }

    // Add authorization header if access token is available
    if (this.accessToken) {
      headers.Authorization = `Bearer ${this.accessToken}`;
    }

    const config: RequestInit = {
      ...options,
      headers,
    };

    const response = await fetch(url, config);

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Domain Management
  async getDomains(tenantId: string): Promise<TenantDomain[]> {
    return this.request<TenantDomain[]>(`/api/tenant-domains?tenantId=${tenantId}`);
  }

  async getDomain(id: string): Promise<TenantDomain> {
    return this.request<TenantDomain>(`/api/tenant-domains/${id}`);
  }

  async createDomain(domain: CreateTenantDomainRequest): Promise<TenantDomain> {
    return this.request<TenantDomain>('/api/tenant-domains', {
      method: 'POST',
      body: JSON.stringify(domain),
    });
  }

  async updateDomain(id: string, updates: UpdateTenantDomainRequest): Promise<TenantDomain> {
    return this.request<TenantDomain>(`/api/tenant-domains/${id}`, {
      method: 'PUT',
      body: JSON.stringify(updates),
    });
  }

  async deleteDomain(id: string): Promise<void> {
    await this.request(`/api/tenant-domains/${id}`, {
      method: 'DELETE',
    });
  }

  async setMainDomain(tenantId: string, domainId: string): Promise<void> {
    await this.request(`/api/tenant-domains/${tenantId}/set-main/${domainId}`, {
      method: 'POST',
    });
  }

  // User Group Management
  async getUserGroups(tenantId: string): Promise<TenantUserGroup[]> {
    return this.request<TenantUserGroup[]>(`/api/tenant-domains/user-groups/tenant/${tenantId}`);
  }

  async getUserGroup(id: string): Promise<TenantUserGroup> {
    return this.request<TenantUserGroup>(`/api/tenant-domains/user-groups/${id}`);
  }

  async createUserGroup(group: CreateTenantUserGroupRequest): Promise<TenantUserGroup> {
    return this.request<TenantUserGroup>('/api/tenant-domains/user-groups', {
      method: 'POST',
      body: JSON.stringify(group),
    });
  }

  async updateUserGroup(id: string, updates: UpdateTenantUserGroupRequest): Promise<TenantUserGroup> {
    return this.request<TenantUserGroup>(`/api/tenant-domains/user-groups/${id}`, {
      method: 'PUT',
      body: JSON.stringify(updates),
    });
  }

  async deleteUserGroup(id: string): Promise<void> {
    await this.request(`/api/tenant-domains/user-groups/${id}`, {
      method: 'DELETE',
    });
  }

  // Membership Management
  async getUserMemberships(userId: string): Promise<TenantUserGroupMembership[]> {
    return this.request<TenantUserGroupMembership[]>(`/api/tenant-domains/memberships/user/${userId}`);
  }

  async getGroupMemberships(groupId: string): Promise<TenantUserGroupMembership[]> {
    return this.request<TenantUserGroupMembership[]>(`/api/tenant-domains/memberships/group/${groupId}`);
  }

  async addUserToGroup(membership: AddUserToGroupRequest): Promise<TenantUserGroupMembership> {
    return this.request<TenantUserGroupMembership>('/api/tenant-domains/memberships', {
      method: 'POST',
      body: JSON.stringify(membership),
    });
  }

  async removeUserFromGroup(membershipId: string): Promise<void> {
    await this.request(`/api/tenant-domains/memberships/${membershipId}`, {
      method: 'DELETE',
    });
  }

  async autoAssignUser(request: AutoAssignUserRequest): Promise<TenantUserGroupMembership> {
    return this.request<TenantUserGroupMembership>('/api/tenant-domains/auto-assign', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // Query Methods
  async getUsersByGroup(groupId: string): Promise<{ id: string; name: string; email: string }[]> {
    return this.request<{ id: string; name: string; email: string }[]>(`/api/tenant-domains/groups/${groupId}/users`);
  }

  async getGroupsByUser(userId: string): Promise<TenantUserGroup[]> {
    return this.request<TenantUserGroup[]>(`/api/tenant-domains/users/${userId}/groups`);
  }
}

export { TenantDomainApiClient };
