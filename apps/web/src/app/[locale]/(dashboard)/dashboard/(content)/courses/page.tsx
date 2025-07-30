import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { CourseListWrapper } from '@/components/courses/common';
import { getPrograms as getCourses } from '@/lib/content-management/programs/programs.actions';

export default async function Page(): Promise<React.JSX.Element> {
  const response = await getCourses();
  const courses = response.data || [];

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Courses</DashboardPageTitle>
        <DashboardPageDescription>Manage your courses</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <CourseListWrapper courses={courses} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
