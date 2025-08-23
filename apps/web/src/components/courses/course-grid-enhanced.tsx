'use client';

import { CourseGrid } from './course-grid';
import { Program, ProgramDifficulty, EnrollmentStatus } from '@/lib/api/generated';
import { useCourseContext } from '@/lib/courses';

// Transform Program to CourseGridCourse format
function transformProgramToCourse(program: Program, index: number) {
  return {
    id: program.id || `course-${index + 1}`, // Use original program ID as string
    title: program.title,
    description: program.description || '',
    category: String(program.category || 'General'),
    level: (program.difficulty === ProgramDifficulty.BEGINNER ? 'Beginner' : 
           program.difficulty === ProgramDifficulty.INTERMEDIATE ? 'Intermediate' : 'Advanced') as 'Beginner' | 'Intermediate' | 'Advanced',
    duration: `${program.estimatedHours || 0}`,
    enrolledStudents: program.currentEnrollments || 0,
    rating: program.averageRating || 0,
    price: 0, // No pricing info in Program type
    image: program.thumbnail || '',
    slug: program.slug || program.id || '',
    instructor: {
      name: 'Instructor', // No instructor info in Program type
      avatar: ''
    },
    isEnrolled: program.enrollmentStatus === EnrollmentStatus.ACTIVE || false,
    progress: 0,
    certification: false
  };
}

export function CourseGridEnhanced() {
  const { state, paginatedCourses } = useCourseContext();
  const transformedCourses = paginatedCourses.map((program, index) => transformProgramToCourse(program, index));
  return <CourseGrid courses={transformedCourses} loading={state.isLoading} />;
}
