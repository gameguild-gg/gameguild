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
  const fallbackId = `course-${index + 1}`;
  const thumbnail = typeof program.thumbnail === 'string' && program.thumbnail.length > 0
    ? program.thumbnail
    : '/default-course-thumbnail.jpg';
  const title = typeof program.title === 'string' && program.title.length > 0
    ? program.title
    : `Course ${index + 1}`;
  const description = typeof program.description === 'string' ? program.description : '';
  const difficulty = Number(program.difficulty ?? ProgramDifficulty.Beginner);
  const rating = typeof program.averageRating === 'number' ? program.averageRating : 0;
  const enrollmentStatus = Number(program.enrollmentStatus ?? EnrollmentStatus.Pending);
  const slug = typeof program.slug === 'string' && program.slug.length > 0
    ? program.slug
    : String(program.id ?? fallbackId);

  return {
    id: String(program.id ?? fallbackId),
    title,
    description,
    category: String(program.category ?? 'General'),
    level: (difficulty === ProgramDifficulty.Beginner
      ? 'Beginner'
      : difficulty === ProgramDifficulty.Intermediate
        ? 'Intermediate'
        : 'Advanced') as 'Beginner' | 'Intermediate' | 'Advanced',
    duration: Number(program.estimatedHours ?? 0),
    enrolledStudents: Number(program.currentEnrollments ?? 0),
    rating,
    price: 0,
    image: thumbnail,
    thumbnailUrl: thumbnail,
    coverUrl: thumbnail,
    slug,
    instructor: {
      name: 'Instructor',
      avatar: ''
    },
    isEnrolled: enrollmentStatus === EnrollmentStatus.Active,
    progress: 0,
    certification: false
  };
}

export function CourseGridEnhanced() {
  const { state, paginatedCourses } = useCourseContext();
  const transformedCourses = paginatedCourses.map((program, index) => transformProgramToCourse(program, index));
  return <CourseGrid courses={transformedCourses} loading={state.isLoading} />;
}
