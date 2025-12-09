'use server';

/**
 * Stub implementations for tenant client actions.
 * These are admin-specific actions for tenant management.
 */

export async function getTenantById(_id: string) {
    return { data: null, error: null };
}

export async function getTenantStatistics(_id: string) {
    return { data: {}, error: null };
}

export async function updateTenant(_id: string, _data: any) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function updateTenantClient(_id: string, _prevState: any, _formData?: FormData) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function updateTenantFormClient(_prevState: { success: boolean; error: string }, _formData?: FormData) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function createTenantClient(_prevState: { success: boolean; error: string }, _formData?: FormData) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function activateTenant(_id: string) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function deactivateTenant(_id: string) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function deleteTenant(_id: string) {
    return { success: false, error: 'Tenant management is disabled' };
}

export async function deleteTenantClient(_id: string) {
    return { success: false, error: 'Tenant management is disabled' };
}

// Also export action functions with 'Action' suffix for compatibility
export const getTenantStatisticsAction = getTenantStatistics;
export const activateTenantAction = activateTenant;
export const deactivateTenantAction = deactivateTenant;
