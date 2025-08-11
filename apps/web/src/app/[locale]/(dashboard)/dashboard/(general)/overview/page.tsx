import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Platform Overview</DashboardPageTitle>
        <DashboardPageDescription>Comprehensive view of your platform&apos;s performance and metrics</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {/*<Suspense fallback={<DashboardEnhancedLoading />}>*/}
        {/*  <DashboardEnhancedContent searchParams={params} />*/}
        {/*</Suspense>*/}
        {/*<Suspense fallback={<DashboardOverviewLoading />}>*/}
        {/*  <DashboardOverviewContent searchParams={params} />*/}
        {/*</Suspense>*/}
      </DashboardPageContent>
    </DashboardPage>
  );
}
