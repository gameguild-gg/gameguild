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

export interface Tenant {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt?: string; // Make optional to match session data
  updatedAt?: string;
}

export interface TenantSummary {
  id: string;
  name: string;
  isActive: boolean;
  memberCount?: number;
  projectCount?: number;
}

export interface TenantMember {
  id: string;
  userId: string;
  tenantId: string;
  role: 'owner' | 'admin' | 'member';
  joinedAt: string;
  user?: {
    id: string;
    name: string;
    email: string;
  };
}
