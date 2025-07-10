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
    if (!accessToken) {
      console.warn('[TenantDomainApiClient] Warning: No access token provided. Requests may fail due to missing authentication.');
    }
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

    // Debug log for outgoing request
    console.log('[TenantDomainApiClient] Request:', {
      url,
      method: options.method || 'GET',
      headers,
      body: options.body,
    });

    const config: RequestInit = {
      ...options,
      headers,
    };

    const response = await fetch(url, config);

    // Debug log for response status
    console.log('[TenantDomainApiClient] Response status:', response.status);

    if (!response.ok) {
      let errorData = {};
      try {
        errorData = await response.json();
      } catch (e) {
        // ignore
      }
      console.error('[TenantDomainApiClient] Error response body:', errorData);
      throw new Error((errorData as any).message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Domain Management
  async getDomains(tenantId?: string | null): Promise<TenantDomain[]> {
    let endpoint = '/api/tenant-domains';
    // Normalize tenantId: treat 'default-tenant-id' and '' as null
    const normalizedTenantId = !tenantId || tenantId === 'default-tenant-id' || tenantId === '' ? null : tenantId;
    console.log('[TenantDomainApiClient] getDomains called with tenantId:', normalizedTenantId);
    if (normalizedTenantId) {
      endpoint += `?tenantId=${normalizedTenantId}`;
    }
    console.log('[TenantDomainApiClient] getDomains final endpoint:', endpoint);
    return this.request<TenantDomain[]>(endpoint);
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
  async getUserGroups(tenantId: string | null): Promise<TenantUserGroup[]> {
    console.log('🔍 [TenantDomainApiClient] Getting user groups for tenantId:', tenantId);

    // For admin users (tenantId null), we don't have a global endpoint yet
    // Return empty array to avoid 404 errors
    if (tenantId === null || tenantId === '' || tenantId === 'default-tenant-id') {
      console.log('⚠️ [TenantDomainApiClient] Admin user or invalid tenantId - returning empty user groups');
      return [];
    }

    try {
      const result = await this.request<TenantUserGroup[]>(`/api/tenant-domains/user-groups?tenantId=${tenantId}`);
      console.log('✅ [TenantDomainApiClient] Successfully fetched user groups:', result.length);
      return result;
    } catch (error) {
      console.error('❌ [TenantDomainApiClient] Failed to fetch user groups:', error);
      throw error;
    }
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
