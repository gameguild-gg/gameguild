import React, { PropsWithChildren } from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';

import { PropsWithSlugParams } from '@/types';

export default async function Layout({ children, params }: PropsWithChildren<PropsWithSlugParams>): Promise<React.JSX.Element> {
  const { slug } = await params;

  // TODO: Fetch course data when needed
  // const course = await getCourseBySlug(slug);
  // if (!course) notFound();

  return (
    <>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Course: {slug}</DashboardPageTitle>
          <DashboardPageDescription>Manage course content and settings</DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>{children}</DashboardPageContent>
      </DashboardPage>
    </>
  );
}
