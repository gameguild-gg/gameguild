import React, { PropsWithChildren } from 'react';
import { notFound } from 'next/navigation';
import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
} from '@/components/dashboard/common/ui/dashboard-page';

import { PropsWithSlugParams } from '@/types';
import { CourseEditorProvider, getCourseBySlug } from '@/components/courses/editor';

export default async function Layout({ children, params }: PropsWithChildren<PropsWithSlugParams>): Promise<React.JSX.Element> {
  const { slug } = await params;

  const course = await getCourseBySlug(slug);

  if (!course) notFound();

  return (
    <>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Course Name</DashboardPageTitle>
          <DashboardPageDescription></DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <CourseEditorProvider course={course}>
            {/*TODO: Add a layout to edit the course here. it may contain a sidebar for navigation. */}
            {children}
          </CourseEditorProvider>
        </DashboardPageContent>
      </DashboardPage>
    </>
  );
}
