import { TenantManagementContent } from '@/components/tenant/management/tenant-management-content';
import { Tenant } from '@/components/tenant/types';
import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default async function Page(): Promise<React.JSX.Element> {
  const tenants: Tenant[] = [];

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Reports</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TenantManagementContent initialTenants={tenants} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
