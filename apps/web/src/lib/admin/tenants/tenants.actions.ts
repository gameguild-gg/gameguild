'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getApiTenants, getApiTenantsById, getApiTenantsSearch } from '@/lib/api/generated/sdk.gen';
import type { GetApiTenantsData, GetApiTenantsSearchData } from '@/lib/api/generated/types.gen';

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
export async function getTenantsAction(params?: GetApiTenantsData) {
    await configureAuthenticatedClient();

    try {
        const result = await getApiTenants({
            query: params?.query,
        });

        return toPlainResult(result);
    } catch (error) {
        return toPlainResult({ error });
    }
}

/**
 * Get tenant by ID
 */
export async function getTenantByIdAction(tenantId: string) {
    await configureAuthenticatedClient();

    try {
        const result = await getApiTenantsById({
            path: { id: tenantId },
        });

        return toPlainResult(result);
    } catch (error) {
        return toPlainResult({ error });
    }
}

/**
 * Search tenants with filters
 */
export async function searchTenantsAction(params?: Omit<GetApiTenantsSearchData, 'url'>) {
    await configureAuthenticatedClient();

    try {
        const result = await getApiTenantsSearch({
            query: params?.query,
        });

        return toPlainResult(result);
    } catch (error) {
        return toPlainResult({ error });
    }
}
