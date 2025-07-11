'use client';

import React from 'react';
import { CourseProvider } from '@/lib/courses/course-context.tsx';
import { CourseErrorBoundary } from './course-error-boundary';
import { CourseFilters } from './course-filters';
import { CourseGrid } from './course-grid';
import { CourseData } from '@/types/courses';

interface CourseContentProps {
  initialData: CourseData;
}

export function CourseContent({ initialData }: CourseContentProps) {
  return (
    <CourseErrorBoundary>
      <CourseProvider initialData={initialData}>
        <div className="container mx-auto px-4 py-8">
          <div className="text-center mb-12">
            <h1 className="text-4xl font-bold mb-4 text-primary">Explore Game Development Courses</h1>
            <p className="text-xl text-muted-foreground">Discover and master essential skills and concepts in game development.</p>
          </div>

          <CourseFilters />
          <CourseGrid />
        </div>
      </CourseProvider>
    </CourseErrorBoundary>
  );
}
