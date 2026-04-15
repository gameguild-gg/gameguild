'use client';

import { Program } from '@/lib/api/generated';
import { useCourseContext } from '@/lib/courses';
import { CourseGrid } from './course-grid';

// ProgramDifficulty enum values (from backend)
const ProgramDifficulty = {
  Beginner: 0,
  Intermediate: 1,
  Advanced: 2,
  Expert: 3,
} as const;

// EnrollmentStatus enum values (from backend)
const EnrollmentStatus = {
  Pending: 0,
  Active: 1,
  Completed: 2,
  Cancelled: 3,
  Expired: 4,
} as const;

// Transform Program to CourseGridCourse format
function transformProgramToCourse(program: Program, index: number) {
  const thumbnail = program.thumbnail || '/default-course-thumbnail.jpg';
  return {
    id: program.id || `course-${index + 1}`, // Use original program ID as string
    title: program.title,
    description: program.description || '',
    category: String(program.category || 'General'),
    level: (program.difficulty === ProgramDifficulty.Beginner ? 'Beginner' :
      program.difficulty === ProgramDifficulty.Intermediate ? 'Intermediate' : 'Advanced') as 'Beginner' | 'Intermediate' | 'Advanced',
    duration: Number(program.estimatedHours || 0), // Changed to number to match CourseGridCourse
    enrolledStudents: program.currentEnrollments || 0,
    rating: program.averageRating || 0,
    price: 0, // No pricing info in Program type
    image: thumbnail,
    thumbnailUrl: thumbnail,
    coverUrl: thumbnail,
    slug: program.slug || program.id || '',
    instructor: {
      name: 'Instructor', // No instructor info in Program type
      avatar: ''
    },
    isEnrolled: program.enrollmentStatus === EnrollmentStatus.Active || false,
    progress: 0,
    certification: false
  };
}

export function CourseGridEnhanced() {
  const { state, paginatedCourses } = useCourseContext();
  const transformedCourses = paginatedCourses.map((program, index) => transformProgramToCourse(program, index));
  return <CourseGrid courses={transformedCourses} loading={state.isLoading} />;
}
