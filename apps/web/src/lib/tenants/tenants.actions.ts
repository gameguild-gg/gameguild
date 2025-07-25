import { revalidateTag } from 'next/cache';
import { auth } from '@/auth';
import { Tenant, CreateTenantRequest, UpdateTenantRequest } from '@/lib/tenants/types';

export interface TenantData {
  tenants: Tenant[];
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface TenantActionState {
  success: boolean;
  error?: string;
  tenant?: Tenant;
}

// Cache configuration
const CACHE_TAGS = {
  TENANTS: 'tenants',
  TENANT_DETAIL: 'tenant-detail',
  USER_TENANTS: 'user-tenants',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get all tenants (admin function) with authentication
 */
export async function getTenantsData(page: number = 1, limit: number = 20): Promise<TenantData> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return {
        tenants: [],
        pagination: { page, limit, total: 0, totalPages: 0 },
      };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const skip = (page - 1) * limit;

    const params = new URLSearchParams({
      skip: skip.toString(),
      take: limit.toString(),
    });

    const response = await fetch(`${apiUrl}/api/tenants?${params}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.TENANTS],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch tenants: ${response.status} ${response.statusText}`);
    }

    const tenants: Tenant[] = await response.json();

    return {
      tenants,
      pagination: {
        page,
        limit,
        total: tenants.length,
        totalPages: Math.ceil(tenants.length / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching tenants:', error);

    // Return empty data for network errors
    if (error instanceof TypeError && error.message.includes('fetch')) {
      return {
        tenants: [],
        pagination: { page, limit, total: 0, totalPages: 0 },
      };
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch tenants');
  }
}

/**
 * Get current user's tenants with authentication
 */
export async function getUserTenants(): Promise<Tenant[]> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return [];
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/tenants/user`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.USER_TENANTS],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch user tenants: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching user tenants:', error);

    if (error instanceof TypeError && error.message.includes('fetch')) {
      return [];
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user tenants');
  }
}

/**
 * Get tenant by ID with authentication
 */
export async function getTenantById(id: string): Promise<Tenant | null> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return null;
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/tenants/${id}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.TENANT_DETAIL, `tenant-${id}`],
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch tenant: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching tenant:', error);

    if (error instanceof TypeError && error.message.includes('fetch')) {
      return null;
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch tenant');
  }
}

/**
 * Create a new tenant (Server Action)
 */
export async function createTenant(prevState: TenantActionState, formData: FormData): Promise<TenantActionState> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'true';

    // Validation
    if (!name || name.trim().length < 2) {
      return { success: false, error: 'Tenant name must be at least 2 characters long' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const tenantData: CreateTenantRequest = {
      name: name.trim(),
      description: description?.trim() || undefined,
      isActive,
    };

    const response = await fetch(`${apiUrl}/api/tenants`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(tenantData),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to create tenant: ${response.status} ${response.statusText}`);
    }

    const tenant = await response.json();

    // Revalidate caches
    await revalidateTenantsData();

    return { success: true, tenant };
  } catch (error) {
    console.error('Error creating tenant:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to create tenant',
    };
  }
}

/**
 * Update tenant (Server Action)
 */
export async function updateTenant(id: string, prevState: TenantActionState, formData: FormData): Promise<TenantActionState> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'true';

    // Validation
    if (!name || name.trim().length < 2) {
      return { success: false, error: 'Tenant name must be at least 2 characters long' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const tenantData: UpdateTenantRequest = {
      name: name.trim(),
      description: description?.trim() || undefined,
      isActive,
    };

    const response = await fetch(`${apiUrl}/api/tenants/${id}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(tenantData),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to update tenant: ${response.status} ${response.statusText}`);
    }

    const tenant = await response.json();

    // Revalidate caches
    await revalidateTenantsData();
    revalidateTag(`tenant-${id}`);

    return { success: true, tenant };
  } catch (error) {
    console.error('Error updating tenant:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to update tenant',
    };
  }
}

/**
 * Delete tenant (Server Action)
 */
export async function deleteTenant(id: string): Promise<{ success: boolean; error?: string }> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/tenants/${id}`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to delete tenant: ${response.status} ${response.statusText}`);
    }

    // Revalidate caches
    await revalidateTenantsData();
    revalidateTag(`tenant-${id}`);

    return { success: true };
  } catch (error) {
    console.error('Error deleting tenant:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to delete tenant',
    };
  }
}

/**
 * Revalidate tenants data cache
 */
export async function revalidateTenantsData(): Promise<void> {
  revalidateTag(CACHE_TAGS.TENANTS);
  revalidateTag(CACHE_TAGS.TENANT_DETAIL);
  revalidateTag(CACHE_TAGS.USER_TENANTS);
}
