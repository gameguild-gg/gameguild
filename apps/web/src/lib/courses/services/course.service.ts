import { cache } from 'react';
import { unstable_cache } from 'next/cache';
import type { Course, CourseId, Result } from '@/types/course-enhanced.ts';
import { COURSE_LEVELS } from '@/types/course-enhanced.ts';

// Cache configuration
const CACHE_TAGS = {
  COURSES: 'courses',
  COURSE_DETAIL: 'course-detail',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get course level configuration
 */
export function getCourseLevelConfig(level: keyof typeof COURSE_LEVELS) {
  return COURSE_LEVELS[level] ?? COURSE_LEVELS[1];
}

/**
 * Cached course data fetcher using React cache and Next.js unstable_cache
 */
const getCachedCourseData = unstable_cache(
  async (): Promise<Result<readonly Course[], Error>> => {
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

      const response = await fetch(`${apiUrl}/api/program/published`, {
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch course data: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();

      if (!Array.isArray(data)) {
        throw new Error('Invalid course data structure - expected array');
      }

      // Transform and filter data
      const courses: Course[] = data
        .filter((program) => !(program.slug?.includes('-track-') || program.title?.includes('Track')))
        .map(transformProgramToCourse);

      return { success: true, data: courses };
    } catch (error) {
      console.error('Error fetching course data:', error);
      return {
        success: false,
        error: error instanceof Error ? error : new Error('Unknown error occurred'),
      };
    }
  },
  ['course-data'],
  {
    revalidate: REVALIDATION_TIME,
    tags: [CACHE_TAGS.COURSES],
  },
);

/**
 * API Program interface
 */
interface ApiProgram {
  id: string;
  title: string;
  description: string;
  category: number;
  difficulty: number;
  thumbnail?: string;
  slug: string;
  tools?: string[];
  progress?: number;
}

/**
 * Transform API program object to Course interface
 */
function transformProgramToCourse(program: ApiProgram): Course {
  return {
    id: program.id as CourseId,
    title: program.title,
    description: program.description,
    area: mapCategoryToArea(program.category),
    level: mapDifficultyToLevel(program.difficulty),
    tools: Object.freeze(program.tools || []),
    image: sanitizeImagePath(program.thumbnail),
    slug: program.slug,
    progress: program.progress,
  } as const;
}

/**
 * Map API category enum to course area
 */
function mapCategoryToArea(category: number): Course['area'] {
  switch (category) {
    case 0: // Programming
    case 4: // GameDevelopment
    case 1: // DataScience
    case 5: // AI
    case 2: // WebDevelopment
    case 3: // MobileDevelopment
      return 'programming';
    case 10: // Design
      return 'design';
    case 15: // CreativeArts
      return 'art';
    default:
      return 'programming';
  }
}

/**
 * Map API difficulty enum to course level
 */
function mapDifficultyToLevel(difficulty: number): Course['level'] {
  switch (difficulty) {
    case 0:
      return 1;
    case 1:
      return 2;
    case 2:
      return 3;
    case 3:
      return 4;
    default:
      return 1;
  }
}

/**
 * Sanitize image paths
 */
function sanitizeImagePath(thumbnail: string | null | undefined): string {
  if (!thumbnail || thumbnail.startsWith('/images/')) {
    return '/placeholder.svg';
  }
  return thumbnail;
}

/**
 * Get all courses using React cache pattern
 */
export const getCourses = cache(async (): Promise<Result<readonly Course[], Error>> => {
  return getCachedCourseData();
});

/**
 * Get course by slug with enhanced error handling
 */
export const getCourseBySlug = cache(async (slug: string): Promise<Result<Course, Error>> => {
  const coursesResult = await getCourses();

  if (!coursesResult.success) {
    return coursesResult;
  }

  const course = coursesResult.data.find((c) => c.slug === slug);

  if (!course) {
    return {
      success: false,
      error: new Error(`Course not found with slug: ${slug}`),
    };
  }

  return { success: true, data: course };
});

/**
 * Get course by ID with enhanced error handling
 */
export const getCourseById = cache(async (id: CourseId): Promise<Result<Course, Error>> => {
  const coursesResult = await getCourses();

  if (!coursesResult.success) {
    return coursesResult;
  }

  const course = coursesResult.data.find((c) => c.id === id);

  if (!course) {
    return {
      success: false,
      error: new Error(`Course not found with ID: ${id}`),
    };
  }

  return { success: true, data: course };
});

/**
 * Revalidate course data cache
 */
export async function revalidateCourseCache(): Promise<void> {
  const { revalidateTag } = await import('next/cache');
  revalidateTag(CACHE_TAGS.COURSES);
}
