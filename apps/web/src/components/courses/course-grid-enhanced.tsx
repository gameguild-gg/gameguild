'use client';

import { CourseGrid } from '@/components/courses/course-grid';
import { Program } from '@/lib/api/generated';
import { useCourseContext } from '@/lib/courses';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';

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

  // Convert Program to Course for the grid component
  const coursesForGrid = paginatedCourses.map((course: Program) => {
    const levelConfig = getCourseLevelConfig(course.difficulty || 0);
    const categoryName = getCourseCategoryName(course.category || 0);

    return {
      id: course.id || '0',
      title: course.title || '',
      description: course.description || '',
      category: categoryName,
      level: course.difficulty === 0 ? ('Beginner' as const) :
        course.difficulty === 1 ? ('Intermediate' as const) :
          course.difficulty === 2 ? ('Advanced' as const) : ('Advanced' as const),
      duration: `${course.estimatedHours || 0}h`,
      enrolledStudents: course.currentEnrollments || 0,
      rating: course.averageRating || 0,
      price: 0, // Free courses
      image: course.thumbnail || 'https://placehold.co/400x225/1f2937/ffffff?text=Course+Image',
      slug: course.slug || '',
      instructor: {
        name: course.programUsers?.[0]?.user?.name || 'Unknown',
        avatar: 'https://placehold.co/40x40/6b7280/ffffff?text=U',
      },
      isEnrolled: false,
      progress: 0, // Default progress
      certification: false,
    };
  });

  console.log('CourseGridEnhanced - coursesForGrid:', coursesForGrid.length, coursesForGrid.slice(0, 2));
  console.log(
    'CourseGridEnhanced - sample course slugs:',
    paginatedCourses.slice(0, 3).map((c) => ({
      title: c.title,
      slug: c.slug,
    })),
  );

  return <CourseGrid courses={coursesForGrid} loading={state.isLoading} />;
}
