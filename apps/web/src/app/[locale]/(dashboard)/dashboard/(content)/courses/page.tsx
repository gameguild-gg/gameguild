import React from 'react';
import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
} from '@/components/dashboard/common/ui/dashboard-page';
import { CourseList } from '@/components/courses/common/ui/course-list';
import { getCourses } from '@/lib/courses/actions';

export default async function Page(): Promise<React.JSX.Element> {
  const courses = await getCourses();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Courses</DashboardPageTitle>
        <DashboardPageDescription>Manage your courses</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <CourseList courses={courses} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
