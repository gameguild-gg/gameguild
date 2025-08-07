import { CourseProvider } from '@/lib/courses';
import { getCourseData } from '@/lib/courses/actions';
import { Suspense } from 'react';

import { CourseErrorBoundary } from '@/components/courses/course-error-boundary';
import { CourseGridEnhanced } from '@/components/courses/course-grid-enhanced';
import { CoursePageError } from '@/components/courses/course-page-error';
import { CourseStates } from '@/components/courses/course-states';

export const dynamic = 'force-dynamic';

export default async function CoursesPage() {
  try {
    const courses = await getCourseData();
    console.log('CoursesPage - courses:', {
      coursesLength: courses.length,
      firstCourse: courses[0],
    });

    return (
      <CourseErrorBoundary>
        <CourseProvider initialCourses={courses}>
          <div className="container mx-auto px-4 py-8">
            <div className="text-center mb-12">
              <h1 className="text-4xl font-bold mb-4 text-primary">Course Catalog</h1>
              <p className="text-xl text-muted-foreground">Browse and filter through all available game development courses to find the perfect learning path for you.</p>
            </div>

            <Suspense fallback={<CourseStates.Loading />}>
              <CourseGridEnhanced />
            </Suspense>
          </div>
        </CourseProvider>
      </CourseErrorBoundary>
    );
  } catch (error) {
    console.error('Error loading courses:', error);
    return <CoursePageError message="Failed to load courses. Please try again later." />;
  }
}
