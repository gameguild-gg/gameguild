import { EnrollmentStatus as APIEnrollmentStatus } from '@/lib/core/api/generated/types.gen';

export interface Course {
  id: string;
  title: string;
  description: string;
  category: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced';
  duration: string;
  enrolledStudents: number;
  rating: number;
  price: number;
  image: string;
  slug: string;
  instructor: {
    name: string;
    avatar: string;
  };
  isEnrolled: boolean;
  progress: number;
  certification: boolean;
}

export interface EnhancedCourse {
  id: string;
  title: string;
  description: string;
  area: string;
  level: number;
  estimatedHours?: number;
  enrollmentCount?: number;
  analytics?: {
    averageRating?: number;
  };
  image?: string;
  slug: string;
  instructors?: string[];
  progress?: number;
}

export interface CourseFilters {
  search: string;
  category: string;
  level: string;
  instructor: string;
  enrollment: string;
}

export interface CourseState {
  courses: EnhancedCourse[];
  filteredCourses: EnhancedCourse[];
  filters: CourseFilters;
  isLoading: boolean;
  error: string | null;
  currentPage: number;
  itemsPerPage: number;
}

export type EnrollmentStatus = APIEnrollmentStatus;

export interface Product {
  id: string;
  name: string;
  price: number;
  currency: string;
}
