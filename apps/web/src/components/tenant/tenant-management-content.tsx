'use client';

import type { Tenant } from '@/lib/api/generated/types.gen';
import { TenantsList } from './tenants-list';

interface TenantManagementContentProps {
    tenants: Tenant[]
}

export function TenantManagementContent({ tenants }: TenantManagementContentProps) {
    console.log('TenantManagementContent received tenants:', tenants.length);

    return (
        <TenantsList tenants={tenants} />
    );
}
