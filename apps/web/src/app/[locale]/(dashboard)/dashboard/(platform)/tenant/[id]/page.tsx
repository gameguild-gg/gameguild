import { notFound } from 'next/navigation';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TenantDetailContent } from '@/components/tenant/detail/tenant-detail-content';
import { getTenantByIdAction } from '@/lib/admin/tenants/tenants.actions';
import type { Tenant } from '@/lib/api/generated/types.gen';
import React from 'react';

interface TenantDetailPageProps {
  params: {
    id: string;
  };
}

export default async function TenantDetailPage({ params }: TenantDetailPageProps): Promise<React.JSX.Element> {
  const { id } = params;

  // For now, we'll simulate admin permissions
  // In production, implement proper permission checking
  const isAdmin = true;

  // Load tenant data
  let tenant: Tenant | null = null;
  try {
    const result = await getTenantByIdAction({
      path: { id },
    });

    if (result.data) {
      tenant = result.data as Tenant;
    }
  } catch (error) {
    console.error('Failed to load tenant:', error);
  }

  if (!tenant) {
    notFound();
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>{tenant.name}</DashboardPageTitle>
        <DashboardPageDescription>Manage tenant details, users, and settings</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TenantDetailContent tenant={tenant} isAdmin={isAdmin} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
