import { Suspense } from 'react';
import { getCourseData } from '@/lib/courses/actions';
import { CourseProvider } from '@/lib/courses';

import { CourseStates } from '@/components/courses/course-states';
import { CourseErrorBoundary } from '@/components/courses/course-error-boundary';
import { CoursePageError } from '@/components/courses/course-page-error';
import { CourseGridEnhanced } from '@/components/courses/course-grid-enhanced';

export const dynamic = 'force-dynamic';

export default async function CoursesPage() {
  try {
    const courseData = await getCourseData();
    console.log('CoursesPage - courseData:', {
      coursesLength: courseData.courses.length,
      firstCourse: courseData.courses[0],
    });

    return (
      <CourseErrorBoundary>
        <CourseProvider initialCourses={courseData.courses}>
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
