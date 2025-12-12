'use server';

// Helper to strip non-serializable fields (Request/Response objects) before crossing the RSC boundary
function toPlainResult<TData = unknown, TError = unknown>(result: any): { data: TData | null; error: TError | null; status: number | null } {
    return {
        data: (result && 'data' in result ? result.data : null) ?? null,
        error: (result && 'error' in result ? result.error : null) ?? null,
        status: result?.response?.status ?? null,
    };
}

/**
 * Get all tenants
 */
export async function getTenantsAction(_params?: any) {
    // STUB: tenants listing disabled
    return toPlainResult({ data: [], response: { status: 200 } });
}

/**
 * Get tenant by ID
 */
export async function getTenantByIdAction(_tenantId: string) {
    // STUB: tenant details disabled
    return toPlainResult({ data: null, response: { status: 404 } });
}

/**
 * Search tenants with filters
 */
export async function searchTenantsAction(_params?: any) {
    // STUB: search disabled; return empty list
    return toPlainResult({ data: [], response: { status: 200 } });
}

/**
 * Get tenant statistics
 */
export async function getTenantStatisticsAction(_tenantId: string) {
    // STUB: statistics disabled
    return toPlainResult({ data: {}, response: { status: 200 } });
}

/**
 * Activate tenant
 */
export async function activateTenantAction(_tenantId: string) {
    // STUB: activation disabled
    return toPlainResult({ error: { message: 'Tenant management is disabled' }, response: { status: 403 } });
}

/**
 * Deactivate tenant
 */
export async function deactivateTenantAction(_tenantId: string) {
    // STUB: deactivation disabled
    return toPlainResult({ error: { message: 'Tenant management is disabled' }, response: { status: 403 } });
}

/**
 * Permanently delete tenant
 */
export async function permanentDeleteTenantAction(_tenantId: string) {
    // STUB: deletion disabled
    return toPlainResult({ error: { message: 'Tenant management is disabled' }, response: { status: 403 } });
}
