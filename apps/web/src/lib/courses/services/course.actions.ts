'use server';

import { Course } from '../types';
import { courseService, getCourseCategoryName, getCourseLevelConfig } from './course.service';

function mapLevel(difficulty: string | number | null | undefined): Course['level'] {
  const level = getCourseLevelConfig(difficulty).name;

  if (level === 'Intermediate' || level === 'Advanced') {
    return level;
  }

  return 'Beginner';
}

function mapProgramToCourse(program: Awaited<ReturnType<typeof courseService.getCourses>>[number]): Course {
  return {
    id: String(program.id ?? program.slug ?? ''),
    title: typeof program.title === 'string' && program.title.length > 0 ? program.title : 'Untitled course',
    description: typeof program.description === 'string' ? program.description : '',
    category: getCourseCategoryName(program.category as string | number | null | undefined),
    level: mapLevel(program.difficulty as string | number | null | undefined),
    duration: typeof program.estimatedHours === 'number' ? `${program.estimatedHours}h` : 'Self-paced',
    enrolledStudents: typeof program.currentEnrollments === 'number' ? program.currentEnrollments : 0,
    rating: typeof program.averageRating === 'number' ? program.averageRating : 0,
    price: 0,
    image: typeof program.thumbnail === 'string' ? program.thumbnail : '',
    slug: typeof program.slug === 'string' ? program.slug : String(program.id ?? ''),
    instructor: {
      name: 'GameGuild Faculty',
      avatar: '',
    },
    isEnrolled: false,
    progress: 0,
    certification: true,
  };
}

/**
 * Server action to get a course by its slug
 */
export async function getCourseBySlugAction(slug: string): Promise<Course | null> {
  try {
    const result = await courseService.getCourseBySlug(slug);

    if (!result.success || !result.data) {
      return null;
    }

    return mapProgramToCourse(result.data);
  } catch (error) {
    console.error('Error getting course by slug:', error);
    return null;
  }
}

/**
 * Server action to get all courses
 */
export async function getCoursesAction(): Promise<Course[]> {
  try {
    const courses = await courseService.getCourses();
    return courses.map(mapProgramToCourse);
  } catch (error) {
    console.error('Error getting courses:', error);
    return [];
  }
}
