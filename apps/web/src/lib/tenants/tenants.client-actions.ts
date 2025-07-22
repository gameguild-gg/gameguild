// Client-safe tenant actions that don't use revalidateTag
import { auth } from '@/auth';
import { TenantResponse } from '@/lib/tenants/types';

interface ActionState {
  success: boolean;
  error?: string;
  tenant?: TenantResponse;
}

/**
 * Get tenants data with authentication (client-safe version)
 */
export async function getTenantsDataClient(page: number = 1, limit: number = 20, search?: string) {
  try {
    const session = await auth();
    
    if (!session?.accessToken) {
      return {
        tenants: [],
        pagination: { page, limit, total: 0, totalPages: 0 },
      };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    const skip = (page - 1) * limit;

    const params = new URLSearchParams({
      skip: skip.toString(),
      take: limit.toString(),
    });

    if (search) {
      params.append('search', search);
    }

    const response = await fetch(`${apiUrl}/api/tenants?${params}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      cache: 'no-store', // Don't cache in client components
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch tenants: ${response.status} ${response.statusText}`);
    }

    const tenants: TenantResponse[] = await response.json();

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
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch tenants');
  }
}

/**
 * Get user tenants (client-safe version)
 */
export async function getUserTenantsClient() {
  try {
    const session = await auth();
    
    if (!session?.accessToken) {
      return [];
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/tenants/user`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      cache: 'no-store',
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch user tenants: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching user tenants:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user tenants');
  }
}

/**
 * Create tenant (client-safe version)
 */
export async function createTenantClient(prevState: ActionState, formData: FormData): Promise<ActionState> {
  try {
    const session = await auth();
    
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'true';

    if (!name || name.trim().length < 2) {
      return { success: false, error: 'Name must be at least 2 characters long' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/tenants`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        name: name.trim(),
        description: description?.trim() || null,
        isActive,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to create tenant: ${response.status} ${response.statusText}`);
    }

    const tenant = await response.json();
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
 * Update tenant (client-safe version)
 */
export async function updateTenantClient(id: string, prevState: ActionState, formData: FormData): Promise<ActionState> {
  try {
    const session = await auth();
    
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'true';

    if (!name || name.trim().length < 2) {
      return { success: false, error: 'Name must be at least 2 characters long' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/tenants/${id}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        name: name.trim(),
        description: description?.trim() || null,
        isActive,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to update tenant: ${response.status} ${response.statusText}`);
    }

    const tenant = await response.json();
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
 * Delete tenant (client-safe version)
 */
export async function deleteTenantClient(id: string) {
  try {
    const session = await auth();
    
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

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

    return { success: true };
  } catch (error) {
    console.error('Error deleting tenant:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to delete tenant',
    };
  }
}
