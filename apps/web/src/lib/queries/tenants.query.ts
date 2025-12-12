import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';

export type ModulesTenantsCreateTenantDomainDto = any;
export type ModulesTenantsCreateTenantDto = any;
export type ModulesTenantsCreateTenantUserGroupDto = any;
export type ModulesTenantsTenant = any;
export type ModulesTenantsTenantDomain = any;
export type ModulesTenantsTenantUserGroup = any;
export type ModulesTenantsUpdateTenantDomainDto = any;
export type ModulesTenantsUpdateTenantDto = any;

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

async function fetchTenants(_params: TenantListParams = {}): Promise<ModulesTenantsTenant[]> {
    throw new Error('Not implemented (STUB): fetchTenants');
}

async function fetchTenantById(_id: string): Promise<ModulesTenantsTenant> {
    throw new Error('Not implemented (STUB): fetchTenantById');
}

async function fetchTenantStatistics(): Promise<TenantStatsData> {
    throw new Error('Not implemented (STUB): fetchTenantStatistics');
}

async function fetchTenantDomains(_tenantId?: string): Promise<ModulesTenantsTenantDomain[]> {
    throw new Error('Not implemented (STUB): fetchTenantDomains');
}

async function fetchTenantUserGroups(_tenantId?: string): Promise<ModulesTenantsTenantUserGroup[]> {
    throw new Error('Not implemented (STUB): fetchTenantUserGroups');
}

async function createTenant(_tenantData: ModulesTenantsCreateTenantDto): Promise<ModulesTenantsTenant> {
    throw new Error('Not implemented (STUB): createTenant');
}

async function updateTenant(_id: string, _tenantData: ModulesTenantsUpdateTenantDto): Promise<ModulesTenantsTenant> {
    throw new Error('Not implemented (STUB): updateTenant');
}

async function deleteTenant(_id: string, _softDelete: boolean = true): Promise<void> {
    throw new Error('Not implemented (STUB): deleteTenant');
}

async function createTenantDomain(_domainData: ModulesTenantsCreateTenantDomainDto): Promise<ModulesTenantsTenantDomain> {
    throw new Error('Not implemented (STUB): createTenantDomain');
}

async function updateTenantDomain(_id: string, _domainData: ModulesTenantsUpdateTenantDomainDto): Promise<ModulesTenantsTenantDomain> {
    throw new Error('Not implemented (STUB): updateTenantDomain');
}

async function deleteTenantDomain(_id: string): Promise<void> {
    throw new Error('Not implemented (STUB): deleteTenantDomain');
}

async function createTenantUserGroup(_groupData: ModulesTenantsCreateTenantUserGroupDto): Promise<ModulesTenantsTenantUserGroup> {
    throw new Error('Not implemented (STUB): createTenantUserGroup');
}

export const tenantQueries = {
    all: () => ['tenants'] as const,

    lists: () => [...tenantQueries.all(), 'list'] as const,
    list: (params: TenantListParams = {}) =>
        queryOptions({
            queryKey: [...tenantQueries.lists(), params] as const,
            queryFn: () => fetchTenants(params),
            staleTime: 2 * 60 * 1000,
            gcTime: 5 * 60 * 1000,
        }),

    details: () => [...tenantQueries.all(), 'detail'] as const,
    detail: (id: string) =>
        queryOptions({
            queryKey: [...tenantQueries.details(), id] as const,
            queryFn: () => fetchTenantById(id),
            staleTime: 5 * 60 * 1000,
            gcTime: 10 * 60 * 1000,
        }),

    stats: () =>
        queryOptions({
            queryKey: [...tenantQueries.all(), 'stats'] as const,
            queryFn: fetchTenantStatistics,
            staleTime: 5 * 60 * 1000,
            gcTime: 10 * 60 * 1000,
        }),

    domains: () => [...tenantQueries.all(), 'domains'] as const,
    domainsList: (tenantId?: string) =>
        queryOptions({
            queryKey: [...tenantQueries.domains(), tenantId || 'all'] as const,
            queryFn: () => fetchTenantDomains(tenantId),
            staleTime: 3 * 60 * 1000,
            gcTime: 10 * 60 * 1000,
        }),

    userGroups: () => [...tenantQueries.all(), 'user-groups'] as const,
    userGroupsList: (tenantId?: string) =>
        queryOptions({
            queryKey: [...tenantQueries.userGroups(), tenantId || 'all'] as const,
            queryFn: () => fetchTenantUserGroups(tenantId),
            staleTime: 3 * 60 * 1000,
            gcTime: 10 * 60 * 1000,
        }),
} as const;

export function useCreateTenant() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createTenant,
        onSuccess: () => {
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
        onSuccess: (updatedTenant: any) => {
            if (updatedTenant?.id) {
                queryClient.setQueryData(
                    tenantQueries.detail(updatedTenant.id).queryKey,
                    updatedTenant
                );
            }
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
        onSuccess: (_: any, { id }: { id: string }) => {
            queryClient.removeQueries({ queryKey: tenantQueries.detail(id).queryKey });
            queryClient.invalidateQueries({ queryKey: tenantQueries.lists() });
            queryClient.invalidateQueries({ queryKey: tenantQueries.stats().queryKey });
        },
    });
}

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

export function useCreateTenantUserGroup() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createTenantUserGroup,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tenantQueries.userGroups() });
        },
    });
}
