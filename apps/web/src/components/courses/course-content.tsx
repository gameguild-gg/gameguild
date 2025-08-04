'use client';

import React from 'react';
import { CourseProvider, useCourseContext } from '@/lib/courses';
import { CourseErrorBoundary } from './course-error-boundary';
import { CourseFilters } from './course-filters';
import { CourseGrid } from './course-grid';
import { EnhancedCourse } from '@/lib/courses/courses-enhanced.context';

interface CourseContentProps {
  initialData: { courses: EnhancedCourse[]; total: number };
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

          <CourseFilters />
          <CourseGridEnhanced />
        </div>
      </CourseProvider>
    </CourseErrorBoundary>
  );
}

// Enhanced CourseGrid that uses the context
function CourseGridEnhanced() {
  const { state, paginatedCourses } = useCourseContext();

  // Convert EnhancedCourse to Course for the grid component
  const coursesForGrid = paginatedCourses.map((course: EnhancedCourse) => ({
    id: course.id,
    title: course.title,
    description: course.description,
    category: course.area,
    level: course.level === 1 ? ('Beginner' as const) : course.level === 2 ? ('Intermediate' as const) : course.level === 3 ? ('Advanced' as const) : ('Advanced' as const),
    duration: `${course.estimatedHours || 0}h`,
    enrolledStudents: course.enrollmentCount || 0,
    rating: course.analytics?.averageRating || 0,
    price: 0, // You may want to add pricing to EnhancedCourse
    image: course.image || '/placeholder-course.jpg',
    slug: course.slug,
    instructor: {
      name: course.instructors?.[0] || 'Unknown',
      avatar: '/placeholder-avatar.jpg',
    },
    isEnrolled: false,
    progress: course.progress || 0,
    certification: false,
  }));

  return <CourseGrid courses={coursesForGrid} loading={state.isLoading} />;
}
