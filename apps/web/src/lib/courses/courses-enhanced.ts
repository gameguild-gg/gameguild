'use server';

import { revalidateTag, unstable_cache } from 'next/cache';
import { Course, CourseArea, CourseLevel } from '@/types/courses';

// Enhanced course types
export interface CourseAnalytics {
  enrollments: number;
  completions: number;
  averageRating: number;
  revenue: number;
}

export interface CourseContent {
  id: string;
  title: string;
  type: 'lesson' | 'quiz' | 'assignment' | 'project';
  duration: number;
  isRequired: boolean;
  order: number;
}

export interface EnhancedCourse extends Course {
  analytics?: CourseAnalytics;
  content?: CourseContent[];
  instructors?: string[];
  tags?: string[];
  publishedAt?: string;
  updatedAt?: string;
  status?: 'draft' | 'published' | 'archived';
  enrollmentCount?: number;
  estimatedHours?: number;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
  area: CourseArea;
  level: CourseLevel;
  tools: string[];
  image?: string;
  slug: string;
  estimatedHours?: number;
  instructors?: string[];
  tags?: string[];
  status?: 'draft' | 'published';
}

export interface UpdateCourseRequest {
  title?: string;
  description?: string;
  area?: CourseArea;
  level?: CourseLevel;
  tools?: string[];
  image?: string;
  estimatedHours?: number;
  instructors?: string[];
  tags?: string[];
  status?: 'draft' | 'published' | 'archived';
}

export interface CourseFilters {
  area?: CourseArea | 'all';
  level?: CourseLevel | 'all';
  status?: 'draft' | 'published' | 'archived' | 'all';
  instructor?: string;
  tag?: string;
  search?: string;
}

