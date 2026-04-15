import { CourseErrorBoundary } from '@/components/courses/course-error-boundary';
import { CourseGridEnhanced } from '@/components/courses/course-grid-enhanced';
import { CoursePageError } from '@/components/courses/course-page-error';
import { CourseProvider } from '@/lib/courses';
import { getCourseData } from '@/lib/courses/actions/index';
import React from 'react';

export default async function CourseCatalogPage(): Promise<React.JSX.Element> {
  try {
    const courses = await getCourseData();

    return (
      <CourseProvider initialCourses={courses}>
        <CourseErrorBoundary>
          <div className="container mx-auto px-4 py-8">
            <h1 className="text-3xl font-bold mb-8">Courses</h1>
            <CourseGridEnhanced />
          </div>
        </CourseErrorBoundary>
      </CourseProvider>
    );
  } catch (err) {
    console.error('Error loading courses:', err);
    return <CoursePageError message={err instanceof Error ? err.message : 'Failed to load courses'} />;
  }
}
