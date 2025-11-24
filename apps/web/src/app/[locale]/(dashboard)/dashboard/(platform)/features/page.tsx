import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { getQueryClient } from '@/components/get-query-client';
// import { FeatureFlagsManagementContent } from '@/components/features/feature-flags-management-content';
// import { featureFlagQueries } from '@/lib/queries/feature-flags.query';
import { dehydrate, HydrationBoundary } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { Suspense } from 'react';

export default async function FeatureFlagsPage(): Promise<React.JSX.Element> {
  const queryClient = getQueryClient();

  // Prefetch feature flags data on the server
  // await queryClient.prefetchQuery(featureFlagQueries.list());

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Feature Flags</DashboardPageTitle>
          <DashboardPageDescription>
            Manage feature flags, toggles, and gradual rollouts across your platform
          </DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                <span>Loading feature flags...</span>
              </div>
            }
          >
            {/* TODO: FeatureFlagsManagementContent component not found */}
            <div className="p-4 text-muted-foreground">Feature flags management coming soon...</div>
          </Suspense>
        </DashboardPageContent>
      </DashboardPage>
    </HydrationBoundary>
  );
}
