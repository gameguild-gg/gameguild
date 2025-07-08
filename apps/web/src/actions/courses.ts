'use server';

import { CourseData, Course, CourseArea, CourseLevel } from '@/types/courses';

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

// Transform API program object to our Course interface
function transformProgramToCourse(program: {
  id: string;
  title: string;
  description: string;
  category: number;
  difficulty: number;
  thumbnail?: string;
  slug: string;
}): Course {
  return {
    id: program.id,
    title: program.title,
    description: program.description,
    area: mapCategoryToArea(program.category),
    level: mapDifficultyToLevel(program.difficulty),
    tools: [], // Empty for now, could be populated from program data later
    image: program.thumbnail || '/images/courses/default.jpg',
    slug: program.slug,
    progress: undefined, // Not provided by API
  };
}

export async function getCourseData(): Promise<CourseData> {
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
    
    // The API returns a flat array of programs, not an object with courses property
    if (!Array.isArray(data)) {
      throw new Error('Invalid course data structure - expected array');
    }

    // Transform the API response to match our expected format
    return {
      courses: data.map(transformProgramToCourse),
      toolsByArea: {
        programming: [],
        art: [],
        design: [],
        audio: [],
      }, // Empty for now, can be populated later if needed
    };
  } catch (error) {
    console.error('Error in getCourseData:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch course data');
  }
}

export async function revalidateCourseData(): Promise<void> {
  const { revalidateTag } = await import('next/cache');
  revalidateTag('course-data');
}

export async function getCourseBySlug(slug: string): Promise<CourseData['courses'][0] | null> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    
    const response = await fetch(`${apiUrl}/api/program/slug/${slug}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: 300, // Revalidate every 5 minutes
        tags: ['course-data'],
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch course: ${response.status} ${response.statusText}`);
    }

    const course = await response.json();
    return course;
  } catch (error) {
    console.error('Error in getCourseBySlug:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch course');
  }
}

export async function getCourseById(id: string): Promise<CourseData['courses'][0] | null> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    
    const response = await fetch(`${apiUrl}/api/program/${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: 300, // Revalidate every 5 minutes
        tags: ['course-data'],
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch course: ${response.status} ${response.statusText}`);
    }

    const course = await response.json();
    return course;
  } catch (error) {
    console.error('Error in getCourseById:', error);
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

export async function createCourse(courseData: CreateCourseRequest): Promise<CourseData['courses'][0]> {
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
