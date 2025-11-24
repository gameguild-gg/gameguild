import {
    deleteApiTenantDomainsById,
    deleteApiTenantsById,
    getApiTenantDomains,
    getApiTenantDomainsUserGroups,
    getApiTenants,
    getApiTenantsActive,
    getApiTenantsById,
    getApiTenantsDeleted,
    getApiTenantsSearch,
    getApiTenantsStatistics,
    postApiTenantDomains,
    postApiTenantDomainsUserGroups,
    postApiTenants,
    putApiTenantDomainsById,
    putApiTenantsById,
} from '@/lib/api/generated/sdk.gen';
import type {
    ModulesTenantsCreateTenantDomainDto,
    ModulesTenantsCreateTenantDto,
    ModulesTenantsCreateTenantUserGroupDto,
    ModulesTenantsTenant,
    ModulesTenantsTenantDomain,
    ModulesTenantsTenantUserGroup,
    ModulesTenantsUpdateTenantDomainDto,
    ModulesTenantsUpdateTenantDto
} from '@/lib/api/generated/types.gen';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';

export interface TenantListParams {
    page?: number;
    limit?: number;
    search?: string;
    includeDeleted?: boolean;
    isActive?: boolean;
}

export interface TenantStatsData {
    totalTenants: number;
    activeTenants: number;
    deletedTenants: number;
    newTenantsThisMonth: number;
    domainsCount: number;
    userGroupsCount: number;
}

// Helper function to configure authenticated client
async function withAuth<T>(operation: () => Promise<T>): Promise<T> {
    const { configureAuthenticatedClient } = await import('@/lib/api/authenticated-client');
    await configureAuthenticatedClient();
    return operation();
}

// API fetching functions
async function fetchTenants(params: TenantListParams = {}): Promise<ModulesTenantsTenant[]> {
    return withAuth(async () => {
        const { search, includeDeleted = false, isActive } = params;

        if (search) {
            const result = await getApiTenantsSearch({
                query: { searchTerm: search },
            });
            return result.data || [];
        }

        if (includeDeleted) {
            const result = await getApiTenantsDeleted();
            return result.data || [];
        }

        if (isActive !== undefined) {
            if (isActive) {
                const result = await getApiTenantsActive();
                return result.data || [];
            } else {
                const result = await getApiTenantsDeleted();
                return result.data || [];
            }
        }

        const result = await getApiTenants();
        return result.data || [];
    });
}

async function fetchTenantById(id: string): Promise<ModulesTenantsTenant> {
    return withAuth(async () => {
        const result = await getApiTenantsById({
            path: { id },
        });

        if (!result.data) {
            throw new Error('Tenant not found');
        }

        return result.data;
    });
}

async function fetchTenantStatistics(): Promise<TenantStatsData> {
    return withAuth(async () => {
        const result = await getApiTenantsStatistics();

        // Transform API response to our expected format
        const stats = result.data as any;
        return {
            totalTenants: stats?.totalTenants || 0,
            activeTenants: stats?.activeTenants || 0,
            deletedTenants: stats?.deletedTenants || 0,
            newTenantsThisMonth: stats?.newTenantsThisMonth || 0,
            domainsCount: stats?.domainsCount || 0,
            userGroupsCount: stats?.userGroupsCount || 0,
        };
    });
}

async function fetchTenantDomains(tenantId?: string): Promise<ModulesTenantsTenantDomain[]> {
    return withAuth(async () => {
        const result = await getApiTenantDomains({
            query: tenantId ? { tenantId } : {},
        });
        return result.data || [];
    });
}

async function fetchTenantUserGroups(tenantId?: string): Promise<ModulesTenantsTenantUserGroup[]> {
    return withAuth(async () => {
        const result = await getApiTenantDomainsUserGroups({
            query: tenantId ? { tenantId } : {},
        });
        return result.data || [];
    });
}

async function createTenant(tenantData: ModulesTenantsCreateTenantDto): Promise<ModulesTenantsTenant> {
    return withAuth(async () => {
        const result = await postApiTenants({
            body: tenantData,
        });

        if (!result.data) {
            throw new Error('Failed to create tenant');
        }

        return result.data;
    });
}

async function updateTenant(id: string, tenantData: ModulesTenantsUpdateTenantDto): Promise<ModulesTenantsTenant> {
    return withAuth(async () => {
        const result = await putApiTenantsById({
            path: { id },
            body: tenantData,
        });

        if (!result.data) {
            throw new Error('Failed to update tenant');
        }

        return result.data;
    });
}

async function deleteTenant(id: string, softDelete: boolean = true): Promise<void> {
    return withAuth(async () => {
        await deleteApiTenantsById({
            path: { id },
            query: { softDelete },
        });
    });
}

async function createTenantDomain(domainData: ModulesTenantsCreateTenantDomainDto): Promise<ModulesTenantsTenantDomain> {
    return withAuth(async () => {
        const result = await postApiTenantDomains({
            body: domainData,
        });

        if (!result.data) {
            throw new Error('Failed to create tenant domain');
        }

        return result.data;
    });
}

