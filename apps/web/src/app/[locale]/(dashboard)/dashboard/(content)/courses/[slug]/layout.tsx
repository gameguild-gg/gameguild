import React, { PropsWithChildren } from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { PropsWithSlugParams } from '@/types';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';

export default async function Layout({ children, params }: PropsWithChildren<PropsWithSlugParams>): Promise<React.JSX.Element> {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const title = result.success && result.data ? result.data.title : slug;

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Course: {title}</DashboardPageTitle>
        <DashboardPageDescription>Manage course content and settings</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>{children}</DashboardPageContent>
    </DashboardPage>
  );
}