// Cache configuration
const CACHE_TAGS = {
  COURSES: 'courses',
  COURSE_DETAIL: 'course-detail',
  COURSE_ANALYTICS: 'course-analytics',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Enhanced course data fetcher with caching
 */
const getCachedEnhancedCourseData = unstable_cache(
  async (filters?: CourseFilters): Promise<{ courses: EnhancedCourse[]; total: number }> => {
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

      // Build query parameters
      const params = new URLSearchParams();
      if (filters?.area && filters.area !== 'all') params.set('area', filters.area);
      if (filters?.level && filters.level !== 'all') params.set('level', filters.level.toString());
      if (filters?.status && filters.status !== 'all') params.set('status', filters.status);
      if (filters?.instructor) params.set('instructor', filters.instructor);
      if (filters?.tag) params.set('tag', filters.tag);
      if (filters?.search) params.set('search', filters.search);

      const response = await fetch(`${apiUrl}/api/program/published?${params}`, {
        headers: {
          'Content-Type': 'application/json',
        },
        next: {
          revalidate: REVALIDATION_TIME,
          tags: [CACHE_TAGS.COURSES],
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch courses: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();

      // Transform API response to enhanced courses
      const courses: EnhancedCourse[] = Array.isArray(data) ? data.map(transformApiCourse) : [];

      return {
        courses,
        total: courses.length,
      };
    } catch (error) {
      console.error('Error fetching enhanced courses:', error);

      if (error instanceof TypeError && error.message.includes('fetch')) {
        return { courses: [], total: 0 };
      }

      throw new Error(error instanceof Error ? error.message : 'Failed to fetch courses');
    }
  },
  ['enhanced-courses-data'],
  {
    revalidate: REVALIDATION_TIME,
    tags: [CACHE_TAGS.COURSES],
  },
);

/**
 * Transform API course to enhanced course
 */
function transformApiCourse(apiCourse: any): EnhancedCourse {
  return {
    id: apiCourse.id,
    title: apiCourse.title,
    description: apiCourse.description,
    area: mapCategoryToArea(apiCourse.category || 0),
    level: mapDifficultyToLevel(apiCourse.difficulty || 0),
    tools: apiCourse.tools || [],
    image: sanitizeImagePath(apiCourse.thumbnail),
    slug: apiCourse.slug,
    progress: undefined,
    estimatedHours: apiCourse.estimatedHours || 0,
    instructors: apiCourse.instructors || [],
    tags: apiCourse.tags || [],
    publishedAt: apiCourse.publishedAt,
    updatedAt: apiCourse.updatedAt,
    status: apiCourse.status || 'published',
    enrollmentCount: apiCourse.enrollmentCount || 0,
  };
}

function sanitizeImagePath(thumbnail: string | null | undefined): string {
  if (!thumbnail) return '/placeholder.svg';
  if (thumbnail.startsWith('/images/')) return '/placeholder.svg';
  return thumbnail;
}

function mapCategoryToArea(category: number): CourseArea {
  switch (category) {
    case 0:
      return 'programming';
    case 4:
      return 'programming';
    case 10:
      return 'design';
    case 15:
      return 'art';
    case 1:
    case 5:
      return 'programming';
    case 2:
    case 3:
      return 'programming';
    default:
      return 'programming';
  }
}

function mapDifficultyToLevel(difficulty: number): CourseLevel {
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
 * Get enhanced course data with filters
 */
export async function getEnhancedCourseData(filters?: CourseFilters): Promise<{ courses: EnhancedCourse[]; total: number }> {
  return getCachedEnhancedCourseData(filters);
}

/**
 * Get course by ID with enhanced data
 */
export async function getEnhancedCourseById(id: string): Promise<EnhancedCourse | null> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program/${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.COURSE_DETAIL, `course-${id}`],
      },
    });

    if (!response.ok) {
      if (response.status === 404) return null;
      throw new Error(`Failed to fetch course: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    return transformApiCourse(data);
  } catch (error) {
    console.error('Error fetching course:', error);
    if (error instanceof TypeError && error.message.includes('fetch')) {
      return null;
    }
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch course');
  }
}

/**
 * Create course (Server Action)
 */
export async function createEnhancedCourse(prevState: any, formData: FormData): Promise<{ success: boolean; error?: string; course?: EnhancedCourse }> {
  try {
    const title = formData.get('title') as string;
    const description = formData.get('description') as string;
    const area = formData.get('area') as CourseArea;
    const level = parseInt(formData.get('level') as string) as CourseLevel;
    const slug = formData.get('slug') as string;
    const estimatedHours = parseInt(formData.get('estimatedHours') as string) || 0;
    const status = (formData.get('status') as string) || 'draft';

    // Parse tools, instructors, and tags from JSON strings or comma-separated values
    const toolsInput = formData.get('tools') as string;
    const instructorsInput = formData.get('instructors') as string;
    const tagsInput = formData.get('tags') as string;

    const tools = toolsInput
      ? toolsInput
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean)
      : [];
    const instructors = instructorsInput
      ? instructorsInput
          .split(',')
          .map((i) => i.trim())
          .filter(Boolean)
      : [];
    const tags = tagsInput
      ? tagsInput
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean)
      : [];

    // Validation
    if (!title || title.trim().length < 3) {
      return { success: false, error: 'Title must be at least 3 characters long' };
    }

    if (!description || description.trim().length < 10) {
      return { success: false, error: 'Description must be at least 10 characters long' };
    }

    if (!slug || !/^[a-z0-9-]+$/.test(slug)) {
      return { success: false, error: 'Slug must contain only lowercase letters, numbers, and hyphens' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        title: title.trim(),
        description: description.trim(),
        category: mapAreaToCategory(area),
        difficulty: mapLevelToDifficulty(level),
        slug: slug.trim(),
        estimatedHours,
        tools,
        instructors,
        tags,
        status,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to create course: ${response.status} ${response.statusText}`);
    }

    const courseData = await response.json();
    const course = transformApiCourse(courseData);

    // Revalidate caches
    await revalidateEnhancedCourseData();

    return { success: true, course };
  } catch (error) {
    console.error('Error creating course:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to create course',
    };
  }
}

/**
 * Update course (Server Action)
 */
