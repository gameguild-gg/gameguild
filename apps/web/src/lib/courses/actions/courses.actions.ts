'use server';

import { CourseArea, CourseLevel, ToolsByArea } from '@/components/legacy/types/courses';
import { EnhancedCourse } from '@/lib/courses/courses-enhanced.context';

// Enhanced course data interface
export interface EnhancedCourseData {
  courses: EnhancedCourse[];
  toolsByArea: ToolsByArea;
}

// Helper function to sanitize image paths
function sanitizeImagePath(thumbnail: string | null | undefined): string {
  if (!thumbnail) return '/placeholder.svg';

  // If the thumbnail path starts with /images/ but the image doesn't exist, use placeholder
  if (thumbnail.startsWith('/images/')) {
    return '/placeholder.svg';
  }

  return thumbnail;
}

// Map API program category enum to our course area
function mapCategoryToArea(category: number): CourseArea {
  // Based on the ProgramCategory enum in the API
  switch (category) {
    case 0: // Programming
      return 'programming';
    case 4: // GameDevelopment
      return 'programming';
    case 10: // Design
      return 'design';
    case 15: // CreativeArts
      return 'art';
    case 1: // DataScience
    case 5: // AI
      return 'programming';
    case 2: // WebDevelopment
    case 3: // MobileDevelopment
      return 'programming';
    default:
      return 'programming'; // Default fallback
  }
}

// Map API difficulty enum to our course level
function mapDifficultyToLevel(difficulty: number): CourseLevel {
  // Based on the API response, difficulty appears to be an enum value (0, 1, 2, 3)
  switch (difficulty) {
    case 0:
      return 1; // Beginner
    case 1:
      return 2; // Intermediate
    case 2:
      return 3; // Advanced
    case 3:
      return 4; // Arcane
    default:
      return 1; // Default to Beginner
  }
}

// Transform API program object to our EnhancedCourse interface
function transformProgramToCourse(program: {
  id: string;
  title: string;
  description: string;
  category: number;
  difficulty: number;
  thumbnail?: string;
  slug: string;
}): EnhancedCourse {
  return {
    id: program.id,
    title: program.title,
    description: program.description,
    area: mapCategoryToArea(program.category),
    level: mapDifficultyToLevel(program.difficulty),
    tools: [], // Empty for now, could be populated from program data later
    image: sanitizeImagePath(program.thumbnail),
    slug: program.slug,
    progress: undefined, // Not provided by API
    // Add the enhanced course properties with defaults
    status: 'published', // Since we're fetching from published programs
    publishedAt: new Date().toISOString(), // Default to current date
    updatedAt: new Date().toISOString(),
    enrollmentCount: 0,
    estimatedHours: 10, // Default estimate
    instructors: [], // Could be populated from program data later
    tags: [], // Could be populated from program data later
    analytics: {
      enrollments: 0,
      completions: 0,
      averageRating: 0,
      revenue: 0,
    },
  };
}

export async function getCourseData(): Promise<EnhancedCourseData> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    // Fetch courses from the public published programs API
    const response = await fetch(`${apiUrl}/api/program/published`, {
      headers: {
        'Content-Type': 'application/json',
      },
      // Enable caching for better performance
      next: {
        revalidate: 300, // Revalidate every 5 minutes
        tags: ['course-data'],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch course data: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    console.log('Raw API data length:', data.length);

    // The API returns a flat array of programs, not an object with courses property
    if (!Array.isArray(data)) {
      throw new Error('Invalid course data structure - expected array');
    }

    // Transform the API response - these are all learning tracks/courses
    console.log(
      'Programs from API:',
      data.map((p: { title?: string; slug?: string }) => ({ title: p.title, slug: p.slug })),
    );

    const transformedCourses = data.map(transformProgramToCourse);
    console.log('Transformed courses length:', transformedCourses.length);

    return {
      courses: transformedCourses,
      toolsByArea: {
        programming: [],
        art: [],
        design: [],
        audio: [],
      }, // Empty for now, can be populated later if needed
    };
  } catch (error) {
    console.error('Error in getCourseData:', error);
    // Return empty data structure for network errors instead of throwing
    if (error instanceof TypeError && error.message.includes('fetch')) {
      console.error('Network error fetching course data, returning empty data');
      return {
        courses: [],
        toolsByArea: {
          programming: [],
          art: [],
          design: [],
          audio: [],
        },
      };
    }
    // Still throw for unexpected errors that should bubble up
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch course data');
  }
}

export async function revalidateCourseData(): Promise<void> {
  const { revalidateTag } = await import('next/cache');
  revalidateTag('course-data');
}

export async function getCourseBySlug(slug: string): Promise<EnhancedCourse | null> {
  try {
    // Get all published courses and find the one with the matching slug
    const courseData = await getCourseData();
    const course = courseData.courses.find((c) => c.slug === slug);

    if (!course) {
      console.log(`Course not found with slug: ${slug}`);
      return null;
    }

    // Return the course from the published list (no need for additional API call)
    return course;
  } catch (error) {
    console.error('Error in getCourseBySlug:', error);
    // Return null for network errors or API failures instead of throwing
    if (error instanceof TypeError && error.message.includes('fetch')) {
      console.error('Network error fetching course:', slug);
      return null;
    }
    // Still throw for unexpected errors that should bubble up
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch course');
  }
}

export async function getCourseById(id: string): Promise<EnhancedCourse | null> {
  try {
    // Get all published courses and find the one with the matching ID
    const courseData = await getCourseData();
    const course = courseData.courses.find((c) => c.id === id);

    if (!course) {
      console.log(`Course not found with ID: ${id}`);
      return null;
    }

    return course;
  } catch (error) {
    console.error('Error in getCourseById:', error);
    // Return null for network errors instead of throwing
    if (error instanceof TypeError && error.message.includes('fetch')) {
      console.error('Network error fetching course:', id);
      return null;
    }
    // Still throw for unexpected errors that should bubble up
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch course');
  }
}

export interface CreateCourseRequest {
  title: string;
  description: string;
  area: string;
  level: number;
  tools: string[];
  image?: string;
  slug: string;
  durationHours?: number;
}

export async function createCourse(courseData: CreateCourseRequest): Promise<EnhancedCourse> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/program`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(courseData),
    });

    if (!response.ok) {
      throw new Error(`Failed to create course: ${response.status} ${response.statusText}`);
    }

    const createdCourse = await response.json();

    // Revalidate the course data after creation
    await revalidateCourseData();

    return createdCourse;
  } catch (error) {
    console.error('Error in createCourse:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create course');
  }
}
