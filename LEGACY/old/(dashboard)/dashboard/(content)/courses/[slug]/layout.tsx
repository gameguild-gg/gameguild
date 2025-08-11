import React, { PropsWithChildren } from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

import { PropsWithSlugParams } from '@/types';
import { CourseEditorProvider } from '@/components/courses/editor';
import { SidebarInset } from '@/components/ui/sidebar';

export default async function Layout({ children, params }: PropsWithChildren<PropsWithSlugParams>): Promise<React.JSX.Element> {
  const { slug } = await params;
  //
  // const course = await getCourseBySlug(slug);
  //
  // if (!course) notFound();

  return (
    <>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Course Name</DashboardPageTitle>
          <DashboardPageDescription></DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          {/*<CourseEditorProvider course={course}>*/}
          {/*<SidebarProvider>*/}
          {/*  <CourseEditorSidebar />*/}
          {/*<SidebarInset>{children}</SidebarInset>*/}
          {/*</SidebarProvider>*/}
          {/*</CourseEditorProvider>*/}
        </DashboardPageContent>
      </DashboardPage>
    </>
  );
}
