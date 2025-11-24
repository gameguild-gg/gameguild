import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { getQueryClient } from '@/components/get-query-client';
import { TestingLabOverview } from '@/components/testing-lab/overview/testing-lab-overview';
import { testingLabQueries } from '@/lib/queries/testing-lab.query';
import { dehydrate, HydrationBoundary } from '@tanstack/react-query';

export default async function TestingLabPage() {
  const queryClient = getQueryClient();

  // Determine user role - in a real app, this would come from auth
  const userRole: 'student' | 'professor' | 'admin' = 'student';

  // Prefetch data on the server
  await Promise.all([
    queryClient.prefetchQuery(testingLabQueries.stats(userRole)),
    queryClient.prefetchQuery(testingLabQueries.sessions()),
    queryClient.prefetchQuery(testingLabQueries.requests()),
  ]);

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Testing Lab</DashboardPageTitle>
          <DashboardPageDescription>
            Manage testing sessions, review feedback, and track testing progress
          </DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <TestingLabOverview userRole={userRole} />
        </DashboardPageContent>
      </DashboardPage>
    </HydrationBoundary>
  );
}
