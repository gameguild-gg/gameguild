'use server';

import { getAllPrograms } from '@/data/courses/mock-data';
import { Program } from '@/lib/api/generated';

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
  return getAllPrograms();
}

export async function getCourseData(): Promise<Program[]> {
  return getCourses();
}

export async function getCourseBySlug(slug: string): Promise<Program | null> {
  const programs = await getCourses();
  return programs.find(program => program.slug === slug) || null;
}
