'use server';

import { getCourses as getCoursesService } from '@/lib/courses/services/course.service';
import { Course } from '@/lib/courses/course-enhanced.types';
import { Course as LegacyCourse } from '@/components/legacy/types/course-enhanced';

/**
 * Transform legacy course to the new course format
 */
function transformLegacyCourse(legacyCourse: LegacyCourse): Course {
  return {
    id: legacyCourse.id,
    title: legacyCourse.title,
    description: legacyCourse.description,
    area: legacyCourse.area,
    level: legacyCourse.level,
    tools: [...legacyCourse.tools], // Convert readonly to mutable
    image: legacyCourse.image,
    slug: legacyCourse.slug,
    progress: legacyCourse.progress,
    // Add optional properties with defaults
    status: 'published',
    estimatedHours: 10,
    analytics: {
      enrollments: 0,
      completions: 0,
      averageRating: 0,
      revenue: 0,
      viewCount: 0,
      completionRate: 0,
    },
  };
}

/**
 * Get courses for server components
 * This function wraps the service and handles the Result type unwrapping
 */
export async function getCourses(): Promise<Course[]> {
  try {
    const result = await getCoursesService();
    
    if (!result.success) {
      console.error('Failed to fetch courses:', result.error);
      return [];
    }
    
    return result.data.map(transformLegacyCourse);
  } catch (error) {
    console.error('Error in getCourses server action:', error);
    return [];
  }
}
