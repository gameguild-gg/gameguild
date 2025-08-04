import React from 'react';
import { CreateCourseForm } from '@/components/courses/forms/create-course-form';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default function CreateCoursePage(): React.JSX.Element {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Create New Course</DashboardPageTitle>
        <DashboardPageDescription>Create a new course to share your knowledge and skills with the community</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <CreateCourseForm />
      </DashboardPageContent>
    </DashboardPage>
  );
}
