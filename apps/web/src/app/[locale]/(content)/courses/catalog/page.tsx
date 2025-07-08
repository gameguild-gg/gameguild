import { Suspense } from 'react';
import { getCourseData } from '@/actions/courses';
import { CourseProvider } from '@/contexts/course-context';
import { CourseFilters } from '@/components/courses/course-filters';
import { CourseGrid } from '@/components/courses/course-grid';
import { CourseStates } from '@/components/courses/course-states';
import { CourseErrorBoundary } from '@/components/courses/course-error-boundary';
import { CoursePageError } from '@/components/courses/course-page-error';

export const dynamic = 'force-dynamic';

export default async function CoursesPage() {
  try {
    const courseData = await getCourseData();
    
    return (
      <CourseErrorBoundary>
        <CourseProvider initialData={courseData}>
          <div className="container mx-auto px-4 py-8">
            <div className="text-center mb-12">
              <h1 className="text-4xl font-bold mb-4 text-primary">Course Catalog</h1>
              <p className="text-xl text-muted-foreground">
                Browse and filter through all available game development courses to find the perfect learning path for you.
              </p>
            </div>

            <Suspense fallback={<CourseStates.Loading />}>
              <CourseFilters />
              <CourseGrid />
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
