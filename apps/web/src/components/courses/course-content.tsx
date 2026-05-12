'use client';

import { Program } from '@/lib/api/generated';
import { CourseProvider } from '@/lib/courses';
import { CourseErrorBoundary } from './course-error-boundary';
import { CourseGridEnhanced } from './course-grid-enhanced';

interface CourseContentProps {
  initialData: { courses: Program[]; total: number };
}

export function CourseContent({ initialData }: CourseContentProps) {
  return (
    <CourseErrorBoundary>
      <CourseProvider initialCourses={initialData.courses}>
        <div className="container mx-auto px-4 py-8">
          <div className="text-center mb-12">
            <h1 className="text-4xl font-bold mb-4 text-primary">Explore Game Development Courses</h1>
            <p className="text-xl text-muted-foreground">Discover and master essential skills and concepts in game development.</p>
          </div>

          {/* <CourseFilter categories={[]} selected="All" /> */}
          <CourseGridEnhanced />
        </div>
      </CourseProvider>
    </CourseErrorBoundary>
  );
}
