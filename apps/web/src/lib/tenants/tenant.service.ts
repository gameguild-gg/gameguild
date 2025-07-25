import { apiClient } from '../api/api-client';
import { CreateTenantRequest, Tenant, UpdateTenantRequest } from '@/lib/tenants/types';

export class TenantService {
  // Get all tenants (admin function)
  static async getAllTenants(accessToken: string): Promise<Tenant[]> {
    return apiClient.authenticatedRequest<Tenant[]>('/tenants', accessToken);
  }

  // Get a specific tenant by ID
  static async getTenantById(tenantId: string, accessToken: string): Promise<Tenant> {
    return apiClient.authenticatedRequest<Tenant>(`/tenants/${tenantId}`, accessToken);
  }

  // Get a tenant by name
  static async getTenantByName(name: string, accessToken: string): Promise<Tenant> {
    return apiClient.authenticatedRequest<Tenant>(`/tenants/by-name/${name}`, accessToken);
  }

  // Create a new tenant (admin function)
  static async createTenant(tenantData: CreateTenantRequest, accessToken: string): Promise<Tenant> {
    return apiClient.authenticatedRequest<Tenant>('/tenants', accessToken, {
      method: 'POST',
      body: JSON.stringify(tenantData),
    });
  }

  // Update a tenant (admin function)
  static async updateTenant(tenantId: string, tenantData: UpdateTenantRequest, accessToken: string): Promise<Tenant> {
    return apiClient.authenticatedRequest<Tenant>(`/tenants/${tenantId}`, accessToken, {
      method: 'PUT',
      body: JSON.stringify(tenantData),
    });
  }

  // Delete a tenant (admin function)
  static async deleteTenant(tenantId: string, accessToken: string): Promise<void> {
    await apiClient.authenticatedRequest(`/tenants/${tenantId}`, accessToken, {
      method: 'DELETE',
    });
  }

  // Get current user's tenant memberships
  static async getUserTenants(accessToken: string): Promise<Tenant[]> {
    return apiClient.authenticatedRequest<Tenant[]>('/tenants/user', accessToken);
  }
}
