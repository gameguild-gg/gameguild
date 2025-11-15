import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { getQueryClient } from '@/components/get-query-client';
import { ResourcesManagementContent } from '@/components/resources/resources-management-content';
import { resourceQueries } from '@/lib/queries/resources.query';
import { dehydrate, HydrationBoundary } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { Suspense } from 'react';

export default async function ResourcesPage(): Promise<React.JSX.Element> {
  const queryClient = getQueryClient();

  // Prefetch resources data on the server
  // Note: We'll need tenantId - for now we'll fetch in client component
  // await queryClient.prefetchQuery(resourceQueries.quotas(tenantId));

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Resource Management</DashboardPageTitle>
          <DashboardPageDescription>
            Monitor resource usage, quotas, and limits across your platform
          </DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                <span>Loading resources...</span>
              </div>
            }
          >
            <ResourcesManagementContent />
          </Suspense>
        </DashboardPageContent>
      </DashboardPage>
    </HydrationBoundary>
  );
}
