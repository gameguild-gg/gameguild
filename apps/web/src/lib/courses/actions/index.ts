'use server';

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
  type: 'video' | 'text' | 'quiz' | 'exercise' | 'assignment';
  duration?: number;
  order: number;
  content?: string;
  videoUrl?: string;
  isCompleted?: boolean;
  isLocked?: boolean;
}

/**
 * Get a course by its slug
 */
export async function getCourseBySlug(slug: string): Promise<Course | null> {
  try {
    // Mock implementation - replace with actual API call
    console.log(`Getting course by slug: ${slug}`);
    
    // Mock course data
    const mockCourse: Course = {
      id: 1,
      title: 'Introduction to Game Development',
      description: 'Learn the fundamentals of game development using modern tools and techniques. This comprehensive course covers everything from basic concepts to advanced implementation.',
      slug: slug,
      category: 'Programming',
      level: 1,
      estimatedHours: 40,
      image: '/placeholder-course.jpg',
      instructors: ['John Doe', 'Jane Smith'],
      analytics: {
        averageRating: 4.5,
        totalReviews: 123,
      },
      enrollment: {
        isEnrolled: false,
        progress: 0,
      },
      modules: [
        {
          id: 1,
          title: 'Getting Started',
          description: 'Introduction to game development concepts and setting up your development environment.',
          order: 1,
          isCompleted: false,
          lessons: [
            {
              id: 1,
              title: 'Welcome to Game Development',
              description: "An overview of what we'll cover in this course.",
              type: 'video',
              duration: 10,
              order: 1,
              videoUrl: '/videos/welcome.mp4',
              isCompleted: false,
              isLocked: false,
            },
            {
              id: 2,
              title: 'Setting Up Your Development Environment',
              description: "Install and configure the tools you'll need for game development.",
              type: 'text',
              duration: 15,
              order: 2,
              content: 'Step-by-step guide to setting up your development environment...',
              isCompleted: false,
              isLocked: false,
            },
            {
              id: 3,
              title: 'Knowledge Check',
              description: 'Test your understanding of the basics.',
              type: 'quiz',
              duration: 5,
              order: 3,
              isCompleted: false,
              isLocked: false,
            },
          ],
        },
        {
          id: 2,
          title: 'Core Concepts',
          description: 'Learn the fundamental concepts of game development.',
          order: 2,
          isCompleted: false,
          lessons: [
            {
              id: 4,
              title: 'Game Loops and State Management',
              description: 'Understanding the core game loop and how to manage game state.',
              type: 'video',
              duration: 25,
              order: 1,
              videoUrl: '/videos/game-loops.mp4',
              isCompleted: false,
              isLocked: true,
            },
            {
              id: 5,
              title: 'Entity Component Systems',
              description: 'Learn about modern game architecture patterns.',
              type: 'video',
              duration: 30,
              order: 2,
              videoUrl: '/videos/ecs.mp4',
              isCompleted: false,
              isLocked: true,
            },
          ],
        },
      ],
    };
    
    return mockCourse;
  } catch (error) {
    console.error('Error getting course by slug:', error);
    return null;
  }
}

/**
 * Get all courses
 */
export async function getCourses(): Promise<Course[]> {
  try {
    // Mock implementation - replace with actual API call
    console.log('Getting all courses');
    
    const mockCourses: Course[] = [
      {
        id: 1,
        title: 'Introduction to Game Development',
        description: 'Learn the fundamentals of game development.',
        slug: 'introduction-to-game-development',
        category: 'Programming',
        level: 1,
        estimatedHours: 40,
        instructors: ['John Doe'],
        modules: [],
        analytics: { averageRating: 4.5, totalReviews: 123 },
      },
      {
        id: 2,
        title: 'Advanced 3D Modeling',
        description: 'Master advanced 3D modeling techniques.',
        slug: 'advanced-3d-modeling',
        category: 'Art',
        level: 3,
        estimatedHours: 60,
        instructors: ['Jane Smith'],
        modules: [],
        analytics: { averageRating: 4.8, totalReviews: 87 },
      },
    ];
    
    return mockCourses;
  } catch (error) {
    console.error('Error getting courses:', error);
    return [];
  }
}

// Alias for backward compatibility
export const getCourseData = getCourses;
