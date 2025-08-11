'use server';

import { createTenantAction, deleteTenantAction, updateTenantAction } from './tenants.actions';
import type { CreateTenantDto, UpdateTenantDto, Tenant } from '@/lib/api/generated/types.gen';

interface ActionResult {
  success: boolean;
  error?: string;
  data?: Tenant;
}

/**
 * Create a new tenant - client action wrapper
 */
export async function createTenantClient(prevState: ActionResult, formData: FormData): Promise<ActionResult> {
  try {
    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'on';

    if (!name?.trim()) {
      return { success: false, error: 'Tenant name is required' };
    }

    // Generate slug from name
    const slug = name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');

    const createData: CreateTenantDto = {
      name: name.trim(),
      description: description?.trim() || undefined,
      isActive,
      slug,
    };

    const result = await createTenantAction({
      body: createData,
      url: '/api/tenants',
    });

    if (result.error) {
      return { success: false, error: String(result.error) || 'Failed to create tenant' };
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Create tenant error:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Update an existing tenant - client action wrapper
 */
export async function updateTenantClient(tenantId: string, prevState: ActionResult, formData: FormData): Promise<ActionResult> {
  try {
    if (!tenantId) {
      return { success: false, error: 'Tenant ID is required' };
    }

    const name = formData.get('name') as string;
    const description = formData.get('description') as string;
    const isActive = formData.get('isActive') === 'on';

    if (!name?.trim()) {
      return { success: false, error: 'Tenant name is required' };
    }

    // Generate slug from name
    const slug = name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');

    const updateData: UpdateTenantDto = {
      name: name.trim(),
      description: description?.trim() || undefined,
      isActive,
      slug,
    };

    const result = await updateTenantAction({
      path: { id: tenantId },
      body: updateData,
      url: '/api/tenants/{id}',
    });

    if (result.error) {
      return { success: false, error: String(result.error) || 'Failed to update tenant' };
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Update tenant error:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Update tenant via form submission without needing to bind the tenant ID at hook init.
 * This avoids the stale parameter issue when using useActionState with a dynamic tenant.
 */
export async function updateTenantFormClient(prevState: ActionResult, formData: FormData): Promise<ActionResult> {
  const tenantId = (formData.get('tenantId') as string) || '';
  return updateTenantClient(tenantId, prevState, formData);
}

/**
 * Delete a tenant - client action wrapper
 */
export async function deleteTenantClient(tenantId: string): Promise<ActionResult> {
  try {
    if (!tenantId) {
      return { success: false, error: 'Tenant ID is required' };
    }

    const result = await deleteTenantAction({
      path: { id: tenantId },
      url: '/api/tenants/{id}',
    });

    if (result.error) {
      return { success: false, error: String(result.error) || 'Failed to delete tenant' };
    }

    return { success: true };
  } catch (error) {
    console.error('Delete tenant error:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}
