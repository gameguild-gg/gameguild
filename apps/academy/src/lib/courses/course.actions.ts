'use server';

import coursesData from '@/data/courses.json';

export interface Chapter {
  id: string;
  title: string;
  image: string;
  coverImage: string;
  description: string;
  duration: string;
  progress?: number;
}

export interface Course {
  name: string;
  slug: string;
  chapters: Chapter[];
}

interface CoursesFileShape {
  courses: Course[];
}

// Temporary server action to fetch all courses from the static JSON file.
// Replace with real data source (DB / API) when available.
export async function getCourses(): Promise<Course[]> {
  const data = coursesData as CoursesFileShape;
  return data.courses;
}

// Optional helper to fetch a single course by slug (may be handy for quick tests)
export async function getCourseBySlug(slug: string): Promise<Course | undefined> {
  const data = coursesData as CoursesFileShape;
  return data.courses.find((c) => c.slug === slug);
}

// Fetch all chapters from all courses
export async function getAllChapters(): Promise<Chapter[]> {
  const data = coursesData as CoursesFileShape;
  return data.courses.flatMap((course) => course.chapters);
}

// Fetch chapters by course slug
export async function getChaptersByCourse(courseSlug: string): Promise<Chapter[]> {
  const data = coursesData as CoursesFileShape;
  const course = data.courses.find((c) => c.slug === courseSlug);
  return course?.chapters || [];
}

// Keep file small & focused; expand with mutations once persistence layer exists.
