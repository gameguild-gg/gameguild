'use client';

import React from 'react';
import { useCourseContext } from '@/lib/courses';
import { CourseGrid } from '@/components/courses/course-grid';
import { EnhancedCourse } from '@/lib/courses/courses-enhanced.context';

// Enhanced CourseGrid that uses the context
export function CourseGridEnhanced() {
  const { state, paginatedCourses } = useCourseContext();
  
  console.log('CourseGridEnhanced - state:', {
    isLoading: state.isLoading,
    coursesCount: state.courses.length,
    filteredCoursesCount: state.filteredCourses.length,
    paginatedCoursesCount: paginatedCourses.length,
    filters: state.filters,
  });
  
  // Convert EnhancedCourse to Course for the grid component
  const coursesForGrid = paginatedCourses.map((course: EnhancedCourse) => ({
    id: course.id,
    title: course.title,
    description: course.description,
    category: course.area,
    level:
      course.level === 1
        ? ('Beginner' as const)
        : course.level === 2
          ? ('Intermediate' as const)
          : course.level === 3
            ? ('Advanced' as const)
            : ('Advanced' as const),
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

  console.log('CourseGridEnhanced - coursesForGrid:', coursesForGrid.length, coursesForGrid.slice(0, 2));
  console.log('CourseGridEnhanced - sample course slugs:', paginatedCourses.slice(0, 3).map(c => ({ title: c.title, slug: c.slug })));

  return <CourseGrid courses={coursesForGrid} loading={state.isLoading} />;
}
