export interface CreateTenantRequest {
  name: string;
  description?: string;
  isActive?: boolean;
}

export interface UpdateTenantRequest {
  name?: string;
  description?: string;
  isActive?: boolean;
}

export interface TenantResponse {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt?: string; // Make optional to match session data
  updatedAt?: string;
}

export interface TenantUser {
  userId: string;
  tenantId: string;
  roles: string[];
  isActive: boolean;
}
