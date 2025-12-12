import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { getQueryClient } from '@/components/get-query-client';
import { TenantManagementContent } from '@/components/tenant/tenant-management-content';
import { tenantQueries } from '@/lib/queries/tenants.query';
import { dehydrate, HydrationBoundary } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { Suspense } from 'react';

export default async function TenantManagementPage(): Promise<React.JSX.Element> {
  const queryClient = getQueryClient();

  // Prefetch tenant data on the server
  await Promise.all([
    queryClient.prefetchQuery(tenantQueries.list()),
    queryClient.prefetchQuery(tenantQueries.stats()),
    queryClient.prefetchQuery(tenantQueries.domainsList()),
  ]);

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Tenant Management</DashboardPageTitle>
          <DashboardPageDescription>
            Manage tenants, domains, and organizations in the system
          </DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                <span>Loading tenant data...</span>
              </div>
            }
          >
            <TenantManagementContent />
          </Suspense>
        </DashboardPageContent>
      </DashboardPage>
    </HydrationBoundary>
  );
}
