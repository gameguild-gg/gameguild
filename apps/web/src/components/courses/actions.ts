'use server';

import {
  createCourse as createLearningCourse,
  deleteCourse as deleteLearningCourse,
  updateCourse as updateLearningCourse,
} from '@/lib/learning/actions';
import { courseService, getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import speakingurl from 'speakingurl';

export interface CourseLegacy {
  id: number;
  sourceId?: string;
  title: string;
  category: string;
  description: string;
  image: string;
  slug: string;
  level?: string;
  duration?: string;
  seatsLeft?: number;
}

function stableNumericId(value: unknown): number {
  const input = String(value ?? '');
  let hash = 0;

  for (let index = 0; index < input.length; index += 1) {
    hash = (hash * 31 + input.charCodeAt(index)) >>> 0;
  }

  return hash || Date.now();
}

function mapCourse(program: Awaited<ReturnType<typeof courseService.getCourses>>[number]): CourseLegacy {
  const sourceId = String(program.id ?? program.slug ?? '');
  const estimatedHours = typeof program.estimatedHours === 'number' ? program.estimatedHours : null;

  return {
    id: stableNumericId(sourceId),
    sourceId,
    title: typeof program.title === 'string' && program.title.length > 0 ? program.title : 'Untitled course',
    category: getCourseCategoryName(program.category as string | number | null | undefined),
    description: typeof program.description === 'string' ? program.description : '',
    image: typeof program.thumbnail === 'string' ? program.thumbnail : '',
    slug: typeof program.slug === 'string' && program.slug.length > 0 ? program.slug : speakingurl(sourceId),
    level: getCourseLevelConfig(program.difficulty as string | number | null | undefined).name,
    duration: estimatedHours ? `${estimatedHours} hours` : 'Self-paced',
    seatsLeft:
      typeof program.maxEnrollments === 'number' && typeof program.currentEnrollments === 'number'
        ? Math.max(program.maxEnrollments - program.currentEnrollments, 0)
        : undefined,
  };
}

export async function fetchCourses(): Promise<CourseLegacy[]> {
  const courses = await courseService.getCourses();
  return courses.map(mapCourse);
}

export async function getCourse(id: number): Promise<CourseLegacy | undefined> {
  const courses = await fetchCourses();
  return courses.find((course) => course.id === id);
}

export async function createCourse(course: Omit<CourseLegacy, 'id'>): Promise<CourseLegacy> {
  const result = await createLearningCourse({
    title: course.title,
    description: course.description,
    slug: course.slug || speakingurl(course.title),
  });

  if (!result.success) {
    throw new Error(result.error);
  }

  return {
    ...course,
    id: stableNumericId(result.data.id),
    sourceId: result.data.id,
    slug: course.slug || speakingurl(course.title),
  };
}

export async function updateCourse(id: number, updates: Partial<Omit<CourseLegacy, 'id'>>): Promise<CourseLegacy | undefined> {
  const existing = await getCourse(id);

  if (!existing?.sourceId) {
    return undefined;
  }

  const result = await updateLearningCourse({
    courseId: existing.sourceId,
    title: updates.title,
    description: updates.description,
    slug: updates.slug,
    thumbnail: updates.image,
    category: updates.category,
    difficulty: updates.level,
  });

  if (!result.success) {
    throw new Error(result.error);
  }

  return { ...existing, ...updates };
}

export async function deleteCourse(id: number): Promise<boolean> {
  const existing = await getCourse(id);

  if (!existing?.sourceId) {
    return false;
  }

  const result = await deleteLearningCourse(existing.sourceId);

  if (!result.success) {
    throw new Error(result.error);
  }

  return true;
}
