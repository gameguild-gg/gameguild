'use client';

import React, { Suspense } from 'react';
import { CourseProvider } from '@/lib/courses';
import { getCourseData } from '@/lib/courses/actions/index';
import { CourseErrorBoundary } from '@/components/courses/course-error-boundary';
import { CourseGridEnhanced } from '@/components/courses/course-grid-enhanced';
import { CoursePageError } from '@/components/courses/course-page-error';
import { CourseStates } from '@/components/courses/course-states';
import { Program } from '@/lib/api/generated';

export default function CourseCatalogPage() {
  const [courses, setCourses] = React.useState<Program[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    async function loadCourses() {
      try {
        setLoading(true);
        const courseData = await getCourseData();
        setCourses(courseData);
      } catch (err) {
        console.error('Error loading courses:', err);
        setError(err instanceof Error ? err.message : 'Failed to load courses');
      } finally {
        setLoading(false);
      }
    }
    loadCourses();
  }, []);

  if (error) {
    return <CoursePageError message={error} />;
  }

  return (
    <CourseProvider initialCourses={courses}>
      <CourseErrorBoundary>
        <div className="container mx-auto px-4 py-8">
          <h1 className="text-3xl font-bold mb-8">Courses</h1>
          {loading ? (
            <CourseStates.Loading />
          ) : (
            <CourseGridEnhanced />
          )}
        </div>
      </CourseErrorBoundary>
    </CourseProvider>
  );
}
