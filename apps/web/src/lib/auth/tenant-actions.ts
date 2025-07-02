'use server';

import { httpClientFactory } from '@/lib/core/http';
import { CreateTenantRequest, TenantResponse, UpdateTenantRequest } from '@/lib/tenant/types';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';

export type TenantActionState = {
  error?: string;
  success?: boolean;
  data?: TenantResponse | TenantResponse[];
};

export async function getUserTenants(): Promise<TenantActionState> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return {
        error: 'Not authenticated',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request<TenantResponse[]>({
      method: 'GET',
      url: `${environment.apiBaseUrl}/tenants/user`,
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to fetch tenants',
        success: false,
      };
    }

    return {
      success: true,
      data: response.body,
    };
  } catch (error) {
    console.error('Get user tenants error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function getTenantById(tenantId: string): Promise<TenantActionState> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return {
        error: 'Not authenticated',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request<TenantResponse>({
      method: 'GET',
      url: `${environment.apiBaseUrl}/tenants/${tenantId}`,
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to fetch tenant',
        success: false,
      };
    }

    return {
      success: true,
      data: response.body,
    };
  } catch (error) {
    console.error('Get tenant error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function createTenant(previousState: TenantActionState, formData: FormData): Promise<TenantActionState> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return {
        error: 'Not authenticated',
        success: false,
      };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'true';

    if (!name) {
      return {
        error: 'Tenant name is required',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const requestData: CreateTenantRequest = {
      name,
      description: description || undefined,
      isActive,
    };

    const response = await httpClient.request<TenantResponse>({
      method: 'POST',
      url: `${environment.apiBaseUrl}/tenants`,
      body: requestData,
    });

    if (response.statusCode !== 200 && response.statusCode !== 201) {
      return {
        error: (response.body as any)?.message || 'Failed to create tenant',
        success: false,
      };
    }

    return {
      success: true,
      data: response.body,
    };
  } catch (error) {
    console.error('Create tenant error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function updateTenant(tenantId: string, previousState: TenantActionState, formData: FormData): Promise<TenantActionState> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return {
        error: 'Not authenticated',
        success: false,
      };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'true';

    const httpClient = httpClientFactory();

    const requestData: UpdateTenantRequest = {
      name: name || undefined,
      description: description || undefined,
      isActive,
    };

    const response = await httpClient.request<TenantResponse>({
      method: 'PUT',
      url: `${environment.apiBaseUrl}/tenants/${tenantId}`,
      body: requestData,
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to update tenant',
        success: false,
      };
    }

    return {
      success: true,
      data: response.body,
    };
  } catch (error) {
    console.error('Update tenant error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function deleteTenant(tenantId: string): Promise<TenantActionState> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return {
        error: 'Not authenticated',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request({
      method: 'DELETE',
      url: `${environment.apiBaseUrl}/tenants/${tenantId}`,
    });

    if (response.statusCode !== 200 && response.statusCode !== 204) {
      return {
        error: (response.body as any)?.message || 'Failed to delete tenant',
        success: false,
      };
    }

    return {
      success: true,
    };
  } catch (error) {
    console.error('Delete tenant error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}
