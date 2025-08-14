import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { CoursesListClient } from './courses-list.client';
import type { Program } from "@/lib/api/generated/types.gen"
import { getPrograms } from "@/lib/content-management/programs/programs.actions"
import React from 'react';

export default async function CoursesPage(): Promise<React.JSX.Element> {
  let courses: Program[] = [];
  
  try {
    const response = await getPrograms();
    courses = response.data || [];
  } catch (error) {
    console.error('Failed to load courses:', error);
    // Keep empty array for graceful degradation
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Courses</DashboardPageTitle>
        <DashboardPageDescription>Manage your educational content</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <CoursesListClient initialCourses={courses} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
