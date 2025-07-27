import React, { Suspense } from 'react';
import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
  DashboardOverviewContent,
  DashboardOverviewLoading,
  DashboardEnhancedLoading,
  DashboardEnhancedContent,
} from '@/components/dashboard';

interface PageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

export default async function Page({ searchParams }: PageProps): Promise<React.JSX.Element> {
  const params = await searchParams;

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Platform Overview</DashboardPageTitle>
        <DashboardPageDescription>Comprehensive view of your platform&apos;s performance and metrics</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <Suspense fallback={<DashboardEnhancedLoading />}>
          <DashboardEnhancedContent searchParams={params} />
        </Suspense>
        <Suspense fallback={<DashboardOverviewLoading />}>
          <DashboardOverviewContent searchParams={params} />
        </Suspense>
      </DashboardPageContent>
    </DashboardPage>
  );
}
