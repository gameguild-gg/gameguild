import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TenantsList } from '@/components/tenant/tenants-list';
import { getTenantsAction } from '@/lib/admin/tenants/tenants.actions';
import type { Tenant } from '@/lib/api/generated/types.gen';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  // Load tenants data
  let tenants: Tenant[] = [];
  try {
    const result = await getTenantsAction();
    if (result.data) {
      tenants = Array.isArray(result.data) ? result.data : [];
    }
  } catch (error) {
    console.error('Failed to load tenants:', error);
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Tenant Management</DashboardPageTitle>
        <DashboardPageDescription>Manage tenants and organizations in the system</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TenantsList initialTenants={tenants} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