async function updateTenantDomain(id: string, domainData: ModulesTenantsUpdateTenantDomainDto): Promise<ModulesTenantsTenantDomain> {
    return withAuth(async () => {
        const result = await putApiTenantDomainsById({
            path: { id },
            body: domainData,
        });

        if (!result.data) {
            throw new Error('Failed to update tenant domain');
        }

        return result.data;
    });
}

async function deleteTenantDomain(id: string): Promise<void> {
    return withAuth(async () => {
        await deleteApiTenantDomainsById({
            path: { id },
        });
    });
}

async function createTenantUserGroup(groupData: ModulesTenantsCreateTenantUserGroupDto): Promise<ModulesTenantsTenantUserGroup> {
    return withAuth(async () => {
        const result = await postApiTenantDomainsUserGroups({
            body: groupData,
        });

        if (!result.data) {
            throw new Error('Failed to create tenant user group');
        }

        return result.data;
    });
}

// Query options for React Query
export const tenantQueries = {
    all: () => ['tenants'] as const,

    lists: () => [...tenantQueries.all(), 'list'] as const,
    list: (params: TenantListParams = {}) =>
        queryOptions({
            queryKey: [...tenantQueries.lists(), params] as const,
            queryFn: () => fetchTenants(params),
            staleTime: 2 * 60 * 1000, // 2 minutes
            gcTime: 5 * 60 * 1000, // 5 minutes
        }),

    details: () => [...tenantQueries.all(), 'detail'] as const,
    detail: (id: string) =>
        queryOptions({
            queryKey: [...tenantQueries.details(), id] as const,
            queryFn: () => fetchTenantById(id),
            staleTime: 5 * 60 * 1000, // 5 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),

    stats: () =>
        queryOptions({
            queryKey: [...tenantQueries.all(), 'stats'] as const,
            queryFn: fetchTenantStatistics,
            staleTime: 5 * 60 * 1000, // 5 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),

    domains: () => [...tenantQueries.all(), 'domains'] as const,
    domainsList: (tenantId?: string) =>
        queryOptions({
            queryKey: [...tenantQueries.domains(), tenantId || 'all'] as const,
            queryFn: () => fetchTenantDomains(tenantId),
            staleTime: 3 * 60 * 1000, // 3 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),

    userGroups: () => [...tenantQueries.all(), 'user-groups'] as const,
    userGroupsList: (tenantId?: string) =>
        queryOptions({
            queryKey: [...tenantQueries.userGroups(), tenantId || 'all'] as const,
            queryFn: () => fetchTenantUserGroups(tenantId),
            staleTime: 3 * 60 * 1000, // 3 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),
} as const;

// Mutation hooks for tenant operations
export function useCreateTenant() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createTenant,
        onSuccess: () => {
            // Invalidate and refetch tenant lists
            queryClient.invalidateQueries({ queryKey: tenantQueries.lists() });
            queryClient.invalidateQueries({ queryKey: tenantQueries.stats().queryKey });
        },
    });
}

export function useUpdateTenant() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, data }: { id: string; data: ModulesTenantsUpdateTenantDto }) =>
            updateTenant(id, data),
        onSuccess: (updatedTenant) => {
            // Update the specific tenant in cache
            if (updatedTenant.id) {
                queryClient.setQueryData(
                    tenantQueries.detail(updatedTenant.id).queryKey,
                    updatedTenant
                );
            }

            // Invalidate lists to reflect changes
            queryClient.invalidateQueries({ queryKey: tenantQueries.lists() });
            queryClient.invalidateQueries({ queryKey: tenantQueries.stats().queryKey });
        },
    });
}

export function useDeleteTenant() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, softDelete = true }: { id: string; softDelete?: boolean }) =>
            deleteTenant(id, softDelete),
        onSuccess: (_, { id }) => {
            // Remove from cache
            queryClient.removeQueries({ queryKey: tenantQueries.detail(id).queryKey });

            // Invalidate lists to reflect changes
            queryClient.invalidateQueries({ queryKey: tenantQueries.lists() });
            queryClient.invalidateQueries({ queryKey: tenantQueries.stats().queryKey });
        },
    });
}

// Domain management mutations
export function useCreateTenantDomain() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createTenantDomain,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tenantQueries.domains() });
        },
    });
}

export function useUpdateTenantDomain() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, data }: { id: string; data: ModulesTenantsUpdateTenantDomainDto }) =>
            updateTenantDomain(id, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tenantQueries.domains() });
        },
    });
}

export function useDeleteTenantDomain() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: deleteTenantDomain,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tenantQueries.domains() });
        },
    });
}

// User group management mutations
export function useCreateTenantUserGroup() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createTenantUserGroup,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tenantQueries.userGroups() });
        },
    });
}