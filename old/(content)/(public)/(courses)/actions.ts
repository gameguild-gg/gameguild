'use server';

import { courses } from '@/data/courses';
import { Api } from '@/types/course-types';
import CourseEntity = Api.CourseEntity;

export async function getCourses(): Promise<CourseEntity[]> {
  return courses;
}

export async function getCourseBySlug(slug: string): Promise<CourseEntity | null> {
  return courses.find((course) => course.slug === slug) || null;
}
