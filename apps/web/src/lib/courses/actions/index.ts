'use server';

import { Program } from '@/lib/api/generated';
import { courseService } from '@/lib/courses/services/course.service';

export interface Course {
  id: number;
  title: string;
  description: string;
  slug: string;
  category: string;
  level: number;
  estimatedHours: number;
  image?: string;
  instructors: string[];
  modules: CourseModule[];
  analytics?: {
    averageRating: number;
    totalReviews: number;
  };
  enrollment?: {
    isEnrolled: boolean;
    progress: number;
    enrollmentDate?: string;
  };
}

export interface CourseModule {
  id: number;
  title: string;
  description: string;
  order: number;
  lessons: CourseLesson[];
  isCompleted?: boolean;
}

export interface CourseLesson {
  id: number;
  title: string;
  description: string;
  duration: number;
  isCompleted?: boolean;
}

export async function getCourses(): Promise<Program[]> {
  return courseService.getCourses();
}

export async function getCourseData(): Promise<Program[]> {
  return getCourses();
}

export async function getCourseBySlug(slug: string): Promise<Program | null> {
  const result = await courseService.getCourseBySlug(slug);
  return result.success ? result.data ?? null : null;
}