export async function updateEnhancedCourse(
  id: string,
  prevState: any,
  formData: FormData,
): Promise<{ success: boolean; error?: string; course?: EnhancedCourse }> {
  try {
    const title = formData.get('title') as string;
    const description = formData.get('description') as string;
    const area = formData.get('area') as CourseArea;
    const level = parseInt(formData.get('level') as string) as CourseLevel;
    const estimatedHours = parseInt(formData.get('estimatedHours') as string) || 0;
    const status = formData.get('status') as string;

    const toolsInput = formData.get('tools') as string;
    const instructorsInput = formData.get('instructors') as string;
    const tagsInput = formData.get('tags') as string;

    const tools = toolsInput
      ? toolsInput
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean)
      : [];
    const instructors = instructorsInput
      ? instructorsInput
          .split(',')
          .map((i) => i.trim())
          .filter(Boolean)
      : [];
    const tags = tagsInput
      ? tagsInput
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean)
      : [];

    // Validation
    if (title && title.trim().length < 3) {
      return { success: false, error: 'Title must be at least 3 characters long' };
    }

    if (description && description.trim().length < 10) {
      return { success: false, error: 'Description must be at least 10 characters long' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const updateData: any = {};
    if (title) updateData.title = title.trim();
    if (description) updateData.description = description.trim();
    if (area) updateData.category = mapAreaToCategory(area);
    if (level) updateData.difficulty = mapLevelToDifficulty(level);
    if (estimatedHours) updateData.estimatedHours = estimatedHours;
    if (status) updateData.status = status;
    if (tools.length > 0) updateData.tools = tools;
    if (instructors.length > 0) updateData.instructors = instructors;
    if (tags.length > 0) updateData.tags = tags;

    const response = await fetch(`${apiUrl}/api/program/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updateData),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to update course: ${response.status} ${response.statusText}`);
    }

    const courseData = await response.json();
    const course = transformApiCourse(courseData);

    // Revalidate caches
    await revalidateEnhancedCourseData();
    revalidateTag(`course-${id}`);

    return { success: true, course };
  } catch (error) {
    console.error('Error updating course:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to update course',
    };
  }
}

/**
 * Delete course (Server Action)
 */
export async function deleteEnhancedCourse(id: string): Promise<{ success: boolean; error?: string }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to delete course: ${response.status} ${response.statusText}`);
    }

    // Revalidate caches
    await revalidateEnhancedCourseData();
    revalidateTag(`course-${id}`);

    return { success: true };
  } catch (error) {
    console.error('Error deleting course:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to delete course',
    };
  }
}

/**
 * Publish/Unpublish course (Server Action)
 */
export async function toggleCoursePublishStatus(id: string, currentStatus: string): Promise<{ success: boolean; error?: string; course?: EnhancedCourse }> {
  try {
    const newStatus = currentStatus === 'published' ? 'draft' : 'published';
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program/${id}/${newStatus === 'published' ? 'publish' : 'unpublish'}`, {
      method: 'POST',
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(
        errorData?.message || `Failed to ${newStatus === 'published' ? 'publish' : 'unpublish'} course: ${response.status} ${response.statusText}`,
      );
    }

    const courseData = await response.json();
    const course = transformApiCourse(courseData);

    // Revalidate caches
    await revalidateEnhancedCourseData();
    revalidateTag(`course-${id}`);

    return { success: true, course };
  } catch (error) {
    console.error('Error toggling course publish status:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to toggle course status',
    };
  }
}

/**
 * Get course analytics
 */
export async function getCourseAnalytics(id: string): Promise<CourseAnalytics | null> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program/${id}/analytics`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: 3600, // Cache analytics for 1 hour
        tags: [CACHE_TAGS.COURSE_ANALYTICS, `course-analytics-${id}`],
      },
    });

    if (!response.ok) {
      if (response.status === 404) return null;
      throw new Error(`Failed to fetch course analytics: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching course analytics:', error);
    return null;
  }
}

/**
 * Revalidate course data cache
 */
export async function revalidateEnhancedCourseData(): Promise<void> {
  revalidateTag(CACHE_TAGS.COURSES);
  revalidateTag(CACHE_TAGS.COURSE_DETAIL);
  revalidateTag(CACHE_TAGS.COURSE_ANALYTICS);
}

/**
 * Duplicate course (Server Action)
 */
export async function duplicateCourse(id: string, newTitle?: string): Promise<{ success: boolean; error?: string; course?: EnhancedCourse }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program/${id}/clone`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        title: newTitle,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to duplicate course: ${response.status} ${response.statusText}`);
    }

    const courseData = await response.json();
    const course = transformApiCourse(courseData);

    // Revalidate caches
    await revalidateEnhancedCourseData();

    return { success: true, course };
  } catch (error) {
    console.error('Error duplicating course:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to duplicate course',
    };
  }
}

// Helper functions
function mapAreaToCategory(area: CourseArea): number {
  switch (area) {
    case 'programming':
      return 0;
    case 'design':
      return 10;
    case 'art':
      return 15;
    case 'audio':
      return 1;
    default:
      return 0;
  }
}

function mapLevelToDifficulty(level: CourseLevel): number {
  switch (level) {
    case 1:
      return 0;
    case 2:
      return 1;
    case 3:
      return 2;
    case 4:
      return 3;
    default:
      return 0;
  }
}
