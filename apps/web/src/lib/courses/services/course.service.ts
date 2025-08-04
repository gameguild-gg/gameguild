import { Course } from '../types';
import { getCourseBySlugAction, getCoursesAction } from './course.actions';

export interface CourseService {
  getCourseBySlug(slug: string): Promise<Course | null>;
  getCourses(): Promise<Course[]>;
}

export interface CourseLevelConfig {
  name: string;
  color: string;
  bgColor: string;
}

export const courseService: CourseService = {
  async getCourseBySlug(slug: string) {
    return getCourseBySlugAction(slug);
  },

  async getCourses() {
    return getCoursesAction();
  },
};

// Named exports for direct import
export const getCourseBySlug = courseService.getCourseBySlug;
export const getCourses = courseService.getCourses;

export function getCourseLevelConfig(level: number | string): CourseLevelConfig {
  const levelNum = typeof level === 'string' ? parseInt(level) : level;

  const configs: Record<number, CourseLevelConfig> = {
    1: {
      name: 'Beginner',
      color: 'text-green-400',
      bgColor: 'bg-green-500/10 border-green-500',
    },
    2: {
      name: 'Intermediate',
      color: 'text-blue-400',
      bgColor: 'bg-blue-500/10 border-blue-500',
    },
    3: {
      name: 'Advanced',
      color: 'text-orange-400',
      bgColor: 'bg-orange-500/10 border-orange-500',
    },
    4: {
      name: 'Expert',
      color: 'text-red-400',
      bgColor: 'bg-red-500/10 border-red-500',
    },
  };

  return configs[levelNum] || configs[1]!;
}
