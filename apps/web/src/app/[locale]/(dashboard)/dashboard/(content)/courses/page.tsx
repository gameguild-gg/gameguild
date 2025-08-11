import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { CourseListWrapper } from '@/components/courses/common';
import { getPrograms as getCourses } from '@/lib/content-management/programs/programs.actions';

export default async function Page(): Promise<React.JSX.Element> {
  try {
    const response = await getCourses();
    // Extract only the serializable data and ensure it's a plain object
    const rawCourses = response.data || [];
    // Serialize and deserialize to ensure we have plain objects
    const courses = JSON.parse(JSON.stringify(rawCourses));

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
  } catch (error) {
    console.error('Error loading courses:', error);
    return (
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Courses</DashboardPageTitle>
          <DashboardPageDescription>Manage your courses</DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <CourseListWrapper courses={[]} />
        </DashboardPageContent>
      </DashboardPage>
    );
  }
}
