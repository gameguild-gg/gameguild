import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TenantManagementContent } from '@/components/tenant/tenant-management-content';
import { getTenantsAction } from '@/lib/admin/tenants/tenants.actions';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { Loader2 } from 'lucide-react';
import { Suspense } from 'react';

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
        <Suspense
          fallback={
            <div className="flex items-center justify-center p-4">
              <Loader2 className="h-4 w-4 animate-spin" />
            </div>
          }
        >
          <TenantManagementContent tenants={tenants} />
        </Suspense>
      </DashboardPageContent>
    </DashboardPage>
  );
}
